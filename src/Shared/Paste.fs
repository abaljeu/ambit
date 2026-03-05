module Gambol.Shared.Paste

open System
open Gambol.Shared

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
        |> List.map (fun (parentId, childIds) -> Op.Replace(parentId, 0, [], childIds))
    topLevel, newNodeOps @ replaceOps

// ---------------------------------------------------------------------------
// Copy / Cut clipboard support
// ---------------------------------------------------------------------------

/// Build a NodeId → SiteNode lookup (first occurrence per nodeId, preorder).
let private buildSiteLookup (siteRoot: SiteNode) : Map<NodeId, SiteNode> =
    let rec collect (acc: Map<NodeId, SiteNode>) (sn: SiteNode) =
        let acc = if acc.ContainsKey sn.nodeId then acc else acc |> Map.add sn.nodeId sn
        sn.children |> List.fold collect acc
    collect Map.empty siteRoot

/// Serialize selected nodes and their visible (unfolded) children as tab-indented
/// text, identical to the snapshot format.  Called synchronously in the copy/cut
/// event handler so the result can be written to e.clipboardData before returning.
let serializeSubtree (graph: Graph) (siteRoot: SiteNode) (topLevelIds: NodeId list) : string =
    let lookup = buildSiteLookup siteRoot
    let sb = Text.StringBuilder()
    let nl = Environment.NewLine
    let rec walk (depth: int) (nodeId: NodeId) =
        let node = graph.nodes.[nodeId]
        sb.Append(String.replicate depth "\t").Append(node.text).Append(nl) |> ignore
        match lookup |> Map.tryFind nodeId with
        | Some sn when sn.expanded ->
            sn.children |> List.iter (fun child -> walk (depth + 1) child.nodeId)
        | _ -> ()
    topLevelIds |> List.iter (walk 0)
    sb.ToString()

/// Collect selected nodes and their visible children into a self-contained
/// ClipboardContent.  Each node's children list is trimmed to only the visible
/// (unfolded) children.  Called in update; stored in model.clipboard for Phase 3.
let collectSubtree (graph: Graph) (siteRoot: SiteNode) (topLevelIds: NodeId list) : ClipboardContent =
    let lookup = buildSiteLookup siteRoot
    let rec walk (acc: Map<NodeId, Node>) (nodeId: NodeId) (visibleChildren: SiteNode list) =
        let node = graph.nodes.[nodeId]
        let visibleChildIds = visibleChildren |> List.map (fun sn -> sn.nodeId)
        let acc = acc |> Map.add nodeId { node with children = visibleChildIds }
        visibleChildren |> List.fold (fun acc childSn ->
            let grandchildren = if childSn.expanded then childSn.children else []
            walk acc childSn.nodeId grandchildren) acc
    let nodes =
        topLevelIds |> List.fold (fun acc topId ->
            match lookup |> Map.tryFind topId with
            | Some topSn ->
                let children = if topSn.expanded then topSn.children else []
                walk acc topId children
            | None ->
                acc |> Map.add topId { graph.nodes.[topId] with children = [] }
        ) Map.empty
    { topLevelIds = topLevelIds; nodes = nodes }

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
            else Some (Op.Replace(mapId oldId, 0, [], node.children |> List.map mapId)))
    newTopLevelIds, newNodeOps @ replaceOps
