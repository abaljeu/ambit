module Gambol.Shared.Tests.FocusChildrenReplaceTests

open System
open Xunit
open Gambol.Shared
open GraphChildMapHelpers

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: {err}"

let private applyOps (ops: Op list) (graph: Graph) : Graph =
    ops
    |> List.fold
        (fun g op ->
            let state = { graph = g; eventId = EventId.zero }
            match Op.apply op state with
            | ApplyResult.Changed s -> s.graph
            | _ -> failwith "op failed")
        graph

let private seedFocus childTexts =
    let graph0 = Graph.create ()
    let focusId = NodeId.New()
    let childIds = childTexts |> List.map (fun _ -> NodeId.New())
    let focus = Node.Create(focusId, text = "focus")
    let childNodes =
        List.map2
            (fun id text -> Node.Create(id, text = text))
            childIds
            childTexts
    let withNodes =
        addDetachedMany (focus :: childNodes) graph0
    let attached =
        Graph.replace graph0.root 0 [] [ ChildNode.owner focusId ] withNodes
        |> requireOk "attach focus"
    let withChildren =
        Graph.replace focusId 0 [] (ChildNode.owners childIds) attached
        |> requireOk "attach children"
    focusId, withChildren

let private ownedTexts (graph: Graph) focusId =
    Graph.children graph focusId
    |> List.filter (fun child -> child.ref = Ownership.Owner)
    |> List.map (fun child -> graph.nodes.[child.id].text)

let private firstOwned (graph: Graph) parentId =
    Graph.children graph parentId
    |> List.tryFind (fun child -> child.ref = Ownership.Owner)
    |> Option.map (fun child -> child.id)

[<Fact>]
let ``empty reply clears every Focus Child`` () =
    let focusId, graph = seedFocus [ "old-a"; "old-b" ]
    let ops =
        FocusChildrenReplace.plan graph focusId ""
        |> requireOk "plan empty"
    let after = applyOps ops graph
    Assert.Empty(ownedTexts after focusId)

[<Fact>]
let ``structural Amb reply replaces Focus Children`` () =
    let focusId, graph = seedFocus [ "old" ]
    let reply = "alpha" + Environment.NewLine + "\tbeta"
    let ops =
        FocusChildrenReplace.plan graph focusId reply
        |> requireOk "plan amb"
    let after = applyOps ops graph
    Assert.Equal<string list>([ "alpha" ], ownedTexts after focusId)
    match firstOwned after focusId with
    | None -> Assert.Fail("missing alpha")
    | Some alphaId ->
        Assert.Equal<string list>([ "beta" ], ownedTexts after alphaId)

[<Fact>]
let ``invalid Amb falls back to Plain and does not keep a partial tree`` () =
    let focusId, graph = seedFocus [ "old" ]
    let reply = "-> ^not-a-valid-id"
    let ops =
        FocusChildrenReplace.plan graph focusId reply
        |> requireOk "plan plain fallback"
    let after = applyOps ops graph
    Assert.Equal<string list>([ reply ], ownedTexts after focusId)
