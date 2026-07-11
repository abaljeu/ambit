module LazyLoadReconciliationTests

open Gambol.Shared
open Xunit

let private applyOps (graph: Graph) (ops: Op list) : Graph =
    let state = { graph = graph; history = History.empty; revision = Revision.Zero }
    ops
    |> List.fold (fun s op ->
        match Op.apply op s with
        | ApplyResult.Changed next
        | ApplyResult.Unchanged next -> next
        | ApplyResult.Invalid(_, msg) -> failwith msg) state
    |> fun s -> s.graph

let private addWorkspace label graph =
    let id, ops = FileNodeOps.planCreateWorkspace graph label
    id, applyOps graph ops

let private ownedNamedChildren graph parentId =
    graph.nodes.[parentId].children
    |> List.choose (fun child ->
        if child.ref <> Ownership.Owner then None
        else
            let node = graph.nodes.[child.id]
            Filename.tryValue node.name
            |> Option.map (fun name -> name, node))

let private requirePlan graph label paths =
    match LazyLoadReconciliation.planAddedPaths graph label paths with
    | Ok ops -> ops
    | Error err -> failwith err

[<Fact>]
let ``one added file creates a file stub`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = requirePlan graph "home" [ "README.md" ] |> applyOps graph
    let children = ownedNamedChildren graph2 workspaceId
    Assert.Equal(1, children.Length)
    Assert.Equal("README.md", fst children.Head)
    Assert.Equal(Special File, (snd children.Head).kind)
    Assert.Empty((snd children.Head).children)

[<Fact>]
let ``nested added path creates missing directory stubs`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = requirePlan graph "home" [ "src/lib/core.fs" ] |> applyOps graph
    let src = ownedNamedChildren graph2 workspaceId |> List.find (fst >> (=) "src") |> snd
    let lib = ownedNamedChildren graph2 src.id |> List.find (fst >> (=) "lib") |> snd
    let file = ownedNamedChildren graph2 lib.id |> List.find (fst >> (=) "core.fs") |> snd
    Assert.Equal(Special Directory, src.kind)
    Assert.Equal(Special Directory, lib.kind)
    Assert.Equal(Special File, file.kind)

[<Fact>]
let ``repeated reconciliation reuses matching stubs`` () =
    let _, graph = Graph.create () |> addWorkspace "home"
    let graph2 = requirePlan graph "home" [ "src/core.fs" ] |> applyOps graph
    let second = requirePlan graph2 "home" [ "src/core.fs"; "src/core.fs" ]
    Assert.Empty(second)

[<Fact>]
let ``amb suffixes are ordinary file names`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let paths = [ "x.amb"; "notes.amb.txt" ]
    let graph2 = requirePlan graph "home" paths |> applyOps graph
    let children = ownedNamedChildren graph2 workspaceId
    let xAmb = children |> List.find (fst >> (=) "x.amb") |> snd
    let notesAmbTxt = children |> List.find (fst >> (=) "notes.amb.txt") |> snd
    Assert.Equal(Special File, xAmb.kind)
    Assert.Equal(Special File, notesAmbTxt.kind)

[<Fact>]
let ``exact amb marker represents its containing directory`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let paths = [ ".git/config"; ".amb"; "docs/.amb" ]
    let graph2 = requirePlan graph "home" paths |> applyOps graph
    let children = ownedNamedChildren graph2 workspaceId
    Assert.DoesNotContain(children, fun (name, _) -> name = ".git" || name = ".amb")
    let docs = children |> List.find (fst >> (=) "docs") |> snd
    Assert.Equal(Special Directory, docs.kind)
    Assert.DoesNotContain(
        ownedNamedChildren graph2 docs.id,
        fun (name, _) -> name = ".amb")

[<Fact>]
let ``workspace label scopes reconciliation`` () =
    let homeId, graph1 = Graph.create () |> addWorkspace "home"
    let workId, graph2 = graph1 |> addWorkspace "work"
    let graph3 = requirePlan graph2 "work" [ "src/main.fs" ] |> applyOps graph2
    Assert.Empty(ownedNamedChildren graph3 homeId)
    Assert.Single(ownedNamedChildren graph3 workId) |> ignore

[<Fact>]
let ``kind conflict at an existing path returns error`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let _, fileOps = FileNodeOps.planCreateOwnedFile graph workspaceId "src"
    let graph2 = applyOps graph fileOps
    match LazyLoadReconciliation.planAddedPaths graph2 "home" [ "src/main.fs" ] with
    | Ok _ -> Assert.Fail("expected kind conflict")
    | Error err ->
        Assert.Contains("src", err)
        Assert.Contains("kind conflict", err)
