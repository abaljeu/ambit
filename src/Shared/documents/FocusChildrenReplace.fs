namespace Gambol.Shared

open System

/// Plan Ops that replace every current Focus Child from a complete reply.
[<RequireQualifiedAccess>]
module FocusChildrenReplace =

    [<Literal>]
    let private AmbReplyPath = "__reply__.amb"

    let private stubRoot (rootId: NodeId) (name: string) : Graph =
        let graph0 = Graph.create ()
        let file =
            Node.Create(
                rootId,
                text = name,
                name = Filename.create name,
                owner = graph0.root,
                kind = Special File,
                documentState = Unparsed)
        Graph.fromNodes
            graph0.root
            (graph0.nodes |> Map.add rootId file)
            (graph0.childMap |> Map.add rootId [])

    let private peelFrom (stub: Graph) rootId after =
        DocumentColdParse.planOpsFromGraphs stub rootId after
        |> DocumentColdParse.peelDocumentRootOps rootId

    let private parseAmb text =
        let rootId = NodeId.New()
        let stub = stubRoot rootId AmbReplyPath
        DocumentColdParse.planApplyCold stub rootId AmbReplyPath text
        |> Result.map (DocumentColdParse.peelDocumentRootOps rootId)

    let private graphFromRead
        (stub: Graph)
        (nodes: Map<NodeId, Node>)
        (childMap: Map<NodeId, ChildNode list>)
        =
        let merged =
            stub.nodes
            |> Map.fold
                (fun acc id node ->
                    if Map.containsKey id acc then acc
                    else Map.add id node acc)
                nodes
        let mergedChildMap =
            stub.childMap
            |> Map.fold
                (fun acc id kids ->
                    if Map.containsKey id acc then acc
                    else Map.add id kids acc)
                childMap
        Graph.fromNodes stub.root merged mergedChildMap

    let private parsePlain text =
        let rootId = NodeId.New()
        let stub: Graph = stubRoot rootId DocumentColdParse.PasteRelativePath
        PlainTextDocument.read text rootId stub
        |> Result.map (fun read ->
            peelFrom stub rootId (graphFromRead stub read.nodes read.childMap))

    let private parseReply text =
        match parseAmb text with
        | Ok parsed -> Ok parsed
        | Error _ -> parsePlain text

    let private currentChildren (graph: Graph) focusId =
        match Map.tryFind focusId graph.nodes with
        | None -> Error "focus not found"
        | Some _ -> Ok (GraphChildren.get graph focusId)

    let private replaceOps focusId oldChildren topIds nested =
        let newEdges = ChildNode.owners topIds
        nested @ [ ChildListWire.replace focusId oldChildren newEdges ]

    /// Empty success clears all Focus Children. Structural Amb, else Plain.
    let plan
        (graph: Graph)
        (focusId: NodeId)
        (reply: string)
        : Result<Op list, string> =
        match currentChildren graph focusId with
        | Error err -> Error err
        | Ok oldChildren when String.IsNullOrWhiteSpace reply ->
            Ok [ ChildListWire.replace focusId oldChildren [] ]
        | Ok oldChildren ->
            match parseReply reply with
            | Error err -> Error err
            | Ok (topIds, nested) ->
                Ok (replaceOps focusId oldChildren topIds nested)
