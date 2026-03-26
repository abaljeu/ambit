module Gambol.Shared.Paste

open System
open Gambol.Shared

let private ownedChildren (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

/// Parse clipboard plain text into (nodeText, depth) pairs.
/// Tabs at the start of a line indicate nesting depth.
/// Runs of 2+ consecutive blank lines collapse to a single blank entry.
let parsePasteText (text: string) : (string * int) list =
    let lines =
        text.Split([| "\r\n"; "\r"; "\n" |], System.StringSplitOptions.None)
        |> Array.toList
    let parsed =
        lines
        |> List.map (fun line ->
            let depth = line |> Seq.takeWhile ((=) '\t') |> Seq.length
            (line.Substring(depth), depth))
    // Collapse runs of 2+ consecutive blank lines to 1 blank
    let rec collapse acc lastWasBlank entries =
        match entries with
        | [] -> List.rev acc
        | (text, _) :: rest when text = "" && lastWasBlank -> collapse acc true rest
        | (text, depth) :: rest -> collapse ((text, depth) :: acc) (text = "") rest
    collapse [] false parsed

/// Turn a list of (text, depth) pairs into NewNode + Replace ops that wire
/// the nodes into a subtree.  Returns (topLevelIds, ops).
let buildPasteOps (entries: (string * int) list) : NodeId list * Op list =
    let withIds = entries |> List.map (fun (text, depth) -> (NodeId.New(), text, depth))
    let addChild childrenMap parentId nodeId =
        let existing = childrenMap |> Map.tryFind parentId |> Option.defaultValue []
        childrenMap |> Map.add parentId (existing @ [nodeId])
    let childrenMap, topLevel, _ =
        withIds |> List.fold (fun (childrenMap, topLevel, stack) (nodeId, _, depth) ->
            let stack = stack |> List.skipWhile (fun (d, _) -> d >= depth)
            match stack with
            | (_, parentId) :: _ ->
                addChild childrenMap parentId nodeId, topLevel, (depth, nodeId) :: stack
            | [] ->
                childrenMap, topLevel @ [nodeId], (depth, nodeId) :: stack
        ) (Map.empty, [], [])
    let newNodeOps = withIds |> List.map (fun (id, text, _) -> Op.NewNode(id, text))
    let replaceOps =
        childrenMap
        |> Map.toList
        |> List.map (fun (parentId, childIds) -> Op.Replace(parentId, 0, [], ownedChildren childIds))
    topLevel, newNodeOps @ replaceOps

// ---------------------------------------------------------------------------
// Copy / Cut clipboard support
// ---------------------------------------------------------------------------

/// Serialize selected nodes and their visible (unfolded) children as tab-indented
/// text, identical to the snapshot format.  Called synchronously in the copy/cut
/// event handler so the result can be written to e.clipboardData before returning.
let serializeSubtree (graph: Graph) (siteMap: SiteMap) (topLevelIds: NodeId list) : string =
    let occIdx = ViewModel.buildOccurrenceIndex siteMap
    let sb = Text.StringBuilder()
    let nl = Environment.NewLine
    let rec walk (depth: int) (nodeId: NodeId) =
        let node = graph.nodes.[nodeId]
        sb.Append(String.replicate depth "\t").Append(node.text).Append(nl) |> ignore
        match Map.tryFind nodeId occIdx |> Option.bind List.tryHead
              |> Option.bind (fun instId -> Map.tryFind instId siteMap.entries) with
        | Some entry when entry.expanded ->
            entry.children |> List.iter (fun childInstId ->
                walk (depth + 1) siteMap.entries.[childInstId].nodeId)
        | _ -> ()
    topLevelIds |> List.iter (walk 0)
    sb.ToString()

/// Collect selected nodes and their visible children into a self-contained
/// ClipboardContent. Receives top-level children directly from a parent.children slice.
/// Each node's children list is trimmed to only the visible (unfolded) children.
let collectSubtree (graph: Graph) (siteMap: SiteMap) 
        (topLevelChildren: ChildNode list) : ClipboardContent =
    let occIdx = ViewModel.buildOccurrenceIndex siteMap
    let findEntry nodeId =
        Map.tryFind nodeId occIdx
        |> Option.bind List.tryHead
        |> Option.bind (fun instId -> Map.tryFind instId siteMap.entries)
    let rec walk
        (acc: Map<NodeId, Node>)
        (nodeId: NodeId)
        (visibleChildren: ChildNode list)
        (visibleChildInstIds: SiteId list)
        =
        let node = graph.nodes.[nodeId]
        let acc = acc |> Map.add nodeId { node with children = visibleChildren }
        visibleChildInstIds |> List.fold (fun acc childInstId ->
            let childEntry = siteMap.entries.[childInstId]
            let grandchildren = if childEntry.expanded then childEntry.children else []
            let childNode = graph.nodes.[childEntry.nodeId]
            let visibleGrandchildren =
                if childEntry.expanded then childNode.children else []
            walk acc childEntry.nodeId visibleGrandchildren grandchildren) acc
    let nodes =
        topLevelChildren |> List.fold (fun acc topChild ->
            let topId = topChild.id
            match findEntry topId with
            | Some topEntry ->
                let children = if topEntry.expanded then topEntry.children else []
                let topNode = graph.nodes.[topId]
                let visibleChildren = if topEntry.expanded then topNode.children else []
                walk acc topId visibleChildren children
            | None ->
                acc |> Map.add topId { graph.nodes.[topId] with children = [] }
        ) Map.empty
    { topLevelIds = topLevelChildren |> List.map (fun child -> child.id); nodes = nodes }

/// Remap all NodeIds in a ClipboardContent to fresh ones, producing NewNode +
/// Replace ops that recreate the subtree with independent identities (deep copy).
let buildPasteOpsFromClipboard (clipboard: ClipboardContent) : NodeId list * Op list =
    let idMap =
        clipboard.nodes
        |> Map.toList
        |> List.map (fun (oldId, _) -> oldId, NodeId.New())
        |> Map.ofList
    let mapId oldId = idMap.[oldId]
    let newTopLevelIds = clipboard.topLevelIds |> List.map mapId
    let newNodeOps =
        clipboard.nodes
        |> Map.toList
        |> List.map (fun (oldId, node) -> Op.NewNode(mapId oldId, node.text))
    let replaceOps =
        clipboard.nodes
        |> Map.toList
        |> List.choose (fun (oldId, node) ->
            if node.children.IsEmpty then None
            else
                let remappedChildren = // not sure about this
                    node.children
                    |> List.map (fun child -> { child with id = mapId child.id })
                Some (Op.Replace(mapId oldId, 0, [], remappedChildren)))
    newTopLevelIds, newNodeOps @ replaceOps
