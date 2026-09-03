module GraphOnlyChangeChunksTests

open System.Diagnostics
open Gambol.Shared
open Xunit

let private dummyOps (count: int) : Op list =
    List.init count (fun i ->
        let id = NodeId.New()
        Op.SetText(id, string i, string (i + 1)))

let private applyOps (graph: Graph) (ops: Op list) : Graph =
    let state =
        { graph = graph
          history = History.empty
          revision = Revision.Zero }
    ops
    |> List.fold
        (fun s op ->
            match Op.apply op s with
            | ApplyResult.Changed s' -> s'
            | ApplyResult.Unchanged s' -> s'
            | ApplyResult.Invalid(_, msg) -> failwith msg)
        state
    |> fun s -> s.graph

let private stubCreateBatch (fileCount: int) : Graph * Op list =
    let graph0 = Graph.create ()
    let wsId, wsOps = FileNodeOps.planCreateWorkspace graph0 "home"
    let graph1 = applyOps graph0 wsOps
    let _, fileOps =
        List.init fileCount id
        |> List.fold
            (fun (graph, acc) i ->
                let name = sprintf "f%03d.txt" i
                let _, ops = FileNodeOps.planCreateOwnedFile graph wsId name
                applyOps graph ops, List.append acc ops)
            (graph1, [])
    graph1, fileOps

[<Fact>]
let ``split empty ops is empty`` () =
    Assert.Empty(GraphOnlyChangeChunks.split [])

[<Fact>]
let ``split at maxOps keeps one chunk`` () =
    let ops = dummyOps GraphOnlyChangeChunks.maxOps
    let chunks = GraphOnlyChangeChunks.split ops
    Assert.Equal(1, chunks.Length)
    Assert.Equal<Op list>(ops, List.concat chunks)

[<Fact>]
let ``split above maxOps yields bounded chunks that concat to the original`` () =
    let extra = 3
    let ops = dummyOps (GraphOnlyChangeChunks.maxOps + extra)
    let chunks = GraphOnlyChangeChunks.split ops
    Assert.Equal(2, chunks.Length)
    Assert.Equal(GraphOnlyChangeChunks.maxOps, chunks.[0].Length)
    Assert.Equal(extra, chunks.[1].Length)
    Assert.Equal<Op list>(ops, List.concat chunks)

[<Fact>]
let ``maxOps stub creates apply well under DbAgent 8s bound`` () =
    let fileCount = GraphOnlyChangeChunks.maxOps / 2
    let graph, ops = stubCreateBatch fileCount
    Assert.True(
        ops.Length <= GraphOnlyChangeChunks.maxOps,
        sprintf "fixture produced %d ops; need a chunk-sized batch" ops.Length)
    Assert.True(ops.Length > 0)
    let state =
        { graph = graph
          history = History.empty
          revision = Revision.Zero }
    let change =
        { id = 0
          changeId = System.Guid.NewGuid()
          ops = ops }
    let sw = Stopwatch.StartNew()
    match ChangeAmendment.applyChange change state with
    | ApplyResult.Invalid(_, msg), _, _ -> failwith msg
    | _, _, _ -> ()
    sw.Stop()
    Assert.True(
        sw.ElapsedMilliseconds < 2000L,
        sprintf
            "applying %d stub ops took %dms (DbAgent bound is 8000ms)"
            ops.Length
            sw.ElapsedMilliseconds)
