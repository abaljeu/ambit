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
    let dirId = NodeId.New()
    let dirNode =
        Node.Create(
            dirId,
            text = "docs",
            name = Filename.create "docs",
            kind = Special Directory)
    let graph1 =
        graph0.nodes
        |> Map.add dirId dirNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let idx = Graph.fileTreeInsertIndex graph1 Graph.rootId
    let graph2 =
        match Graph.replace Graph.rootId idx [] [ owned dirId ] graph1 with
        | Ok g -> g
        | Error e -> failwith e
    dirId, graph2

[<Fact>]
let ``planCreateWorkspace creates workspace under Workspaces`` () =
    let graph = Graph.create ()
    let wsId, ops = FileNodeOps.planCreateWorkspace graph ""
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
    let fileId, ops = FileNodeOps.planCreateOwnedFile graph focus ""
    let graph2 = applyOps graph ops
    let fileNode = graph2.nodes.[fileId]
    Assert.Equal(Special File, fileNode.kind)
    Assert.Equal(Filename.Ok "file.txt", fileNode.name)
    Assert.Equal(focus, fileNode.owner)

[<Fact>]
let ``planCreateOwnedDirectory creates folder under focus parent`` () =
    let focus, graph = outlineSetup ()
    let dirId, ops = FileNodeOps.planCreateOwnedDirectory graph focus ""
    let graph2 = applyOps graph ops
    let dirNode = graph2.nodes.[dirId]
    Assert.Equal(Special Directory, dirNode.kind)
    Assert.Equal(Filename.Ok "folder", dirNode.name)

[<Fact>]
let ``planCreateOwnedDirectory uses query as name and text`` () =
    let focus, graph = outlineSetup ()
    let dirId, ops = FileNodeOps.planCreateOwnedDirectory graph focus "my-docs"
    let graph2 = applyOps graph ops
    let dirNode = graph2.nodes.[dirId]
    Assert.Equal(Special Directory, dirNode.kind)
    Assert.Equal(Filename.Ok "my-docs", dirNode.name)
    Assert.Equal("my-docs", dirNode.text)

[<Fact>]
let ``planCreateOwnedFile picks unused sibling name`` () =
    let focus, graph = outlineSetup ()
    let _, ops1 = FileNodeOps.planCreateOwnedFile graph focus ""
    let graph2 = applyOps graph ops1
    let _, ops2 = FileNodeOps.planCreateOwnedFile graph2 focus ""
    let graph3 = applyOps graph2 ops2
    let names =
        graph3.nodes.[focus].children
        |> List.choose (fun c ->
            if c.ref <> Ownership.Owner then None
            else
                match graph3.nodes.[c.id].name with
                | Filename.Ok n -> Some n
                | _ -> None)
    Assert.Equal< string list >([ "file.txt"; "file1.txt" ], names)

[<Fact>]
let ``planCreateWorkspace avoids root-owned file name`` () =
    let graph0 = Graph.create ()
    let fileId, opsFile = FileNodeOps.planCreateOwnedFile graph0 Graph.rootId "shared"
    let graph1 = applyOps graph0 opsFile
    Assert.Equal(Filename.Ok "shared", graph1.nodes.[fileId].name)
    let wsId, opsWs = FileNodeOps.planCreateWorkspace graph1 "shared"
    let graph2 = applyOps graph1 opsWs
    Assert.Equal(Filename.Ok "shared1", graph2.nodes.[wsId].name)

[<Fact>]
let ``planCreateOwnedFile under root avoids workspace name`` () =
    let graph0 = Graph.create ()
    let wsId, opsWs = FileNodeOps.planCreateWorkspace graph0 "shared"
    let graph1 = applyOps graph0 opsWs
    Assert.Equal(Filename.Ok "shared", graph1.nodes.[wsId].name)
    let fileId, opsFile = FileNodeOps.planCreateOwnedFile graph1 Graph.rootId "shared"
    let graph2 = applyOps graph1 opsFile
    Assert.Equal(Filename.Ok "shared1", graph2.nodes.[fileId].name)

[<Fact>]
let ``planCreateOwnedDirectory under nested parent ignores workspace name`` () =
    let focus, graph0 = outlineSetup ()
    let wsId, opsWs = FileNodeOps.planCreateWorkspace graph0 "shared"
    let graph1 = applyOps graph0 opsWs
    Assert.Equal(Filename.Ok "shared", graph1.nodes.[wsId].name)
    let dirId, opsDir = FileNodeOps.planCreateOwnedDirectory graph1 focus "shared"
    let graph2 = applyOps graph1 opsDir
    Assert.Equal(Filename.Ok "shared", graph2.nodes.[dirId].name)

let private normalUnderRoot () =
    let graph0 = Graph.create ()
    let graph1, normalId = Graph.newNode "note" graph0
    let idx = Graph.fileTreeInsertIndex graph1 Graph.rootId
    let graph2 =
        match Graph.replace Graph.rootId idx [] [ owned normalId ] graph1 with
        | Ok g -> g
        | Error e -> failwith e
    normalId, graph2

[<Fact>]
let ``planCreateOwnedFile under invalid focus inserts beside under parent`` () =
    let focus, graph = normalUnderRoot ()
    let fileId, ops = FileNodeOps.planCreateOwnedFile graph focus ""
    Assert.True(ops.Length > 0)
    let graph2 = applyOps graph ops
    let fileNode = graph2.nodes.[fileId]
    Assert.Equal(Special File, fileNode.kind)
    Assert.Equal(Graph.rootId, fileNode.owner)
    let rootChildren = graph2.nodes.[Graph.rootId].children
    let focusIdx =
        rootChildren |> List.findIndex (fun c -> c.id = focus)
    Assert.Equal(focus, rootChildren.[focusIdx].id)
    Assert.Equal(fileId, rootChildren.[focusIdx + 1].id)
    Assert.Equal(Ownership.Owner, rootChildren.[focusIdx + 1].ref)

[<Fact>]
let ``planCreateOwnedDirectory under invalid focus inserts beside under parent`` () =
    let focus, graph = normalUnderRoot ()
    let dirId, ops = FileNodeOps.planCreateOwnedDirectory graph focus ""
    Assert.True(ops.Length > 0)
    let graph2 = applyOps graph ops
    Assert.Equal(Graph.rootId, graph2.nodes.[dirId].owner)
    let rootChildren = graph2.nodes.[Graph.rootId].children
    let focusIdx =
        rootChildren |> List.findIndex (fun c -> c.id = focus)
    Assert.Equal(dirId, rootChildren.[focusIdx + 1].id)

let private normalOwnedByNormal () =
    let parentId, graph0 = normalUnderRoot ()
    let graph1, childId = Graph.newNode "nested" graph0
    let graph2 =
        match Graph.replace parentId 0 [] [ owned childId ] graph1 with
        | Ok g -> g
        | Error e -> failwith e
    childId, graph2

[<Fact>]
let ``planCreateOwnedFile under normal owned by normal returns empty ops`` () =
    let focus, graph = normalOwnedByNormal ()
    let _, ops = FileNodeOps.planCreateOwnedFile graph focus ""
    Assert.Empty(ops)

[<Fact>]
let ``planCreateOwnedDirectory under normal owned by normal returns empty ops`` () =
    let focus, graph = normalOwnedByNormal ()
    let _, ops = FileNodeOps.planCreateOwnedDirectory graph focus ""
    Assert.Empty(ops)

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
