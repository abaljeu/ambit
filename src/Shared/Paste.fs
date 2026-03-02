module Gambol.Shared.Paste

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
        | (text, depth) :: rest ->
            if text = "" && lastWasBlank then collapse acc true rest
            else collapse ((text, depth) :: acc) (text = "") rest
    collapse [] false parsed

/// Turn a list of (text, depth) pairs into NewNode + Replace ops that wire
/// the nodes into a subtree.  Returns (topLevelIds, ops).
let buildPasteOps (entries: (string * int) list) : NodeId list * Op list =
    let withIds = entries |> List.map (fun (text, depth) -> (NodeId.New(), text, depth))
    // Map each nodeId -> its ordered children
    let mutable childrenMap : Map<NodeId, NodeId list> = Map.empty
    let mutable topLevel : NodeId list = []
    // Stack of (depth, nodeId) — nearest ancestor at head
    let mutable stack : (int * NodeId) list = []
    for (nodeId, _, depth) in withIds do
        // Pop ancestors that are at the same depth or deeper
        while (match stack with | (d, _) :: _ -> d >= depth | [] -> false) do
            stack <- List.tail stack
        match stack with
        | (_, parentId) :: _ ->
            let existing = childrenMap |> Map.tryFind parentId |> Option.defaultValue []
            childrenMap <- childrenMap |> Map.add parentId (existing @ [nodeId])
        | [] -> topLevel <- topLevel @ [nodeId]
        stack <- (depth, nodeId) :: stack
    let newNodeOps = withIds |> List.map (fun (id, text, _) -> Op.NewNode(id, text))
    let replaceOps =
        childrenMap
        |> Map.toList
        |> List.map (fun (parentId, childIds) -> Op.Replace(parentId, 0, [], childIds))
    topLevel, newNodeOps @ replaceOps
