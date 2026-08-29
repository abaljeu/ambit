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

let private newNodeTexts (ops: Op list) =
    ops |> List.choose (function Op.NewNode(_, text) -> Some text | _ -> None)

let private hasBlueletterText (ops: Op list) (text: string) =
    let textOk = newNodeTexts ops |> List.contains text
    let classOk =
        ops
        |> List.exists (function
            | Op.SetClasses(_, _, classes) -> CssClass.contains "blueletter" classes
            | _ -> false)
    textOk && classOk

let private hasBlueletter ops = hasBlueletterText ops "No matches found"

let private applyOps (f: Fixture) line =
    match runFocus f line with
    | ExprRun.Ignore -> failwith $"expected Apply for {line}"
    | ExprRun.Apply plan -> plan

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
let ``parse failure writes the parse error not the input`` () =
    let f = build ()
    let line = "= /"
    let plan = applyOps f line
    Assert.True(plan.unfold)
    Assert.Equal<string list>([ "missing argument" ], newNodeTexts plan.ops)
    Assert.True(hasBlueletterText plan.ops "missing argument")
    Assert.DoesNotContain(line, newNodeTexts plan.ops)

[<Fact>]
let ``type failure writes the type error not the input`` () =
    let f = build ()
    let line = "= root text child"
    let plan = applyOps f line
    Assert.True(plan.unfold)
    Assert.Equal<string list>([ "type error" ], newNodeTexts plan.ops)
    Assert.True(hasBlueletterText plan.ops "type error")
    Assert.DoesNotContain(line, newNodeTexts plan.ops)

[<Fact>]
let ``zero Answers write blueletter No matches found`` () =
    let f = build ()
    let plan = applyOps f "= named \"zzz\""
    Assert.True(plan.unfold)
    Assert.True(hasBlueletterText plan.ops "No matches found")

let private specialNamed id kind name owner =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private exampleGraph () =
    let focusId = NodeId.New()
    let exampleWs = NodeId.New()
    let nestedDir = NodeId.New()
    let exampleFile = NodeId.New()
    let graph0 = Graph.create ()
    let graph =
        graph0
        |> addUnder graph0.root
            (Node.Create(focusId, text = "= //Example", owner = graph0.root))
        |> addUnder Graph.workspacesId
            (specialNamed exampleWs Workspace "Example" Graph.workspacesId)
        |> addUnder exampleWs
            (specialNamed nestedDir Directory "Example" exampleWs)
        |> addUnder nestedDir
            (specialNamed exampleFile File "Example" nestedDir)
    focusId, exampleWs, nestedDir, exampleFile, graph

let private outcomeOf graph focusId source =
    let input = ExprAnswer.Node graph.nodes.[focusId]
    ExprCompile.evalOutcome graph input source

[<Fact>]
let ``double-slash Example parses as root then structural name`` () =
    match ExprParse.parseExpr "//Example" with
    | Error err -> failwith $"parse failed: {err}"
    | Ok expr ->
        let expected =
            Expr.Term(
                ExprTerm.Cluster(
                    [ ClusterStep.Root; ClusterStep.Structural "Example" ],
                    None))
        Assert.Equal(expected, expr)

[<Fact>]
let ``equals slash-slash Example refs first-layer Workspace named Example`` () =
    let focusId, exampleWs, nestedDir, exampleFile, graph = exampleGraph ()
    match outcomeOf graph focusId "//Example" with
    | ExprCompile.ParseFailed e -> failwith $"parse failed: {e}"
    | ExprCompile.TypeFailed e -> failwith $"type failed: {e}"
    | ExprCompile.Hits(_, answers) ->
        let ids =
            answers
            |> List.map (function
                | ExprAnswer.Node n -> n.id
                | _ -> failwith "expected Node")
        Assert.Equal<NodeId list>([ exampleWs ], ids)
        Assert.DoesNotContain(nestedDir, ids)
        Assert.DoesNotContain(exampleFile, ids)
    match ExprRun.run focusId graph "= //Example" with
    | ExprRun.Ignore -> failwith "expected Apply"
    | ExprRun.Apply plan ->
        Assert.False(hasBlueletter plan.ops)
        Assert.Equal<(NodeId * Ownership) list>(
            [ exampleWs, Ownership.Ref ],
            refIds plan.ops)
    match AmbleRun.run focusId graph "= //Example" with
    | Error e -> failwith $"AmbleRun error: {e}"
    | Ok ops ->
        Assert.False(hasBlueletter ops)
        Assert.Equal<(NodeId * Ownership) list>(
            [ exampleWs, Ownership.Ref ],
            refIds ops)

[<Fact>]
let ``equals slash-slash Example is no-match when Example is nested`` () =
    let focusId = NodeId.New()
    let wsId = NodeId.New()
    let nestedDir = NodeId.New()
    let graph0 = Graph.create ()
    let graph =
        graph0
        |> addUnder graph0.root
            (Node.Create(focusId, text = "= //Example", owner = graph0.root))
        |> addUnder Graph.workspacesId
            (specialNamed wsId Workspace "ws" Graph.workspacesId)
        |> addUnder wsId (specialNamed nestedDir Directory "Example" wsId)
    match ExprRun.run focusId graph "= //Example" with
    | ExprRun.Ignore -> failwith "expected Apply"
    | ExprRun.Apply plan ->
        Assert.True(hasBlueletter plan.ops)

[<Fact>]
let ``equals root descendant named hit writes at most maxMaterialisedAnswers`` () =
    let focusId = NodeId.New()
    let extra = 10
    let total = ExprRun.maxMaterialisedAnswers + extra
    let graph0 = Graph.create ()
    let graph1 =
        graph0
        |> addUnder graph0.root
            (Node.Create(focusId, text = "focus", owner = graph0.root))
    let graph, hitIds =
        [ 1..total ]
        |> List.fold
            (fun (g, ids) _ ->
                let id = NodeId.New()
                let child = namedNormal id "hit" Graph.workspacesId
                addUnder Graph.workspacesId child g, id :: ids)
            (graph1, [])
    let hitIds = List.rev hitIds
    match ExprRun.run focusId graph "= root descendant named \"hit\"" with
    | ExprRun.Ignore -> failwith "expected Apply"
    | ExprRun.Apply plan ->
        Assert.True(plan.unfold)
        let kids = refIds plan.ops |> List.map fst
        Assert.Equal(ExprRun.maxMaterialisedAnswers, kids.Length)
        Assert.Equal<NodeId list>(
            hitIds |> List.take ExprRun.maxMaterialisedAnswers,
            kids)
        Assert.DoesNotContain(List.item ExprRun.maxMaterialisedAnswers hitIds, kids)

[<Fact>]
let ``bang-star containing plans at most maxMaterialisedAnswers without SiteMap`` () =
    let line = "=!* containing \"OpenDrive\""
    let graph0 = Graph.create ()
    let focusId = NodeId.New()
    let hitId = NodeId.New()
    let graph =
        graph0
        |> addUnder graph0.root
            (Node.Create(focusId, text = line, owner = graph0.root))
        |> addUnder graph0.root
            (Node.Create(hitId, text = "OpenDrive notes", owner = graph0.root))
    match ExprRun.run focusId graph line with
    | ExprRun.Ignore -> failwith "expected Apply"
    | ExprRun.Apply plan ->
        let kids = refIds plan.ops |> List.map fst
        Assert.True(kids.Length > 0)
        Assert.True(kids.Length <= ExprRun.maxMaterialisedAnswers)
        Assert.Contains(hitId, kids)
        Assert.Contains(focusId, kids)
