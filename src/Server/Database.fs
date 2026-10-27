namespace Gambol.Server

open System
open System.Data
open System.Threading.Tasks
open Dapper
open Gambol.Shared
open Newtonsoft.Json
open Npgsql

/// PostgreSQL: append-only `events` plus normalized `graph` / `nodes` / `node_children`.
[<RequireQualifiedAccess>]
module Database =

    let getConnection (connectionString: string) =
        new NpgsqlConnection(connectionString)

    let private deadlockSqlState = "40P01"

    let private isDeadlock (ex: exn) =
        match ex with
        | :? PostgresException as pg ->
            pg.SqlState = deadlockSqlState
        | _ -> false

    /// Retry when Postgres aborts this transaction as the deadlock victim.
    /// Schema work locks `nodes` before `node_children`. A peer that already
    /// holds `node_children` and then waits for `nodes` deadlocks with it.
    let private executeWithDeadlockRetry
        (connectionString: string)
        (sql: string)
        : Task =
        let rec attempt (retriesLeft: int) : Task =
            task {
                try
                    use conn = getConnection connectionString
                    do! conn.OpenAsync()
                    use cmd = conn.CreateCommand()
                    cmd.CommandText <- sql
                    do! cmd.ExecuteNonQueryAsync() :> Task
                with ex when isDeadlock ex && retriesLeft > 0 ->
                    do! attempt (retriesLeft - 1)
            }

        attempt 4

    let initSchema (connectionString: string) : Task =
        executeWithDeadlockRetry connectionString """
                DROP TABLE IF EXISTS snapshots;
                DROP TABLE IF EXISTS changes;

                CREATE TABLE IF NOT EXISTS events (
                    event_id        INT          PRIMARY KEY,
                    submission_id   UUID         NOT NULL,
                    payload         TEXT         NOT NULL,
                    recorded_at     TIMESTAMPTZ  DEFAULT NOW()
                );

                CREATE UNIQUE INDEX IF NOT EXISTS idx_events_submission_id
                    ON events (submission_id);

                CREATE TABLE IF NOT EXISTS graph (
                    singleton   SMALLINT PRIMARY KEY DEFAULT 1 CHECK (singleton = 1),
                    root_id     UUID         NOT NULL,
                    revision    INT          NOT NULL
                );

                CREATE TABLE IF NOT EXISTS nodes (
                    id            UUID         PRIMARY KEY,
                    text          TEXT         NOT NULL,
                    name          TEXT         NULL,
                    css_classes   JSONB        NOT NULL,
                    update_time   TIMESTAMPTZ  NOT NULL
                        DEFAULT '0001-01-01T00:00:00Z'
                );

                ALTER TABLE nodes
                    ADD COLUMN IF NOT EXISTS update_time TIMESTAMPTZ;

                UPDATE nodes
                SET update_time = '0001-01-01T00:00:00Z'::timestamptz
                WHERE update_time IS NULL;

                ALTER TABLE nodes
                    ALTER COLUMN update_time SET NOT NULL;

                ALTER TABLE nodes
                    ADD COLUMN IF NOT EXISTS kind TEXT NOT NULL DEFAULT 'normal';

                ALTER TABLE nodes
                    ADD COLUMN IF NOT EXISTS document_state TEXT NOT NULL DEFAULT 'current';

                ALTER TABLE nodes
                    ADD COLUMN IF NOT EXISTS persist_state TEXT NOT NULL DEFAULT 'persisted';

                ALTER TABLE nodes
                    ADD COLUMN IF NOT EXISTS persisted_version
                    INTEGER NOT NULL DEFAULT 0;

                ALTER TABLE nodes
                    ADD COLUMN IF NOT EXISTS changed_version
                    INTEGER NOT NULL DEFAULT 0;

                UPDATE nodes
                SET kind = 'workspace'
                WHERE id = '00000000-0000-0000-0000-000000000000';

                UPDATE nodes
                SET kind = 'workspaces'
                WHERE id = '00000000-0000-0000-0000-000000000002';

                UPDATE nodes
                SET kind = 'trash'
                WHERE id = '00000000-0000-0000-0000-000000000001';

                CREATE TABLE IF NOT EXISTS node_children (
                    parent_id   UUID         NOT NULL REFERENCES nodes (id) ON DELETE CASCADE,
                    ordinal     INT          NOT NULL,
                    child_id    UUID         NOT NULL REFERENCES nodes (id) ON DELETE CASCADE,
                    ownership   TEXT         NOT NULL CHECK (ownership IN ('owner','ref')),
                    PRIMARY KEY (parent_id, ordinal)
                );

                CREATE INDEX IF NOT EXISTS idx_node_children_child
                    ON node_children (child_id);
            """

    type EventRow =
        { event_id: int
          payload: string }

    type GraphSingletonRow =
        { root_id: Guid
          revision: int }

    type NodeDbRow =
        { id: Guid
          text: string
          name: string // null from SQL when column is NULL
          css_classes: string
          update_time: DateTime
          kind: string
          document_state: string
          persist_state: string
          persisted_version: int
          changed_version: int }

    type NodeChildDbRow =
        { parent_id: Guid
          ordinal: int
          child_id: Guid
          ownership: string }

    let private cssJson (classes: CssClasses) : string =
        JsonConvert.SerializeObject(CssClass.toList classes)

    let private decodeCss (json: string) : CssClasses =
        if String.IsNullOrWhiteSpace json then
            CssClass.empty
        else
            try
                CssClass.ofList (JsonConvert.DeserializeObject<string list>(json))
            with _ ->
                CssClass.empty


    let appendEvent
        (connectionString: string)
        (eventId: int)
        (submissionId: Guid)
        (json: string)
        : Task =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()
            let! _ =
                conn.ExecuteAsync(
                    """
                    INSERT INTO events (event_id, submission_id, payload)
                    VALUES (@event_id, @submission_id, @payload)
                    """,
                    {| event_id = eventId
                       submission_id = submissionId
                       payload = json |})
            return ()
        }

    let clearEvents (connectionString: string) : Task =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()
            do! conn.ExecuteAsync("DELETE FROM events") :> Task
        }

    /// Schema plus an empty graph. Identity on events restarts at 1.
    let resetStoredTables (connectionString: string) : Task<unit> =
        task {
            do! initSchema connectionString
            do!
                executeWithDeadlockRetry
                    connectionString
                    ("TRUNCATE TABLE events, node_children, nodes, graph " +
                     "RESTART IDENTITY CASCADE;")
            return ()
        }

    let getEventsAfter
        (connectionString: string)
        (afterEventId: int)
        : Task<EventRow list> =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()
            let! rows =
                conn.QueryAsync<EventRow>(
                    """
                    SELECT event_id, payload FROM events
                    WHERE event_id > @after
                    ORDER BY event_id ASC
                    """,
                    {| after = afterEventId |})
            return rows |> Seq.toList
        }

    let getEvents (connectionString: string) : Task<EventRow list> =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()
            let! rows =
                conn.QueryAsync<EventRow>(
                    """
                    SELECT event_id, payload FROM events
                    ORDER BY event_id ASC
                    """)
            return rows |> Seq.toList
        }

    let tryGetGraphSingleton (connectionString: string) : Task<GraphSingletonRow option> =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()

            let! row =
                conn.QueryFirstOrDefaultAsync<GraphSingletonRow>(
                    "SELECT root_id, revision FROM graph WHERE singleton = 1")

            return if obj.ReferenceEquals(row, null) then None else Some row
        }

    let isEmpty (connectionString: string) : Task<bool> =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()

            let! hasGraph =
                conn.QuerySingleAsync<bool>(
                    "SELECT EXISTS (SELECT 1 FROM graph)")

            return not hasGraph
        }

    let private readNodeRows (conn: NpgsqlConnection) : Task<NodeDbRow list> =
        task {
            let! rows =
                conn.QueryAsync<NodeDbRow>(
                    """
                    SELECT id, text, name, css_classes::text, update_time, kind,
                           document_state, persist_state,
                           persisted_version, changed_version
                    FROM nodes
                    """)
            return rows |> Seq.toList
        }

    let private readChildRows (conn: NpgsqlConnection) : Task<NodeChildDbRow list> =
        task {
            let! rows =
                conn.QueryAsync<NodeChildDbRow>(
                    "SELECT parent_id, ordinal, child_id, ownership FROM node_children")

            return rows |> Seq.toList
        }

    let private decodeProjectionEventId (eventId: int) =
        EventId.fromJson eventId

    let tryLoadGraphFromProjection (connectionString: string) 
            : Task<Result<Graph * EventId, string>> =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()

            let! singleton =
                conn.QueryFirstOrDefaultAsync<GraphSingletonRow>(
                    "SELECT root_id, revision FROM graph WHERE singleton = 1")

            match
                if obj.ReferenceEquals(singleton, null) then
                    None
                else
                    Some singleton
            with
            | None -> return Ok(Graph.create (), EventId.zero)
            | Some gRow ->
                let! nRows = readNodeRows conn |> Async.AwaitTask
                let! cRows = readChildRows conn |> Async.AwaitTask

                if List.isEmpty nRows then
                    return Ok(Graph.create (), decodeProjectionEventId gRow.revision)
                else

                let nPersist =
                    nRows
                    |> List.map (fun r ->
                        ({ id = r.id
                           text = r.text
                           name =
                            if isNull r.name then
                                None
                            else
                                Some r.name
                           kind = r.kind
                           documentState = r.document_state
                           persistState = r.persist_state
                           persistedVersion = r.persisted_version
                           changedVersion = r.changed_version
                           cssClassNames = CssClass.toList (decodeCss r.css_classes)
                           updateTime = NodeUpdateTime.toDbPrecision r.update_time }
                        : GraphProjection.NodePersistenceRow))

                let cPersist =
                    cRows
                    |> List.map (fun r ->
                        ({ parentId = r.parent_id
                           ordinal = r.ordinal
                           childId = r.child_id
                           ownership =
                            match r.ownership with
                            | "owner" -> Ownership.Owner
                            | "ref" -> Ownership.Ref
                            | x -> failwith $"invalid ownership: {x}" }
                        : GraphProjection.ChildPersistenceRow))

                let rootId = NodeId gRow.root_id

                return
                    match GraphProjection.graphFromPersistence rootId nPersist cPersist with
                    | Ok g -> Ok(g, decodeProjectionEventId gRow.revision)
                    | Error e -> Error e
        }

    let replaceGraphProjectionWithTx (tx: IDbTransaction) (graph: Graph) (revision: int) : Task =
        task {
            let conn = tx.Connection :?> NpgsqlConnection

            do! conn.ExecuteAsync("TRUNCATE node_children, nodes RESTART IDENTITY CASCADE", transaction = tx)
                :> Task

            do!
                conn.ExecuteAsync(
                    """
                    INSERT INTO graph (singleton, root_id, revision) VALUES (1, @root, @rev)
                    ON CONFLICT (singleton) DO UPDATE SET root_id = @root, revision = @rev
                    """,
                    {| root = graph.root.Value; rev = revision |},
                    tx)
                :> Task

            let nodeRows = GraphProjection.nodeRowsFromGraph graph

            for r in nodeRows do
                let nameParam: obj = match r.name with | None -> null | Some n -> box n

                do!
                    conn.ExecuteAsync(
                        """
                        INSERT INTO nodes (
                            id, text, name, css_classes, update_time, kind,
                            document_state, persist_state,
                            persisted_version, changed_version
                        )
                        VALUES (
                            @id, @text, @name, CAST(@css AS jsonb), @update_time, @kind,
                            @document_state, @persist_state,
                            @persisted_version, @changed_version
                        )
                        """,
                        {| id = r.id
                           text = r.text
                           name = nameParam
                           css = cssJson (CssClass.ofList r.cssClassNames)
                           update_time = NodeUpdateTime.toDbPrecision r.updateTime
                           kind = r.kind
                           document_state = r.documentState
                           persist_state = r.persistState
                           persisted_version = r.persistedVersion
                           changed_version = r.changedVersion |},
                        tx)
                    :> Task

            let childRows = GraphProjection.childRowsFromGraph graph

            for c in childRows do
                let own =
                    match c.ownership with
                    | Ownership.Owner -> "owner"
                    | Ownership.Ref -> "ref"

                do!
                    conn.ExecuteAsync(
                        """
                        INSERT INTO node_children (parent_id, ordinal, child_id, ownership)
                        VALUES (@p, @o, @c, @own)
                        """,
                        {| p = c.parentId
                           o = c.ordinal
                           c = c.childId
                           own = own |},
                        tx)
                    :> Task
        }

    let loadPersistedState
        (connectionString: string)
        : Task<State> =
        task {
            let! proj = tryLoadGraphFromProjection connectionString |> Async.AwaitTask

            let graph, eventId =
                match proj with
                | Ok (g, id) -> g, id
                | Error _ -> Graph.create (), EventId.zero

            return
                { graph = graph
                  eventId = eventId }
        }

    /// Truncate SQL tables and replace the projection from a pre-loaded file `State`.
    let rebuildFromDocumentFiles (connectionString: string) (fileState: State) : Task =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()
            use tx = conn.BeginTransaction()

            do!
                conn.ExecuteAsync(
                    "TRUNCATE node_children, nodes RESTART IDENTITY CASCADE",
                    transaction = tx)
                :> Task

            do! conn.ExecuteAsync("DELETE FROM graph", transaction = tx) :> Task

            do!
                replaceGraphProjectionWithTx
                    tx
                    fileState.graph
                    (EventId.value fileState.eventId)
                |> Async.AwaitTask
            tx.Commit()
        }

    let private updateDocumentStateSql =
        """
        UPDATE nodes
        SET document_state = @document_state
        WHERE id = @id
        """

    let private updatePersistStateSql =
        """
        UPDATE nodes
        SET persist_state = @persist_state,
            persisted_version = @persisted_version,
            changed_version = @changed_version
        WHERE id = @id
        """

    let private updatePersistedSql =
        """
        UPDATE nodes
        SET persist_state = @persist_state,
            persisted_version = @persisted_version,
            changed_version = @changed_version,
            update_time = @update_time
        WHERE id = @id
        """

    let private updateReceivedNodeSql =
        """
        UPDATE nodes
        SET text = @text,
            name = @name,
            css_classes = CAST(@css AS jsonb),
            update_time = @update_time,
            kind = @kind,
            document_state = @document_state,
            persist_state = @persist_state,
            persisted_version = @persisted_version,
            changed_version = @changed_version
        WHERE id = @id
        """

    let private executeSql
        (connectionString: string)
        (sql: string)
        (args: obj)
        : Task<unit> =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()
            let! _ = conn.ExecuteAsync(sql, args)
            return ()
        }

    let private rowOf (graph: Graph) (nodeId: NodeId) =
        Map.tryFind nodeId graph.nodes
        |> Option.map GraphProjection.nodeRowFromNode

    let private writeDocumentState connectionString graph nodeId =
        match rowOf graph nodeId with
        | None -> task { return () }
        | Some row ->
            executeSql
                connectionString
                updateDocumentStateSql
                {| id = row.id
                   document_state = row.documentState |}

    let private writePersistState connectionString graph nodeId =
        match rowOf graph nodeId with
        | None -> task { return () }
        | Some row ->
            executeSql
                connectionString
                updatePersistStateSql
                {| id = row.id
                   persist_state = row.persistState
                   persisted_version = row.persistedVersion
                   changed_version = row.changedVersion |}

    let private writePersistedNode connectionString graph nodeId =
        match rowOf graph nodeId with
        | None -> task { return () }
        | Some row ->
            executeSql
                connectionString
                updatePersistedSql
                {| id = row.id
                   persist_state = row.persistState
                   persisted_version = row.persistedVersion
                   changed_version = row.changedVersion
                   update_time = row.updateTime |}

    let private writeReceivedNode connectionString graph nodeId =
        match rowOf graph nodeId with
        | None -> task { return () }
        | Some row ->
            let nameParam: obj =
                match row.name with
                | None -> null
                | Some name -> box name
            executeSql
                connectionString
                updateReceivedNodeSql
                {| id = row.id
                   text = row.text
                   name = nameParam
                   css = cssJson (CssClass.ofList row.cssClassNames)
                   update_time = row.updateTime
                   kind = row.kind
                   document_state = row.documentState
                   persist_state = row.persistState
                   persisted_version = row.persistedVersion
                   changed_version = row.changedVersion |}

    /// One node row for a Notice. Other Notices do not write.
    let writeNoticeRow
        (connectionString: string)
        (notice: Notice)
        (graph: Graph)
        : Task =
        match notice with
        | Notice.NodeParsed nodeId
        | Notice.NodeUnparsed nodeId ->
            writeDocumentState connectionString graph nodeId
        | Notice.NodeUnpersisted nodeId ->
            writePersistState connectionString graph nodeId
        | Notice.NodePersisted (nodeId, _, _) ->
            writePersistedNode connectionString graph nodeId
        | Notice.NodeReceived nodeId ->
            writeReceivedNode connectionString graph nodeId
        | Notice.NodeUploading _
        | Notice.NodeParsing _
        | Notice.NodePersisting _
        | Notice.PersistFailed _
        | Notice.ParseFailed _ ->
            task { return () }
