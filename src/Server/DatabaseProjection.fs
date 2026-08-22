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

    type ProjectionMaintenanceResult =
        { deletedIds: Guid list
          requiresReload: bool
          logFacts: ProjectionOwnershipRepair.LogFacts }

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
        | Op.Replace(nodeId, _, _)
        | Op.NewSpecialNode(nodeId, _, _)
        | Op.SetName(nodeId, _, _)
        | Op.SetDocumentState(nodeId, _, _)
        | Op.SetUpdateTime(nodeId, _, _) -> nodeId

    let private replacedParentFromOp op =
        match op with
        | Op.Replace(parentId, _, _) -> Some parentId
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

    let sqlText command =
        match command with
        | UpsertNodes _ -> upsertNodesSql
        | DeleteChildren _ -> deleteChildrenSql
        | InsertChildren _ -> insertChildrenSql
        | UpsertGraph _ -> upsertGraphSql

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

    let private executeCommandSafe
        (conn: NpgsqlConnection)
        (tx: NpgsqlTransaction)
        (command: ProjectionCommand)
        : Task<Result<unit, string>> =
        task {
            try
                use sqlCommand = new NpgsqlCommand(sqlText command, conn, tx)
                bindings command
                |> List.map parameterFromBinding
                |> List.iter (sqlCommand.Parameters.Add >> ignore)
                do! sqlCommand.ExecuteNonQueryAsync() :> Task
                return Ok ()
            with ex ->
                return Error ex.Message
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

    let private noOpMaintenance: ProjectionMaintenanceResult =
        { deletedIds = []
          requiresReload = false
          logFacts = ProjectionOwnershipRepair.emptyPlan.logFacts }

    let private execSql conn tx sql parameters : Task<Result<unit, string>> = task {
        try
            use cmd = new NpgsqlCommand(sql, conn, tx)
            parameters |> List.iter (cmd.Parameters.Add >> ignore)
            do! cmd.ExecuteNonQueryAsync() :> Task
            return Ok ()
        with ex ->
            return Error ex.Message
    }

    let rec private readRows (reader: System.Data.Common.DbDataReader) map acc =
        if reader.Read() then readRows reader map (map reader :: acc)
        else List.rev acc

    let private readNode (reader: System.Data.Common.DbDataReader) : GraphProjection.NodePersistenceRow =
        let cssJson = reader.GetString 3
        let css =
            if String.IsNullOrWhiteSpace cssJson then []
            else JsonConvert.DeserializeObject<string list>(cssJson)
        { id = reader.GetGuid 0
          text = reader.GetString 1
          name = if reader.IsDBNull 2 then None else Some(reader.GetString 2)
          kind = reader.GetString 5
          documentState = reader.GetString 6
          cssClassNames = css
          updateTime = NodeUpdateTime.toDbPrecision (reader.GetDateTime 4) }

    let private parseOwnership value =
        match value with
        | "owner" -> Ok Ownership.Owner
        | "ref" -> Ok Ownership.Ref
        | other -> Error $"invalid ownership: {other}"

    let rec private readChildren (reader: System.Data.Common.DbDataReader) acc =
        if not (reader.Read()) then
            Ok(List.rev acc)
        else
            match parseOwnership (reader.GetString 3) with
            | Error e -> Error e
            | Ok ownership ->
                let row: GraphProjection.ChildPersistenceRow =
                    { parentId = reader.GetGuid 0
                      ordinal = reader.GetInt32 1
                      childId = reader.GetGuid 2
                      ownership = ownership }
                readChildren reader (row :: acc)

    let private ownershipText ownership =
        match ownership with
        | Ownership.Owner -> "owner"
        | Ownership.Ref -> "ref"

    let private applyDeletes conn tx ids = task {
        if List.isEmpty ids then
            return Ok ()
        else
            return!
                execSql conn tx
                    "DELETE FROM nodes WHERE id = ANY(@ids::uuid[])"
                    [ arrayParameter "ids" NpgsqlDbType.Uuid (box (List.toArray ids)) ]
    }

    let private applyOwnership
        conn
        tx
        (updates: ProjectionOwnershipRepair.OwnershipUpdate list)
        = task {
        if List.isEmpty updates then
            return Ok ()
        else
            let parentIds = updates |> List.map _.parentId |> List.toArray
            let ordinals = updates |> List.map _.ordinal |> List.toArray
            let childIds = updates |> List.map _.childId |> List.toArray
            let ownerships =
                updates |> List.map (fun u -> ownershipText u.ownership) |> List.toArray
            return!
                execSql conn tx
                    """
                    UPDATE node_children AS c SET ownership = v.ownership
                    FROM UNNEST(
                        @parent_ids::uuid[], @ordinals::int[], @child_ids::uuid[],
                        @ownerships::text[]
                    ) AS v(parent_id, ordinal, child_id, ownership)
                    WHERE c.parent_id = v.parent_id
                      AND c.ordinal = v.ordinal
                      AND c.child_id = v.child_id
                    """
                    [ arrayParameter "parent_ids" NpgsqlDbType.Uuid (box parentIds)
                      arrayParameter "ordinals" NpgsqlDbType.Integer (box ordinals)
                      arrayParameter "child_ids" NpgsqlDbType.Uuid (box childIds)
                      arrayParameter "ownerships" NpgsqlDbType.Text (box ownerships) ]
    }

    let private rootOrdinalStage = 1_000_000

    let private applyRootOrdinals
        conn
        tx
        rootId
        (updates: ProjectionOwnershipRepair.RootOrdinalUpdate list)
        = task {
        if List.isEmpty updates then
            return Ok ()
        else
            let fromOrdinals = updates |> List.map _.fromOrdinal |> List.toArray
            let ordinals = updates |> List.map _.ordinal |> List.toArray
            match!
                execSql conn tx
                    """
                    UPDATE node_children
                    SET ordinal = ordinal + @stage
                    WHERE parent_id = @root_id AND ordinal = ANY(@from_ordinals::int[])
                    """
                    [ NpgsqlParameter("root_id", NpgsqlDbType.Uuid, Value = rootId)
                      NpgsqlParameter("stage", NpgsqlDbType.Integer, Value = rootOrdinalStage)
                      arrayParameter "from_ordinals" NpgsqlDbType.Integer (box fromOrdinals) ]
            with
            | Error e -> return Error e
            | Ok () ->
                return!
                    execSql conn tx
                        """
                        UPDATE node_children AS c SET ordinal = v.ordinal
                        FROM UNNEST(@from_ordinals::int[], @ordinals::int[])
                            AS v(from_ordinal, ordinal)
                        WHERE c.parent_id = @root_id
                          AND c.ordinal = v.from_ordinal + @stage
                        """
                        [ NpgsqlParameter("root_id", NpgsqlDbType.Uuid, Value = rootId)
                          NpgsqlParameter("stage", NpgsqlDbType.Integer, Value = rootOrdinalStage)
                          arrayParameter "from_ordinals" NpgsqlDbType.Integer (box fromOrdinals)
                          arrayParameter "ordinals" NpgsqlDbType.Integer (box ordinals) ]
    }

    let private applyPlan conn tx rootId (plan: ProjectionOwnershipRepair.Plan) = task {
        match! applyDeletes conn tx plan.deleteNodeIds with
        | Error e -> return Error e
        | Ok () ->
        match! applyOwnership conn tx plan.ownershipUpdates with
        | Error e -> return Error e
        | Ok () ->
        match!
            if List.isEmpty plan.insertNodes then
                task { return Ok () }
            else
                executeCommandSafe conn tx (UpsertNodes plan.insertNodes)
        with
        | Error e -> return Error e
        | Ok () ->
        match! applyRootOrdinals conn tx rootId plan.rootOrdinalUpdates with
        | Error e -> return Error e
        | Ok () ->
            if List.isEmpty plan.insertChildren then
                return Ok ()
            else
                return!
                    executeCommandSafe conn tx (InsertChildren plan.insertChildren)
    }

    let executeMaintenance
        (connectionString: string)
        (command: ProjectionMaintenanceCommand)
        : Task<Result<ProjectionMaintenanceResult, string>> =
        task {
            let protectedIds =
                match command with
                | SweepUnreachable patch -> patch.protectedNodeIds
            use conn = Database.getConnection connectionString
            do! conn.OpenAsync()
            use tx = conn.BeginTransaction()
            try
                use rootCmd =
                    new NpgsqlCommand(
                        "SELECT root_id FROM graph WHERE singleton = 1",
                        conn,
                        tx)
                let! rootObj = rootCmd.ExecuteScalarAsync()
                if isNull rootObj || rootObj = box DBNull.Value then
                    tx.Commit()
                    return Ok noOpMaintenance
                else
                let rootId = unbox<Guid> rootObj
                use nodeCmd =
                    new NpgsqlCommand(
                        """
                        SELECT id, text, name, css_classes::text, update_time,
                               kind, document_state
                        FROM nodes
                        """,
                        conn,
                        tx)
                let! nodeReader = nodeCmd.ExecuteReaderAsync()
                let nodes = readRows nodeReader readNode []
                do! nodeReader.DisposeAsync()
                use childCmd =
                    new NpgsqlCommand(
                        "SELECT parent_id, ordinal, child_id, ownership FROM node_children",
                        conn,
                        tx)
                let! childReader = childCmd.ExecuteReaderAsync()
                match readChildren childReader [] with
                | Error e ->
                    do! childReader.DisposeAsync()
                    tx.Rollback()
                    return Error e
                | Ok children ->
                    do! childReader.DisposeAsync()
                    match
                        ProjectionOwnershipRepair.plan rootId protectedIds nodes children
                    with
                    | Error e ->
                        tx.Rollback()
                        return Error e
                    | Ok plan when ProjectionOwnershipRepair.isNoOp plan ->
                        tx.Commit()
                        return
                            Ok
                                { deletedIds = []
                                  requiresReload = false
                                  logFacts = plan.logFacts }
                    | Ok plan ->
                        match! applyPlan conn tx rootId plan with
                        | Error e ->
                            tx.Rollback()
                            return Error e
                        | Ok () ->
                            tx.Commit()
                            return
                                Ok
                                    { deletedIds = plan.deleteNodeIds
                                      requiresReload = true
                                      logFacts = plan.logFacts }
            with ex ->
                try
                    tx.Rollback()
                with _ ->
                    ()
                return Error ex.Message
        }

