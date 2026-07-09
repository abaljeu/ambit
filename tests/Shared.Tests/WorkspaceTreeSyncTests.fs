module WorkspaceTreeSyncTests

open Gambol.Shared
open Xunit

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private applyOps (graph: Graph) (ops: Op list) : Graph =
    let state = { graph = graph; history = History.empty; revision = Revision.Zero }
    ops
    |> List.fold (fun s op ->
        match Op.apply op s with
        | ApplyResult.Changed s' -> s'
        | ApplyResult.Unchanged s' -> s'
        | ApplyResult.Invalid(_, msg) -> failwith msg) state
    |> fun s -> s.graph

let private dirGraph () =
    let ws = NodeId.New()
    let dir = NodeId.New()
    let g0 = Graph.create ()
    let g1 =
        applyOps g0
            [ Op.NewSpecialNode(ws, Workspace, "repo")
              Op.NewSpecialNode(dir, Directory, "src")
              Op.Replace(Graph.workspacesId, 0, [], owned [ ws ])
              Op.Replace(ws, 0, [], owned [ dir ]) ]
    g1, dir

[<Fact>]
let ``sync creates missing disk stub`` () =
    let g, dir = dirGraph ()
    let disk = [ { name = "new.txt"; kind = File; mtimeUtc = 1L } ]
    match WorkspaceTreeSync.planShallowSync g dir disk with
    | Error e -> Assert.Fail(e)
    | Ok plan ->
        Assert.Equal(1, plan.summary.created)
        Assert.True(plan.ops.Length > 0)

[<Fact>]
let ``sync skips dot amb`` () =
    Assert.True(WorkspaceTreeSync.shouldSkipEntry ".amb")
    Assert.True(WorkspaceTreeSync.shouldSkipEntry ".git")

[<Fact>]
let ``sync reuses same-kind owned child`` () =
    let g, dir = dirGraph ()
    let file = NodeId.New()
    let g1 =
        applyOps g
            [ Op.NewSpecialNode(file, File, "keep.txt")
              Op.Replace(dir, 0, [], owned [ file ]) ]
    let disk = [ { name = "keep.txt"; kind = File; mtimeUtc = 1L } ]
    match WorkspaceTreeSync.planShallowSync g1 dir disk with
    | Error e -> Assert.Fail(e)
    | Ok plan ->
        Assert.Equal(1, plan.summary.reused)
        Assert.Equal(0, plan.summary.created)
        Assert.True(plan.ops.IsEmpty)

[<Fact>]
let ``sync kind collision renames graph node`` () =
    let g, dir = dirGraph ()
    let file = NodeId.New()
    let g1 =
        applyOps g
            [ Op.NewSpecialNode(file, File, "item")
              Op.Replace(dir, 0, [], owned [ file ]) ]
    let disk = [ { name = "item"; kind = Directory; mtimeUtc = 1L } ]
    match WorkspaceTreeSync.planShallowSync g1 dir disk with
    | Error e -> Assert.Fail(e)
    | Ok plan ->
        Assert.Equal(1, plan.summary.renamed)
        Assert.Equal(1, plan.summary.created)
