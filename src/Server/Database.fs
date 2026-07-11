namespace Gambol.Server

open System
open System.Data
open System.Threading.Tasks
open Dapper
open Gambol.Shared
open Newtonsoft.Json
open Npgsql

/// PostgreSQL: append-only `changes` plus normalized `graph` / `nodes` / `node_children`.
[<RequireQualifiedAccess>]
module Database =

    let getConnection (connectionString: string) =
        new NpgsqlConnection(connectionString)

    let initSchema (connectionString: string) : Task =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()
            use cmd = conn.CreateCommand()

            cmd.CommandText <- """
                DROP TABLE IF EXISTS snapshots;

                CREATE TABLE IF NOT EXISTS changes (
                    seq_id               BIGSERIAL    PRIMARY KEY,
                    client_base_revision INT          NOT NULL,
                    change_uuid          UUID         NOT NULL,
                    payload              TEXT         NOT NULL,
                    recorded_at          TIMESTAMPTZ  DEFAULT NOW()
                );

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'changes' AND column_name = 'change_id'
                    ) AND NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'changes' AND column_name = 'client_base_revision'
                    ) THEN
                        ALTER TABLE changes RENAME COLUMN change_id TO client_base_revision;
                    END IF;
                END $$;

                ALTER TABLE changes
                    ADD COLUMN IF NOT EXISTS change_uuid UUID;

                UPDATE changes
                SET change_uuid = (payload::jsonb ->> 'changeId')::uuid
                WHERE change_uuid IS NULL
                  AND payload::jsonb ? 'changeId';

                ALTER TABLE changes
                    ALTER COLUMN change_uuid SET NOT NULL;

                CREATE UNIQUE INDEX IF NOT EXISTS idx_changes_change_uuid
                    ON changes (change_uuid);

                CREATE INDEX IF NOT EXISTS idx_changes_client_base_revision
                    ON changes (client_base_revision);

                ALTER TABLE changes
                    ADD COLUMN IF NOT EXISTS server_revision_after INTEGER;

                UPDATE changes AS c
                SET server_revision_after = s.rn
                FROM (
                    SELECT seq_id, ROW_NUMBER() OVER (ORDER BY seq_id) AS rn
                    FROM changes
                    WHERE server_revision_after IS NULL
                ) AS s
                WHERE c.seq_id = s.seq_id;

                ALTER TABLE changes
                    ALTER COLUMN server_revision_after SET NOT NULL;

                CREATE INDEX IF NOT EXISTS idx_changes_server_revision_after
                    ON changes (server_revision_after);

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

            do! cmd.ExecuteNonQueryAsync() :> Task
        }

    type ChangeRow =
        { client_base_revision: int
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
          document_state: string }

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

    let appendChangeWithTx
        (tx: IDbTransaction)
        (serverRevisionAfter: int)
        (clientBaseRevision: int)
        (clientChangeId: Guid)
        (json: string)
        : Task =
        tx.Connection.ExecuteAsync(
            """
            INSERT INTO changes (
                client_base_revision,
                change_uuid,
                server_revision_after,
                payload
            )
            VALUES (
                @client_base_revision,
                @change_uuid,
                @server_revision_after,
                @payload
            )
            """,
            {| client_base_revision = clientBaseRevision
               change_uuid = clientChangeId
               server_revision_after = serverRevisionAfter
               payload = json |},
            tx)
        :> Task

    let appendChange
        (connectionString: string)
        (serverRevisionAfter: int)
        (clientBaseRevision: int)
        (clientChangeId: Guid)
        (json: string)
        : Task =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()
            use tx = conn.BeginTransaction()

            do!
                appendChangeWithTx tx serverRevisionAfter clientBaseRevision clientChangeId json

            tx.Commit()
        }

    let getChangesAfterCheckpointRevision
        (connectionString: string)
        (checkpointRevision: int)
        : Task<ChangeRow list> =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()

            let! rows =
                conn.QueryAsync<ChangeRow>(
                    """
                    SELECT client_base_revision, payload FROM changes
                    WHERE server_revision_after > @rev
                    ORDER BY server_revision_after ASC
                    """,
                    {| rev = checkpointRevision |})

            return rows |> Seq.toList
        }

    let hasPersistedChangeId
        (connectionString: string)
        (changeId: Guid)
        : Task<bool> =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()

            let! exists =
                conn.QuerySingleAsync<bool>(
                    """
                    SELECT EXISTS (
                        SELECT 1 FROM changes WHERE change_uuid = @change_uuid
                    )
                    """,
                    {| change_uuid = changeId |})

            return exists
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

            let! hasChanges =
                conn.QuerySingleAsync<bool>(
                    "SELECT EXISTS (SELECT 1 FROM changes)")

            return not hasGraph && not hasChanges
        }

    let private readNodeRows (conn: NpgsqlConnection) : Task<NodeDbRow list> =
        task {
            let! rows =
                conn.QueryAsync<NodeDbRow>(
                    """
                    SELECT id, text, name, css_classes::text, update_time, kind, document_state
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

    let tryLoadGraphFromProjection (connectionString: string) : Task<Result<Graph * int, string>> =
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
            | None -> return Ok(Graph.create (), 0)
            | Some gRow ->
                let! nRows = readNodeRows conn |> Async.AwaitTask
                let! cRows = readChildRows conn |> Async.AwaitTask

                if List.isEmpty nRows then
                    return Ok(Graph.create (), gRow.revision)
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
                    | Ok g -> Ok(g, gRow.revision)
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
                            id, text, name, css_classes, update_time, kind, document_state
                        )
                        VALUES (
                            @id, @text, @name, CAST(@css AS jsonb), @update_time, @kind,
                            @document_state
                        )
                        """,
                        {| id = r.id
                           text = r.text
                           name = nameParam
                           css = cssJson (CssClass.ofList r.cssClassNames)
                           update_time = NodeUpdateTime.toDbPrecision r.updateTime
                           kind = r.kind
                           document_state = r.documentState |},
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

    let loadPersistedState (connectionString: string) (decodeChange: string -> Result<Change, string>) : Task<State> =
        task {
            let! proj = tryLoadGraphFromProjection connectionString |> Async.AwaitTask

            let baseGraph, baseRevision =
                match proj with
                | Ok (g, r) -> g, r
                | Error _ -> Graph.create (), 0

            let! rows =
                getChangesAfterCheckpointRevision connectionString baseRevision
                |> Async.AwaitTask

            let st0 =
                { graph = baseGraph
                  history = History.empty
                  revision = Revision baseRevision }

            let stFinal =
                rows
                |> List.fold
                    (fun st row ->
                        match decodeChange row.payload with
                        | Error _ -> st
                        | Ok change ->
                            match History.applyChange change st with
                            | ApplyResult.Changed newState ->
                                { newState with revision = Revision (st.revision.Value + 1) }
                            | _ -> st)
                    st0

            return stFinal
        }

    /// Truncate SQL tables and replace the projection from a pre-loaded file `State`.
    /// `changes` is cleared; new posts repopulate the log.
    let rebuildFromDocumentFiles (connectionString: string) (fileState: State) : Task =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()
            use tx = conn.BeginTransaction()

            do!
                conn.ExecuteAsync(
                    "TRUNCATE changes RESTART IDENTITY CASCADE",
                    transaction = tx)
                :> Task

            do!
                conn.ExecuteAsync(
                    "TRUNCATE node_children, nodes RESTART IDENTITY CASCADE",
                    transaction = tx)
                :> Task

            do! conn.ExecuteAsync("DELETE FROM graph", transaction = tx) :> Task

            do! replaceGraphProjectionWithTx tx fileState.graph fileState.revision.Value |> Async.AwaitTask
            tx.Commit()
        }
