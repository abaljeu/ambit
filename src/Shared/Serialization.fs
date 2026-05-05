namespace Gambol.Shared

open Thoth.Json.Core
open Thoth.Json.JavaScript


/// Response from GET /{file}/poll — { r, b, p, c }.
/// `changes` is empty when client is up-to-date; populated with the tail when client is behind.
type PollResponse =
    { revision: int
      buildEpochSec: int
      pageBuildEpochSec: int
      changes: Change list }

type ChangeBatch =
    { changes: Change list }

type ChangeBatchAck =
    { revision: Revision
      ackedChangeIds: System.Guid list }

[<RequireQualifiedAccess>]
module Serialization =
    let private encodeSpecialKind (kind: SpecialKind) : IEncodable =
        match kind with
        | Trash -> Encode.string "trash"

    let private decodeSpecialKind: Decoder<SpecialKind> =
        Decode.string
        |> Decode.andThen (function
            | "trash" -> Decode.succeed Trash
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
            { ref = get.Required.Field "ref" decodeOwnership
              id = get.Required.Field "id" (Decode.guid |> Decode.map NodeId) })

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
              "name", Encode.lossyOption Encode.string node.name
              "children", node.children |> List.map encodeChildNode |> Encode.list
              "cssClasses", node.cssClasses |> CssClass.toList |> List.map Encode.string |> Encode.list
              "kind", encodeNodeKind node.kind ]

    let decodeNode: Decoder<Node> =
        Decode.object (fun get ->
            let kind =
                get.Optional.Field "kind" decodeNodeKind
                |> Option.defaultValue Normal
            { id = get.Required.Field "id" decodeNodeId
              text = get.Required.Field "text" Decode.string
              name = get.Optional.Field "name" Decode.string
              children = get.Required.Field "children" (Decode.list decodeChildNode)
              cssClasses = get.Optional.Field "cssClasses" (Decode.list Decode.string) |> Option.defaultValue [] |> CssClass.ofList
              owner = Graph.rootId
              kind = kind })

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
                if n.id <> Graph.rootId || n.text <> "ROOT" || n.name.IsSome
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
            | other ->
                Decode.fail $"Unknown Op type: {other}")

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

    let encodeChangeBatch (batch: ChangeBatch) : IEncodable =
        Encode.object
            [ "changes", batch.changes |> List.map encodeChange |> Encode.list ]

    let decodeChangeBatch: Decoder<ChangeBatch> =
        Decode.object (fun get ->
            { changes = get.Required.Field "changes" (Decode.list decodeChange) })
        |> Decode.andThen (fun batch ->
            if batch.changes.IsEmpty then Decode.fail "changes must not be empty"
            else Decode.succeed batch)

    let encodeChangeBatchAck (ack: ChangeBatchAck) : IEncodable =
        Encode.object
            [ "revision", encodeRevision ack.revision
              "ackedChangeIds", ack.ackedChangeIds |> List.map Encode.guid |> Encode.list ]

    let decodeChangeBatchAck: Decoder<ChangeBatchAck> =
        Decode.object (fun get ->
            { revision = get.Required.Field "revision" decodeRevision
              ackedChangeIds = get.Required.Field "ackedChangeIds" (Decode.list Decode.guid) })

    // ---- PollResponse ----
    // Defined after Change encode/decode because the response now includes a change tail.

    let encodePollResponse (r: PollResponse) : IEncodable =
        Encode.object
            [ "r", Encode.int r.revision
              "b", Encode.int r.buildEpochSec
              "p", Encode.int r.pageBuildEpochSec
              "c", r.changes |> List.map encodeChange |> Encode.list ]

    /// Decoder usable with any Thoth backend (Newtonsoft in tests, JavaScript in Fable).
    let decodePollResponseDecoder: Decoder<PollResponse> =
        Decode.object (fun get ->
            { revision = get.Required.Field "r" Decode.int
              buildEpochSec = get.Required.Field "b" Decode.int
              pageBuildEpochSec = get.Required.Field "p" Decode.int
              // Optional for backward-compat: old server responses omit "c".
              changes =
                get.Optional.Field "c" (Decode.list decodeChange)
                |> Option.defaultValue [] })

    let decodePollResponse (text: string) : Result<PollResponse, string> =
        Decode.fromString decodePollResponseDecoder text
