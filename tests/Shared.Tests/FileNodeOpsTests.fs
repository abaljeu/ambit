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
