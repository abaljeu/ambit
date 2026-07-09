module FileExpandTests

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

let private fileGraph () =
    let ws = NodeId.New()
    let file = NodeId.New()
    let g0 = Graph.create ()
    let g1 =
        applyOps g0
            [ Op.NewSpecialNode(ws, Workspace, "repo")
              Op.NewSpecialNode(file, File, "notes.txt")
              Op.Replace(Graph.workspacesId, 0, [], owned [ ws ])
              Op.Replace(ws, 0, [], owned [ file ]) ]
    g1, file

[<Fact>]
let ``expand unparsed file attaches children and marks parsed`` () =
    let g, file = fileGraph ()
    match FileExpand.planParseFile g file "notes.txt" "alpha\nbeta" 42L with
    | Error e -> Assert.Fail(e)
    | Ok (ops, _) ->
        let g2 = applyOps g ops
        let node = g2.nodes.[file]
        Assert.Equal(FileState.Parsed 42L, node.fileState)
        Assert.True(node.children.Length > 0)

[<Fact>]
let ``isStale when disk mtime is newer`` () =
    let g, file = fileGraph ()
    let g1 =
        applyOps g [ Op.SetFileState(file, FileState.Unparsed, FileState.Parsed 100L) ]
    Assert.True(FileExpand.isStale g1 file 200L)
    Assert.False(FileExpand.isStale g1 file 100L)

[<Fact>]
let ``planParseFile warns when reparse on stale file`` () =
    let g, file = fileGraph ()
    let g1 =
        applyOps g [ Op.SetFileState(file, FileState.Unparsed, FileState.Parsed 100L) ]
    match FileExpand.planParseFile g1 file "notes.txt" "alpha" 200L with
    | Error e -> Assert.Fail(e)
    | Ok (_, status) ->
        Assert.True(status.IsSome)
        Assert.Contains("reparse", status.Value.text, System.StringComparison.OrdinalIgnoreCase)
