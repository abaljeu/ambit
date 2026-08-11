namespace Gambol.Server

open System
open System.Data
open System.Threading.Tasks
open Gambol.Shared
open Newtonsoft.Json
open Npgsql
open NpgsqlTypes

/// Typed incremental updates for the normalized PostgreSQL graph projection.
[<RequireQualifiedAccess>]
module DatabaseProjection =

    type GraphPatch =
        { rootId: Guid
          revision: int }

    type ChildReplacement =
        { parentId: Guid
          rows: GraphProjection.ChildPersistenceRow list }

    type ProjectionPatch =
        { nodeUpserts: GraphProjection.NodePersistenceRow list
          childReplacements: ChildReplacement list
          graph: GraphPatch }

    type ProjectionCommand =
        | UpsertNodes of GraphProjection.NodePersistenceRow list
        | DeleteChildren of Guid list
        | InsertChildren of GraphProjection.ChildPersistenceRow list
        | UpsertGraph of GraphPatch

    type ProjectionMaintenancePatch =
        { protectedNodeIds: Guid list }

    type ProjectionMaintenanceCommand =
        | SweepUnreachable of ProjectionMaintenancePatch

    type SqlValue =
        | GuidValues of Guid list
        | StringValues of string list
        | OptionalStringValues of string option list
        | DateTimeValues of DateTime list
        | IntValues of int list
        | GuidValue of Guid
        | IntValue of int

    type SqlBinding =
        { name: string
          value: SqlValue }

    let private canonicalNodeIds =
        [ Graph.rootId; Graph.trashId; Graph.workspacesId; Graph.systemId ]

    let trimDeletedNodes (deletedNodeIds: NodeId list) (graph: Graph) : Graph =
        let protectedIds = Set.ofList (graph.root :: canonicalNodeIds)
        let deletedIds =
            deletedNodeIds
            |> Set.ofList
            |> Set.filter (fun nodeId -> not (Set.contains nodeId protectedIds))
        let retainedNodes =
            graph.nodes
            |> Map.filter (fun nodeId _ -> not (Set.contains nodeId deletedIds))
            |> Map.map (fun _ node ->
                { node with
                    children =
                        node.children
                        |> List.filter (fun child ->
                            not (Set.contains child.id deletedIds)) })

        Graph.fromNodes graph.root retainedNodes

    let startupSweepPatch : ProjectionMaintenancePatch =
        { protectedNodeIds = canonicalNodeIds |> List.map _.Value }

    let maintenanceCommand patch : ProjectionMaintenanceCommand =
        SweepUnreachable patch

    let private nodeIdFromOp op =
        match op with
        | Op.NewNode(nodeId, _)
        | Op.SetText(nodeId, _, _)
        | Op.SetClasses(nodeId, _, _)
        | Op.Replace(nodeId, _, _, _)
        | Op.NewSpecialNode(nodeId, _, _)
        | Op.SetName(nodeId, _, _)
        | Op.SetDocumentState(nodeId, _, _)
        | Op.SetUpdateTime(nodeId, _, _) -> nodeId

    let private replacedParentFromOp op =
        match op with
        | Op.Replace(parentId, _, _, _) -> Some parentId
        | _ -> None

    let private distinctIds select (changes: Change list) =
        changes
        |> List.collect _.ops
        |> List.choose select
        |> Set.ofList
        |> Set.toList

    let private nodeRows (graph: Graph) (changes: Change list) =
        changes
        |> distinctIds (nodeIdFromOp >> Some)
        |> List.choose (fun nodeId ->
            graph.nodes
            |> Map.tryFind nodeId
            |> Option.map GraphProjection.nodeRowFromNode)

    let private childReplacements (graph: Graph) (changes: Change list) =
        changes
        |> distinctIds replacedParentFromOp
        |> List.choose (fun parentId ->
            graph.nodes
            |> Map.tryFind parentId
            |> Option.map (fun node ->
                { parentId = parentId.Value
                  rows = GraphProjection.childRowsFromNode graph node }))

    let plan (graph: Graph) (revision: int) (changes: Change list) : ProjectionPatch =
        { nodeUpserts = nodeRows graph changes
          childReplacements = childReplacements graph changes
          graph =
            { rootId = graph.root.Value
              revision = revision } }

    let commands (patch: ProjectionPatch) : ProjectionCommand list =
        let childParents = patch.childReplacements |> List.map _.parentId
        let childRows = patch.childReplacements |> List.collect _.rows

        [ if not (List.isEmpty patch.nodeUpserts) then
              UpsertNodes patch.nodeUpserts
          if not (List.isEmpty childParents) then
              DeleteChildren childParents
          if not (List.isEmpty childRows) then
              InsertChildren childRows
          UpsertGraph patch.graph ]

    let private upsertNodesSql =
        """
        INSERT INTO nodes (
            id, text, name, css_classes, update_time, kind, document_state
        )
        SELECT
            id, text, name, css_classes::jsonb, update_time, kind, document_state
        FROM UNNEST(
            @ids::uuid[], @texts::text[], @names::text[], @css_classes::text[],
            @update_times::timestamptz[], @kinds::text[], @document_states::text[]
        ) AS rows(
            id, text, name, css_classes, update_time, kind, document_state
        )
        ON CONFLICT (id) DO UPDATE SET
            text = EXCLUDED.text,
            name = EXCLUDED.name,
            css_classes = EXCLUDED.css_classes,
            update_time = EXCLUDED.update_time,
            kind = EXCLUDED.kind,
            document_state = EXCLUDED.document_state
        """

    let private deleteChildrenSql =
        "DELETE FROM node_children WHERE parent_id = ANY(@parent_ids::uuid[])"

    let private insertChildrenSql =
        """
        INSERT INTO node_children (parent_id, ordinal, child_id, ownership)
        SELECT parent_id, ordinal, child_id, ownership
        FROM UNNEST(
            @parent_ids::uuid[], @ordinals::int[], @child_ids::uuid[],
            @ownerships::text[]
        ) AS rows(parent_id, ordinal, child_id, ownership)
        """

    let private upsertGraphSql =
        """
        INSERT INTO graph (singleton, root_id, revision)
        VALUES (1, @root_id, @revision)
        ON CONFLICT (singleton) DO UPDATE SET
            root_id = EXCLUDED.root_id,
            revision = EXCLUDED.revision
        """

    let private sweepUnreachableSql =
        """
        WITH RECURSIVE reachable(id) AS (
            SELECT root_id FROM graph WHERE singleton = 1
            UNION
            SELECT children.child_id
            FROM node_children AS children
            JOIN reachable ON reachable.id = children.parent_id
        ),
        deleted AS (
            DELETE FROM nodes
            WHERE EXISTS (SELECT 1 FROM graph WHERE singleton = 1)
              AND NOT (id = ANY(@protected_ids::uuid[]))
              AND NOT EXISTS (
                  SELECT 1 FROM reachable WHERE reachable.id = nodes.id
              )
            RETURNING id
        )
        SELECT id FROM deleted ORDER BY id
        """

    let sqlText command =
        match command with
        | UpsertNodes _ -> upsertNodesSql
        | DeleteChildren _ -> deleteChildrenSql
        | InsertChildren _ -> insertChildrenSql
        | UpsertGraph _ -> upsertGraphSql

    let maintenanceSqlText command =
        match command with
        | SweepUnreachable _ -> sweepUnreachableSql

    let private cssJson (row: GraphProjection.NodePersistenceRow) =
        JsonConvert.SerializeObject(row.cssClassNames)

    let bindings command : SqlBinding list =
        match command with
        | UpsertNodes rows ->
            [ { name = "ids"; value = GuidValues(rows |> List.map _.id) }
              { name = "texts"; value = StringValues(rows |> List.map _.text) }
              { name = "names"; value = OptionalStringValues(rows |> List.map _.name) }
              { name = "css_classes"; value = StringValues(rows |> List.map cssJson) }
              { name = "update_times"; value = DateTimeValues(rows |> List.map _.updateTime) }
              { name = "kinds"; value = StringValues(rows |> List.map _.kind) }
              { name = "document_states";
                value = StringValues(rows |> List.map _.documentState) } ]
        | DeleteChildren parentIds ->
            [ { name = "parent_ids"; value = GuidValues parentIds } ]
        | InsertChildren rows ->
            [ { name = "parent_ids"; value = GuidValues(rows |> List.map _.parentId) }
              { name = "ordinals"; value = IntValues(rows |> List.map _.ordinal) }
              { name = "child_ids"; value = GuidValues(rows |> List.map _.childId) }
              { name = "ownerships";
                value =
                    rows
                    |> List.map (fun row ->
                        match row.ownership with
                        | Ownership.Owner -> "owner"
                        | Ownership.Ref -> "ref")
                    |> StringValues } ]
        | UpsertGraph graph ->
            [ { name = "root_id"; value = GuidValue graph.rootId }
              { name = "revision"; value = IntValue graph.revision } ]

    let maintenanceBindings command : SqlBinding list =
        match command with
        | SweepUnreachable patch ->
            [ { name = "protected_ids"
                value = GuidValues patch.protectedNodeIds } ]

    let private arrayParameter name itemType values =
        let parameter = NpgsqlParameter(name, NpgsqlDbType.Array ||| itemType)
        parameter.Value <- values
        parameter

    let private parameterFromBinding binding =
        match binding.value with
        | GuidValues values ->
            arrayParameter binding.name NpgsqlDbType.Uuid (box (List.toArray values))
        | StringValues values ->
            arrayParameter binding.name NpgsqlDbType.Text (box (List.toArray values))
        | OptionalStringValues values ->
            let nullable = values |> List.map (Option.defaultValue null) |> List.toArray
            arrayParameter binding.name NpgsqlDbType.Text (box nullable)
        | DateTimeValues values ->
            let dbValues = values |> List.map NodeUpdateTime.toDbPrecision |> List.toArray
            arrayParameter binding.name NpgsqlDbType.TimestampTz (box dbValues)
        | IntValues values ->
            arrayParameter binding.name NpgsqlDbType.Integer (box (List.toArray values))
        | GuidValue value -> NpgsqlParameter(binding.name, NpgsqlDbType.Uuid, Value = value)
        | IntValue value -> NpgsqlParameter(binding.name, NpgsqlDbType.Integer, Value = value)

    let executeMaintenance
        (connectionString: string)
        (command: ProjectionMaintenanceCommand)
        : Task<Guid list> =
        task {
            use conn = Database.getConnection connectionString
            do! conn.OpenAsync()
            use sqlCommand = new NpgsqlCommand(maintenanceSqlText command, conn)
            maintenanceBindings command
            |> List.map parameterFromBinding
            |> List.iter (sqlCommand.Parameters.Add >> ignore)
            use! reader = sqlCommand.ExecuteReaderAsync()
            let deletedIds = ResizeArray<Guid>()

            while reader.Read() do
                deletedIds.Add(reader.GetGuid 0)

            return deletedIds |> Seq.toList
        }

    let private executeCommand
        (conn: NpgsqlConnection)
        (tx: NpgsqlTransaction)
        (command: ProjectionCommand)
        : Task =
        task {
            use sqlCommand = new NpgsqlCommand(sqlText command, conn, tx)
            bindings command
            |> List.map parameterFromBinding
            |> List.iter (sqlCommand.Parameters.Add >> ignore)
            do! sqlCommand.ExecuteNonQueryAsync() :> Task
        }

    let executeWithTx (tx: IDbTransaction) (patch: ProjectionPatch) : Task =
        task {
            let conn = tx.Connection :?> NpgsqlConnection
            let npgsqlTx = tx :?> NpgsqlTransaction

            for command in commands patch do
                do! executeCommand conn npgsqlTx command |> Async.AwaitTask
        }

    let private hasGraphSingleton
        (conn: NpgsqlConnection)
        (tx: NpgsqlTransaction)
        : Task<bool> =
        task {
            use command =
                new NpgsqlCommand(
                    "SELECT EXISTS (SELECT 1 FROM graph WHERE singleton = 1)",
                    conn,
                    tx)
            let! result = command.ExecuteScalarAsync()
            return unbox<bool> result
        }

    let persistWithTx
        (tx: IDbTransaction)
        (graph: Graph)
        (patch: ProjectionPatch)
        : Task =
        task {
            let conn = tx.Connection :?> NpgsqlConnection
            let npgsqlTx = tx :?> NpgsqlTransaction
            let! initialized = hasGraphSingleton conn npgsqlTx

            if initialized then
                do! executeWithTx tx patch |> Async.AwaitTask
            else
                do!
                    Database.replaceGraphProjectionWithTx tx graph patch.graph.revision
                    |> Async.AwaitTask
        }
