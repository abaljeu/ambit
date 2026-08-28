module ExprRunTests

open Gambol.Shared
open Xunit

type private Fixture =
    { graph: Graph
      focus: NodeId
      blue1: NodeId
      blue2: NodeId }

let private namedNormal id name owner =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner)

let private addUnder parentId child graph =
    let parent = graph.nodes.[parentId]
    let nodes =
        graph.nodes
        |> Map.add child.id child
        |> Map.add parentId
            { parent with children = parent.children @ [ ChildNode.owner child.id ] }
    Graph.fromNodes graph.root nodes

let private build () : Fixture =
    let focusId = NodeId.New()
    let blue1 = NodeId.New()
    let blue2 = NodeId.New()
    let graph0 = Graph.create ()
    let graph =
        graph0
        |> addUnder graph0.root
            (Node.Create(focusId, text = "focus", owner = graph0.root))
        |> addUnder Graph.workspacesId (namedNormal blue1 "blue" Graph.workspacesId)
        |> addUnder Graph.workspacesId (namedNormal blue2 "blue" Graph.workspacesId)
    { graph = graph
      focus = focusId
      blue1 = blue1
      blue2 = blue2 }

let private runFocus (f: Fixture) line =
    ExprRun.run f.focus f.graph line

let private refIds ops =
    ops
    |> List.choose (function
        | Op.Replace(_, _, kids) -> Some(kids |> List.map (fun c -> c.id, c.ref))
        | _ -> None)
    |> List.tryHead
    |> Option.defaultValue []

let private hasBlueletter (ops: Op list) =
    let textOk =
        ops
        |> List.exists (function
            | Op.NewNode(_, "No matches found") -> true
            | _ -> false)
    let classOk =
        ops
        |> List.exists (function
            | Op.SetClasses(_, _, classes) -> CssClass.contains "blueletter" classes
            | _ -> false)
    textOk && classOk

[<Fact>]
let ``equals root descendant named blue writes Ref Children`` () =
    let f = build ()
    match runFocus f "= root descendant named \"blue\"" with
    | ExprRun.Ignore -> failwith "expected Apply"
    | ExprRun.Apply plan ->
        Assert.True(plan.unfold)
        let kids = refIds plan.ops
        Assert.Equal<(NodeId * Ownership) list>(
            [ f.blue1, Ownership.Ref; f.blue2, Ownership.Ref ],
            kids)

[<Fact>]
let ``name-equals renames current Node and materialises`` () =
    let f = build ()
    match runFocus f "todo=root descendant named \"blue\"" with
    | ExprRun.Ignore -> failwith "expected Apply"
    | ExprRun.Apply plan ->
        match plan.ops |> List.tryFind (function Op.SetName _ -> true | _ -> false) with
        | Some(Op.SetName(id, _, "todo")) -> Assert.Equal(f.focus, id)
        | _ -> failwith "expected SetName todo"
        Assert.Equal<(NodeId * Ownership) list>(
            [ f.blue1, Ownership.Ref; f.blue2, Ownership.Ref ],
            refIds plan.ops)

[<Fact>]
let ``bare Expression is not a Run statement`` () =
    let f = build ()
    match runFocus f "root descendant named \"blue\"" with
    | ExprRun.Ignore -> ()
    | ExprRun.Apply _ -> failwith "expected Ignore"
    match runFocus f "#todo = root" with
    | ExprRun.Ignore -> ()
    | ExprRun.Apply _ -> failwith "expected Ignore"

[<Fact>]
let ``parse type and zero Answers write blueletter No matches found`` () =
    let f = build ()
    let cases =
        [ "= /"
          "= root text child"
          "= named \"zzz\"" ]
    for line in cases do
        match runFocus f line with
        | ExprRun.Ignore -> failwith $"expected Apply for {line}"
        | ExprRun.Apply plan ->
            Assert.True(hasBlueletter plan.ops)
            Assert.True(plan.unfold)
