module SpecialNodeTestHelpers

open System
open Gambol.Shared

let eventOps (event: Ev) =
    Ev.ops event |> Option.defaultValue []

let changeEvent commandName (id: EventId) submissionId ops : Ev =
    { id = id
      submissionId = submissionId
      authority = Authority "Browser"
      commandName = commandName
      body = EventBody.Change ops }

let changeEventZero commandName ops =
    changeEvent commandName EventId.zero (Guid.NewGuid()) ops

let applyChange (event: Ev) (state: State) =
    ChangeValidation.applyOps (eventOps event) state

let inverseEvent (id: EventId) submissionId (event: Ev) : Ev =
    { event with
        id = id
        submissionId = submissionId
        body =
            EventBody.Change(
                Ev.inverseOps event |> Option.defaultValue []) }

/// Predicate: true for nodes that are not special (i.e. Normal).
let isUserNode (node: Node) =
    match node.kind with
    | Normal -> true
    | Special _ -> false

/// Count of non-special nodes.
let userNodeCount (graph: Graph) =
    graph.nodes
    |> Map.toSeq
    |> Seq.map snd
    |> Seq.filter isUserNode
    |> Seq.length

/// Root children excluding any special nodes.
let userRootChildren (graph: Graph) =
    Graph.children graph graph.root
    |> List.filter (fun c -> graph.nodes.[c.id] |> isUserNode)

/// Tree shape (depth, text) excluding special nodes entirely.
let userTreeShape (graph: Graph) : (int * string) list =
    let rec walk depth nodeId =
        let node = graph.nodes.[nodeId]
        if isUserNode node then
            (depth, node.text)
            :: (Graph.children graph nodeId
                |> List.collect (fun child -> walk (depth + 1) child.id))
        else
            []

    Graph.children graph graph.root
    |> List.collect (fun child -> walk 0 child.id)

/// Strip TRASH-related lines from a snapshot outline string for legacy tests.
let stripSpecialLinesFromOutline (text: string) : string =
    let filtered =
        text.Split(System.Environment.NewLine)
        |> Array.toList
        |> List.filter (fun line ->
            let trimmed = line.Trim()
            not (
                trimmed = "#WORKSPACES Workspaces"
                || trimmed = "-> #WORKSPACES"
                || trimmed = "#SYSTEM System"
                || trimmed = "#SYSTEM SYSTEM"
                || trimmed = "-> #SYSTEM"
                || trimmed = "#TRASH Trash"
                || trimmed = "#TRASH TRASH"
                || trimmed = "-> #TRASH"
            ))
        |> List.rev
        |> List.skipWhile (fun l -> l = "")
        |> List.rev
    let s = filtered |> String.concat System.Environment.NewLine
    if text.EndsWith(System.Environment.NewLine) && s <> "" then
        s + System.Environment.NewLine
    else
        s

