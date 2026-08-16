namespace Gambol.Shared

open System
open Thoth.Json.Core
open Thoth.Json.JavaScript


type ChangeBatch =
    { changes: ChangeRequest list }

type ChangeBatchAck =
    { revision: Revision
      ackedChangeIds: System.Guid list
      /// Disk mtime stamps after persist; empty when graph-only / no disk write.
      stampOps: Op list
      /// File-write status when graph change succeeded but artifact save had issues.
      message: string option }

[<RequireQualifiedAccess>]
module Serialization =
    let private encodeDocumentState (state: DocumentState) : IEncodable =
        match state with
        | Current -> Encode.string "current"
        | Unparsed -> Encode.string "unparsed"
        | NoServerFile -> Encode.string "noServerFile"

    let private decodeDocumentState: Decoder<DocumentState> =
        Decode.string
        |> Decode.andThen (function
            | "current" -> Decode.succeed Current
            | "unparsed" -> Decode.succeed Unparsed
            | "noServerFile" -> Decode.succeed NoServerFile
            | other -> Decode.fail $"Unknown document state: {other}")

    let private encodeChildrenStatus (status: ChildrenStatus) : IEncodable =
        match status with
        | Loaded -> Encode.string "loaded"
        | Unloaded -> Encode.string "unloaded"

    let private decodeChildrenStatus: Decoder<ChildrenStatus> =
        Decode.string
        |> Decode.andThen (function
            | "loaded" -> Decode.succeed Loaded
            | "unloaded" -> Decode.succeed Unloaded
            | other -> Decode.fail $"Unknown children status: {other}")

    let private encodeSpecialKind (kind: SpecialKind) : IEncodable =
        match kind with
        | Workspaces -> Encode.string "workspaces"
        | Workspace -> Encode.string "workspace"
        | Directory -> Encode.string "directory"
        | File -> Encode.string "file"

    let private decodeSpecialKind: Decoder<SpecialKind> =
        Decode.string
        |> Decode.andThen (function
            | "workspaces" -> Decode.succeed Workspaces
            | "workspace" -> Decode.succeed Workspace
            | "directory" -> Decode.succeed Directory
            | "file" -> Decode.succeed File
            | "trash" -> Decode.succeed Directory
            | other -> Decode.fail $"Unknown special node kind: {other}")

    let private encodeNodeKind (kind: NodeKind) : IEncodable =
        match kind with
        | Normal -> Encode.string "normal"
        | Special sk ->
            Encode.object
                [ "type", Encode.string "special"
                  "kind", encodeSpecialKind sk ]

    let private decodeNodeKind: Decoder<NodeKind> =
        Decode.oneOf
            [ Decode.string
              |> Decode.andThen (function
                  | "normal" -> Decode.succeed Normal
                  | other -> Decode.fail $"Unknown node kind: {other}")
              Decode.object (fun get ->
                  match get.Required.Field "type" Decode.string with
                  | "special" ->
                      let sk = get.Required.Field "kind" decodeSpecialKind
                      Special sk
                  | other ->
                      failwithf "Unknown node kind type discriminator: %s" other) ]
    let private encodeOwnership (ownership: Ownership) : IEncodable =
        match ownership with
        | Ownership.Ref -> Encode.string "ref"
        | Ownership.Owner -> Encode.string "owner"

    let private decodeOwnership: Decoder<Ownership> =
        Decode.string
        |> Decode.andThen (function
            | "ref" -> Decode.succeed Ownership.Ref
            | "owner" -> Decode.succeed Ownership.Owner
            | other -> Decode.fail $"Unknown ownership: {other}")

    let encodeChildNode (child: ChildNode) : IEncodable =
        Encode.object
            [ "ref", encodeOwnership child.ref
              "id", Encode.guid child.id.Value ]

    let decodeChildNode: Decoder<ChildNode> =
        Decode.object (fun get ->
            ChildNode.ofOwnership
                (get.Required.Field "ref" decodeOwnership)
                (get.Required.Field "id" (Decode.guid |> Decode.map NodeId)))

    // ---- NodeId ----

    let encodeNodeId (nodeId: NodeId) : IEncodable =
        Encode.guid nodeId.Value

    let decodeNodeId: Decoder<NodeId> =
        Decode.guid |> Decode.map NodeId

    // ---- Revision ----

    let encodeRevision (rev: Revision) : IEncodable =
        Encode.int rev.Value

    let decodeRevision: Decoder<Revision> =
        Decode.int |> Decode.map Revision

    // ---- Node ----

    let encodeNode (node: Node) : IEncodable =
        Encode.object
            [ "id", encodeNodeId node.id
              "text", Encode.string node.text
              "name", Encode.lossyOption Encode.string (Filename.tryValue node.name)
              "children", node.children |> List.map encodeChildNode |> Encode.list
              "childrenStatus", encodeChildrenStatus node.childrenStatus
              "cssClasses", node.cssClasses |> CssClass.toList |> List.map Encode.string |> Encode.list
              "kind", encodeNodeKind node.kind
              "documentState", encodeDocumentState node.documentState
              "updateTime", Encode.int64 node.updateTime.Ticks ]

    let decodeNode: Decoder<Node> =
        Decode.object (fun get ->
            let kind =
                get.Optional.Field "kind" decodeNodeKind
                |> Option.defaultValue Normal
            let name =
                get.Optional.Field "name" Decode.string
                |> Option.map Filename.create
                |> Option.defaultValue Filename.Empty
            let cssClasses =
                get.Optional.Field "cssClasses" (Decode.list Decode.string)
                |> Option.defaultValue []
                |> CssClass.ofList
            let updateTime =
                get.Optional.Field "updateTime" Decode.int64
                |> Option.map (fun ticks -> DateTime(ticks, DateTimeKind.Utc))
                |> Option.defaultValue NodeUpdateTime.missing
            let documentState =
                get.Optional.Field "documentState" decodeDocumentState
                |> Option.defaultValue Current
            let children = get.Required.Field "children" (Decode.list decodeChildNode)
            let childrenStatus =
                get.Optional.Field "childrenStatus" decodeChildrenStatus
                |> Option.defaultValue Loaded
            get.Required.Field "id" decodeNodeId,
            get.Required.Field "text" Decode.string,
            name,
            children,
            childrenStatus,
            cssClasses,
            kind,
            documentState,
            updateTime)
        |> Decode.andThen (fun (id, text, name, children, childrenStatus, cssClasses, kind, documentState, updateTime) ->
            match childrenStatus, children with
            | Unloaded, _ :: _ ->
                Decode.fail "Unloaded childrenStatus requires empty children"
            | _ ->
                Decode.succeed (
                    Node.Create(
                        id,
                        text = text,
                        name = name,
                        children = children,
                        childrenStatus = childrenStatus,
                        cssClasses = cssClasses,
                        kind = kind,
                        documentState = documentState,
                        updateTime = updateTime)))

    // ---- Graph ----

    let encodeGraph (graph: Graph) : IEncodable =
        let nodeList =
            graph.nodes |> Map.toList |> List.map (snd >> encodeNode)

        Encode.object
            [ "root", encodeNodeId graph.root
              "nodes", Encode.list nodeList ]

    let decodeGraph: Decoder<Graph> =
        Decode.object (fun get ->
            let root = get.Required.Field "root" decodeNodeId
            let nodeList = get.Required.Field "nodes" (Decode.list decodeNode)
            let nodes = nodeList |> List.map (fun n -> n.id, n) |> Map.ofList
            Graph.fromNodes root nodes)
        |> Decode.andThen (fun g ->
            if g.root <> Graph.rootId then
                Decode.fail "graph root id must be canonical"
            elif not (Map.containsKey Graph.rootId g.nodes) then
                Decode.fail "graph missing canonical root node"
            else
                let n = g.nodes.[Graph.rootId]
                if n.id <> Graph.rootId || n.text <> "ROOT" || n.name <> Filename.Empty
                   || n.cssClasses <> CssClass.empty then
                    Decode.fail "canonical root node has wrong shape"
                else
                    Decode.succeed g)

    // ---- Op ----

    let encodeOp (op: Op) : IEncodable =
        match op with
        | Op.NewNode(nodeId, text) ->
            Encode.object
                [ "type", Encode.string "NewNode"
                  "nodeId", encodeNodeId nodeId
                  "text", Encode.string text ]
        | Op.SetText(nodeId, oldText, newText) ->
            Encode.object
                [ "type", Encode.string "SetText"
                  "nodeId", encodeNodeId nodeId
                  "oldText", Encode.string oldText
                  "newText", Encode.string newText ]
        | Op.SetClasses(nodeId, oldClasses, newClasses) ->
            Encode.object
                [ "type", Encode.string "SetClasses"
                  "nodeId", encodeNodeId nodeId
                  "oldClasses", oldClasses |> CssClass.toList |> List.map Encode.string |> Encode.list
                  "newClasses", newClasses |> CssClass.toList |> List.map Encode.string |> Encode.list ]
        | Op.Replace(parentId, index, oldChildren, newChildren) ->
            Encode.object
                [ "type", Encode.string "Replace"
                  "parentId", encodeNodeId parentId
                  "index", Encode.int index
                  "oldChildren", oldChildren |> List.map encodeChildNode |> Encode.list
                  "newChildren", newChildren |> List.map encodeChildNode |> Encode.list ]
        | Op.NewSpecialNode(nodeId, kind, name) ->
            Encode.object
                [ "type", Encode.string "NewSpecialNode"
                  "nodeId", encodeNodeId nodeId
                  "kind", encodeSpecialKind kind
                  "name", Encode.string name ]
        | Op.SetName(nodeId, oldName, newName) ->
            Encode.object
                [ "type", Encode.string "SetName"
                  "nodeId", encodeNodeId nodeId
                  "oldName", Encode.string oldName
                  "newName", Encode.string newName ]
        | Op.SetDocumentState(nodeId, oldState, newState) ->
            Encode.object
                [ "type", Encode.string "SetDocumentState"
                  "nodeId", encodeNodeId nodeId
                  "oldState", encodeDocumentState oldState
                  "newState", encodeDocumentState newState ]
        | Op.SetUpdateTime(nodeId, oldTime, newTime) ->
            Encode.object
                [ "type", Encode.string "SetUpdateTime"
                  "nodeId", encodeNodeId nodeId
                  "oldTime", Encode.int64 oldTime.Ticks
                  "newTime", Encode.int64 newTime.Ticks ]

    let decodeOp: Decoder<Op> =
        Decode.field "type" Decode.string
        |> Decode.andThen (fun opType ->
            match opType with
            | "NewNode" ->
                Decode.object (fun get ->
                    Op.NewNode(
                        get.Required.Field "nodeId" decodeNodeId,
                        get.Required.Field "text" Decode.string))
            | "SetText" ->
                Decode.object (fun get ->
                    Op.SetText(
                        get.Required.Field "nodeId" decodeNodeId,
                        get.Required.Field "oldText" Decode.string,
                        get.Required.Field "newText" Decode.string))
            | "SetClasses" ->
                Decode.object (fun get ->
                    Op.SetClasses(
                        get.Required.Field "nodeId" decodeNodeId,
                        get.Required.Field "oldClasses" (Decode.list Decode.string) |> CssClass.ofList,
                        get.Required.Field "newClasses" (Decode.list Decode.string) |> CssClass.ofList))
            | "Replace" ->
                Decode.object (fun get ->
                    Op.Replace(
                        get.Required.Field "parentId" decodeNodeId,
                        get.Required.Field "index" Decode.int,
                        get.Required.Field "oldChildren" (Decode.list decodeChildNode),
                        get.Required.Field "newChildren" (Decode.list decodeChildNode)))
            | "NewSpecialNode" ->
                Decode.object (fun get ->
                    Op.NewSpecialNode(
                        get.Required.Field "nodeId" decodeNodeId,
                        get.Required.Field "kind" decodeSpecialKind,
                        get.Required.Field "name" Decode.string))
            | "SetName" ->
                Decode.object (fun get ->
                    Op.SetName(
                        get.Required.Field "nodeId" decodeNodeId,
                        get.Required.Field "oldName" Decode.string,
                        get.Required.Field "newName" Decode.string))
            | "SetDocumentState" ->
                Decode.object (fun get ->
                    Op.SetDocumentState(
                        get.Required.Field "nodeId" decodeNodeId,
                        get.Required.Field "oldState" decodeDocumentState,
                        get.Required.Field "newState" decodeDocumentState))
            | "SetUpdateTime" ->
                Decode.object (fun get ->
                    Op.SetUpdateTime(
                        get.Required.Field "nodeId" decodeNodeId,
                        DateTime(
                            get.Required.Field "oldTime" Decode.int64,
                            DateTimeKind.Utc),
                        DateTime(
                            get.Required.Field "newTime" Decode.int64,
                            DateTimeKind.Utc)))
            | other ->
                Decode.fail $"Unknown Op type: {other}")

    // ---- Desktop Import ----

    let encodeDesktopImportPackage (package: DesktopImportPackage) : IEncodable =
        Encode.object
            [ "sourcePath", Encode.string package.sourcePath
              "isDirectory", Encode.bool package.isDirectory
              "topLevelIds", package.topLevelIds |> List.map encodeNodeId |> Encode.list
              "ops", package.ops |> List.map encodeOp |> Encode.list ]

    let decodeDesktopImportPackage: Decoder<DesktopImportPackage> =
        Decode.object (fun get ->
            { sourcePath = get.Required.Field "sourcePath" Decode.string
              isDirectory =
                get.Optional.Field "isDirectory" Decode.bool
                |> Option.defaultValue false
              topLevelIds = get.Required.Field "topLevelIds" (Decode.list decodeNodeId)
              ops = get.Required.Field "ops" (Decode.list decodeOp) })

    // ---- Desktop Export ----

    let encodeDesktopExportRequest (request: DesktopExportRequest) : IEncodable =
        Encode.object
            [ "path", Encode.string request.path
              "content", Encode.string request.content ]

    let decodeDesktopExportRequest: Decoder<DesktopExportRequest> =
        Decode.object (fun get ->
            { path = get.Required.Field "path" Decode.string
              content = get.Required.Field "content" Decode.string })

    let encodeDesktopExportResponse (response: DesktopExportResponse) : IEncodable =
        Encode.object [ "path", Encode.string response.path ]

    let decodeDesktopExportResponse: Decoder<DesktopExportResponse> =
        Decode.object (fun get -> { path = get.Required.Field "path" Decode.string })

    let private encodeDesktopFileStatus (status: DesktopFileStatus) : IEncodable =
        status |> NodeStatus.label |> Encode.string

    let private decodeDesktopFileStatus: Decoder<DesktopFileStatus> =
        Decode.string
        |> Decode.andThen (fun text ->
            match NodeStatus.tryParse text with
            | Some status -> Decode.succeed status
            | None -> Decode.fail $"Unknown desktop file status: {text}")

    let encodeDesktopFileStatusResponse (response: DesktopFileStatusResponse) : IEncodable =
        let fields =
            [ "path", Encode.string response.path
              "status", encodeDesktopFileStatus response.status ]

        let fields =
            match response.sourceModifiedUtc with
            | None -> fields
            | Some t ->
                fields
                @ [ "sourceModifiedUtc", Encode.int64 (t.ToUniversalTime().Ticks) ]

        Encode.object fields

    let decodeDesktopFileStatusResponse: Decoder<DesktopFileStatusResponse> =
        Decode.object (fun get ->
            { path = get.Required.Field "path" Decode.string
              status = get.Required.Field "status" decodeDesktopFileStatus
              sourceModifiedUtc =
                  get.Optional.Field "sourceModifiedUtc" Decode.int64
                  |> Option.map (fun ticks -> System.DateTime(ticks, System.DateTimeKind.Utc)) })

    // ---- Change ----

    let encodeChange (change: Change) : IEncodable =
        Encode.object
            [ "id", Encode.int change.id
              "changeId", Encode.guid change.changeId
              "ops", change.ops |> List.map encodeOp |> Encode.list ]

    let decodeChange: Decoder<Change> =
        Decode.object (fun get ->
            { id = get.Required.Field "id" Decode.int
              // Optional for backward-compat with existing log entries written before this field was added.
              changeId =
                get.Optional.Field "changeId" Decode.guid
                |> Option.defaultWith System.Guid.NewGuid
              ops = get.Required.Field "ops" (Decode.list decodeOp) })

    let private encodePendingKind =
        function
        | PendingKind.Normal -> Encode.string "normal"
        | PendingKind.Undo -> Encode.string "undo"
        | PendingKind.Redo -> Encode.string "redo"

    let private decodePendingKind: Decoder<PendingKind> =
        Decode.string
        |> Decode.andThen (function
            | "normal" -> Decode.succeed PendingKind.Normal
            | "undo" -> Decode.succeed PendingKind.Undo
            | "redo" -> Decode.succeed PendingKind.Redo
            | other -> Decode.fail ("Unknown pending kind: " + other))

    let private encodePendingTransition (transition: PendingTransition) : IEncodable =
        Encode.object
            [ "recordId", Encode.int transition.recordId
              "submittedChangeId", Encode.guid transition.submittedChangeId
              "kind", encodePendingKind transition.kind ]

    let private decodePendingTransition: Decoder<PendingTransition> =
        Decode.object (fun get ->
            { recordId = get.Required.Field "recordId" Decode.int
              submittedChangeId = get.Required.Field "submittedChangeId" Decode.guid
              kind = get.Required.Field "kind" decodePendingKind })

    let encodePendingChange (item: PendingChange) : IEncodable =
        Encode.object (
            [ "change", encodeChange item.change ]
            @ match item.transition with
              | None -> []
              | Some transition ->
                  [ "transition", encodePendingTransition transition ])

    let decodePendingChange: Decoder<PendingChange> =
        Decode.object (fun get ->
            { change = get.Required.Field "change" decodeChange
              transition = get.Optional.Field "transition" decodePendingTransition })

    let encodeChangeRequest (action: ChangeRequest) : IEncodable =
        match action with
        | ChangeRequest.Change change -> encodeChange change
        | ChangeRequest.Undo(id, changeId) ->
            Encode.object
                [ "action", Encode.string "undo"
                  "id", Encode.int id
                  "changeId", Encode.guid changeId ]
        | ChangeRequest.Redo(id, changeId) ->
            Encode.object
                [ "action", Encode.string "redo"
                  "id", Encode.int id
                  "changeId", Encode.guid changeId ]

    let decodeChangeRequest: Decoder<ChangeRequest> =
        Decode.object (fun get ->
            get.Optional.Field "action" Decode.string,
            get.Required.Field "id" Decode.int,
            get.Optional.Field "changeId" Decode.guid,
            get.Optional.Field "ops" (Decode.list decodeOp))
        |> Decode.andThen (fun (kind, id, changeId, ops) ->
            match kind, ops with
            | None, Some changeOps ->
                let actionId =
                    changeId |> Option.defaultWith System.Guid.NewGuid
                Decode.succeed (
                    ChangeRequest.Change
                        { id = id
                          changeId = actionId
                          ops = changeOps })
            | Some "undo", None when changeId.IsSome ->
                let actionId = Option.get changeId
                Decode.succeed (ChangeRequest.Undo(id, actionId))
            | Some "redo", None when changeId.IsSome ->
                let actionId = Option.get changeId
                Decode.succeed (ChangeRequest.Redo(id, actionId))
            | Some ("undo" | "redo"), None ->
                Decode.fail "History action requires changeId"
            | None, None -> Decode.fail "Change requires ops"
            | Some action, _ -> Decode.fail $"Unknown history action: {action}")

    let encodeChangeBatch (batch: ChangeBatch) : IEncodable =
        Encode.object
            [ "changes",
              batch.changes |> List.map encodeChangeRequest |> Encode.list ]

    let decodeChangeBatch: Decoder<ChangeBatch> =
        Decode.object (fun get ->
            { changes =
                get.Required.Field
                    "changes"
                    (Decode.list decodeChangeRequest) })
        |> Decode.andThen (fun batch ->
            if batch.changes.IsEmpty then Decode.fail "changes must not be empty"
            else Decode.succeed batch)

    let encodeChangeBatchAck (ack: ChangeBatchAck) : IEncodable =
        Encode.object (
            [ "revision", encodeRevision ack.revision
              "ackedChangeIds", ack.ackedChangeIds |> List.map Encode.guid |> Encode.list
              "stampOps", ack.stampOps |> List.map encodeOp |> Encode.list ]
            @ match ack.message with
              | None -> []
              | Some msg -> [ "message", Encode.string msg ]
        )

    let decodeChangeBatchAck: Decoder<ChangeBatchAck> =
        Decode.object (fun get ->
            { revision = get.Required.Field "revision" decodeRevision
              ackedChangeIds = get.Required.Field "ackedChangeIds" (Decode.list Decode.guid)
              stampOps =
                get.Optional.Field "stampOps" (Decode.list decodeOp)
                |> Option.defaultValue []
              message = get.Optional.Field "message" Decode.string })

