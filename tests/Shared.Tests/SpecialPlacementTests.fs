module SpecialPlacementTests

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

let private wsWithDir () =
    let ws = NodeId.New()
    let dir = NodeId.New()
    let g0 = Graph.create ()
    let g1 =
        applyOps g0
            [ Op.NewSpecialNode(ws, Workspace, "repo")
              Op.NewSpecialNode(dir, Directory, "src")
              Op.Replace(Graph.workspacesId, 0, [], owned [ ws ])
              Op.Replace(ws, 0, [], owned [ dir ]) ]
    g1, ws, dir

[<Fact>]
let ``owned File under Workspace is legal`` () =
    let g, ws, _ = wsWithDir ()
    let file = NodeId.New()
    let g2 =
        applyOps g
            [ Op.NewSpecialNode(file, File, "a.txt")
              Op.Replace(ws, 0, [], owned [ file ]) ]
    Assert.True(Map.containsKey file g2.nodes)

[<Fact>]
let ``owned File under File is rejected`` () =
    let g, ws, _ = wsWithDir ()
    let file = NodeId.New()
    let inner = NodeId.New()
    let g1 =
        applyOps g
            [ Op.NewSpecialNode(file, File, "outer.txt")
              Op.NewSpecialNode(inner, File, "inner.txt")
              Op.Replace(ws, 0, [], owned [ file ]) ]
    match Graph.replace file 0 [] (owned [ inner ]) g1 with
    | Ok _ -> Assert.Fail("expected placement error")
    | Error msg -> Assert.Contains("Workspace or Directory", msg)

[<Fact>]
let ``file ref under Normal remains legal`` () =
    let g, ws, _ = wsWithDir ()
    let file = NodeId.New()
    let normal = NodeId.New()
    let g1 =
        applyOps g
            [ Op.NewSpecialNode(file, File, "a.txt")
              Op.NewNode(normal, "note")
              Op.Replace(ws, 0, [], owned [ file; normal ]) ]
    let g2 =
        applyOps g1 [ Op.Replace(normal, 0, [], [ { ref = Ownership.Ref; id = file } ]) ]
    Assert.Contains(file, g2.nodes.[normal].children |> List.map (fun c -> c.id))
