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
let ``planCreateFileInWorkspaces creates workspace directory and file`` () =
    let t = tree.Value
    let target =
        FilePathResolve.tryResolveConcreteTarget t.contentFile t.graph "@newws:pkg/util.fs"
        |> Option.defaultWith (fun () -> failwith "expected concrete target")
    let fileId, ops = FileNodeOps.planCreateFileInWorkspaces t.graph target |> requireOk
    let graph2 = applyOps t.graph ops
    let fileNode = graph2.nodes.[fileId]
    Assert.Equal(Special File, fileNode.kind)
    Assert.Equal(Filename.Ok "util.fs", fileNode.name)
    Assert.Equal(
        Some "@newws:/pkg/util.fs",
        NodeDesktopPath.pathForNodeId graph2 fileId
    )

[<Fact>]
let ``planCreateFileInWorkspaces reuses existing directories`` () =
    let t = tree.Value
    let target =
        FilePathResolve.tryResolveConcreteTarget t.contentFile t.graph "@bobby:src/reuse.fs"
        |> Option.defaultWith (fun () -> failwith "expected concrete target")
    let _, ops = FileNodeOps.planCreateFileInWorkspaces t.graph target |> requireOk
    Assert.DoesNotContain(ops, fun op ->
        match op with
        | Op.NewSpecialNode(_, Directory, _) -> true
        | _ -> false)

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

[<Fact>]
let ``planCreateFileInWorkspaces inserts file before Workspaces on ROOT`` () =
    let focus, graph = outlineSetup ()
    let target =
        FilePathResolve.tryResolveConcreteTarget focus graph "file1"
        |> Option.defaultWith (fun () -> failwith "expected concrete target")
    let fileId, ops = FileNodeOps.planCreateFileInWorkspaces graph target |> requireOk
    let graph2 = applyOps graph ops
    let rootChildren = graph2.nodes.[Graph.rootId].children
    let fileIdx = rootChildren |> List.findIndex (fun c -> c.id = fileId)
    let wsIdx = rootChildren |> List.findIndex (fun c -> c.id = Graph.workspacesId)
    Assert.True(fileIdx < wsIdx)

[<Fact>]
let ``planAddFileAtFocus from outline creates file under ROOT and ref at focus`` () =
    let focus, graph = outlineSetup ()
    let target =
        FilePathResolve.tryResolveConcreteTarget focus graph "file1"
        |> Option.defaultWith (fun () -> failwith "expected concrete target")
    let insert = { parentId = focus; index = 0 }
    let fileId, ops = FileNodeOps.planAddFileAtFocus graph insert target |> requireOk
    let graph2 = applyOps graph ops
    Assert.True(
        graph2.nodes.[Graph.rootId].children
        |> List.exists (fun c -> c.id = fileId && c.ref = Ownership.Owner)
    )
    Assert.True(
        graph2.nodes.[focus].children
        |> List.exists (fun c -> c.id = fileId && c.ref = Ownership.Ref)
    )

[<Fact>]
let ``planAddFileAtFocus creates dir file and ref for relative path`` () =
    let t = tree.Value
    let target =
        FilePathResolve.tryResolveConcreteTarget t.contentFile t.graph "dir/file2"
        |> Option.defaultWith (fun () -> failwith "expected concrete target")
    let insert = { parentId = t.blueChild; index = 0 }
    let fileId, ops = FileNodeOps.planAddFileAtFocus t.graph insert target |> requireOk
    let graph2 = applyOps t.graph ops
    let dirId =
        graph2.nodes.[t.workspaceRoot].children
        |> List.pick (fun c ->
            match graph2.nodes |> Map.tryFind c.id with
            | Some n when n.kind = Special Directory && n.name = Filename.Ok "dir" -> Some c.id
            | _ -> None)
    let fileNode = graph2.nodes.[fileId]
    Assert.Equal(Special File, fileNode.kind)
    Assert.Equal(Filename.Ok "file2", fileNode.name)
    Assert.Equal(dirId, fileNode.owner)
    Assert.True(
        graph2.nodes.[t.blueChild].children
        |> List.exists (fun c -> c.id = fileId && c.ref = Ownership.Ref)
    )
