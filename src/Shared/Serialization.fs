namespace Gambol.Shared

open Thoth.Json.Core

[<RequireQualifiedAccess>]
module Serialization =

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
              "children", node.children |> List.map encodeNodeId |> Encode.list
              "cssClasses", node.cssClasses |> CssClass.toList |> List.map Encode.string |> Encode.list ]

    let decodeNode: Decoder<Node> =
        Decode.object (fun get ->
            { id = get.Required.Field "id" decodeNodeId
              text = get.Required.Field "text" Decode.string
              name = get.Optional.Field "name" Decode.string
              children = get.Required.Field "children" (Decode.list decodeNodeId)
              cssClasses = get.Optional.Field "cssClasses" (Decode.list Decode.string) |> Option.defaultValue [] |> CssClass.ofList })

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
            { root = root; nodes = nodes })

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
        | Op.Replace(parentId, index, oldIds, newIds) ->
            Encode.object
                [ "type", Encode.string "Replace"
                  "parentId", encodeNodeId parentId
                  "index", Encode.int index
                  "oldIds", oldIds |> List.map encodeNodeId |> Encode.list
                  "newIds", newIds |> List.map encodeNodeId |> Encode.list ]

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
                        get.Required.Field "oldIds" (Decode.list decodeNodeId),
                        get.Required.Field "newIds" (Decode.list decodeNodeId)))
            | other ->
                Decode.fail $"Unknown Op type: {other}")

    // ---- Change ----

    let encodeChange (change: Change) : IEncodable =
        Encode.object
            [ "id", Encode.int change.id
              "ops", change.ops |> List.map encodeOp |> Encode.list ]

    let decodeChange: Decoder<Change> =
        Decode.object (fun get ->
            { id = get.Required.Field "id" Decode.int
              ops = get.Required.Field "ops" (Decode.list decodeOp) })
