module FileNodeOpsTests

open Gambol.Shared
open RefExprTestTree
open Xunit

let private tree = lazy build ()

let private requireOk r =
    match r with
    | Ok v -> v
    | Error e -> failwith e

let private applyOps (graph: Graph) (ops: Op list) : Graph =
    let state = { graph = graph; history = History.empty; revision = Revision.Zero }
    ops
    |> List.fold (fun s op ->
        match Op.apply op s with
        | ApplyResult.Changed s' -> s'
        | ApplyResult.Unchanged s' -> s'
        | ApplyResult.Invalid(_, msg) -> failwith msg) state
    |> fun s -> s.graph

let private owned id = { ref = Ownership.Owner; id = id }
let private asRef id = { ref = Ownership.Ref; id = id }

let private outlineSetup () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "focus" ] graph0
    let focus = ids.[0]
    let graph2 =
        match Graph.replace graph1.root 0 [] [ owned focus ] graph1 with
        | Ok g -> g
        | Error e -> failwith e
    focus, graph2

[<Fact>]
let ``planCreateWorkspace creates workspace under Workspaces`` () =
    let graph = Graph.create ()
    let wsId, ops = FileNodeOps.planCreateWorkspace graph
    let graph2 = applyOps graph ops
    let wsNode = graph2.nodes.[wsId]
    Assert.Equal(Special Workspace, wsNode.kind)
    Assert.Equal(Filename.Ok "workspace", wsNode.name)
    Assert.True(
        graph2.nodes.[Graph.workspacesId].children
        |> List.exists (fun c -> c.id = wsId && c.ref = Ownership.Owner))

[<Fact>]
let ``planCreateOwnedFile creates file under focus parent`` () =
    let focus, graph = outlineSetup ()
    let fileId, ops = FileNodeOps.planCreateOwnedFile graph focus
    let graph2 = applyOps graph ops
    let fileNode = graph2.nodes.[fileId]
    Assert.Equal(Special File, fileNode.kind)
    Assert.Equal(Filename.Ok "file.txt", fileNode.name)
    Assert.Equal(focus, fileNode.owner)

[<Fact>]
let ``planCreateOwnedDirectory creates folder under focus parent`` () =
    let focus, graph = outlineSetup ()
    let dirId, ops = FileNodeOps.planCreateOwnedDirectory graph focus
    let graph2 = applyOps graph ops
    let dirNode = graph2.nodes.[dirId]
    Assert.Equal(Special Directory, dirNode.kind)
    Assert.Equal(Filename.Ok "folder", dirNode.name)

[<Fact>]
let ``planCreateOwnedFile picks unused sibling name`` () =
    let focus, graph = outlineSetup ()
    let _, ops1 = FileNodeOps.planCreateOwnedFile graph focus
    let graph2 = applyOps graph ops1
    let _, ops2 = FileNodeOps.planCreateOwnedFile graph2 focus
    let graph3 = applyOps graph2 ops2
    let names =
        graph3.nodes.[focus].children
        |> List.choose (fun c ->
            if c.ref <> Ownership.Owner then None
            else
                match graph3.nodes.[c.id].name with
                | Filename.Ok n -> Some n
                | _ -> None)
    Assert.Equal< string list >([ "file.txt"; "file.txt1" ], names)

[<Fact>]
let ``planInsertFileRefAtFocus inserts ref at index`` () =
    let t = tree.Value
    let insert = { parentId = t.blueChild; index = 0 }
    let ops = FileNodeOps.planInsertFileRefAtFocus insert t.libFs t.graph
    Assert.Equal(1, ops.Length)
    let graph2 = applyOps t.graph ops
    let child = graph2.nodes.[t.blueChild].children.[0]
    Assert.Equal(Ownership.Ref, child.ref)
    Assert.Equal(t.libFs, child.id)

[<Fact>]
let ``planInsertFileRefAtFocus is idempotent when ref already present`` () =
    let t = tree.Value
    let graph1 =
        match Graph.replace t.blueChild 0 [] [ asRef t.libFs ] t.graph with
        | Ok g -> g
        | Error e -> failwith e
    let insert = { parentId = t.blueChild; index = 0 }
    let ops = FileNodeOps.planInsertFileRefAtFocus insert t.libFs graph1
    Assert.Empty(ops)
