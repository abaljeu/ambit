module AmbleRunTests

open Gambol.Shared
open Xunit

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private nameString (name: Filename) : string =
    match name with
    | Filename.Ok s -> s
    | _ -> ""

let private newNodeTexts (ops: Op list) =
    ops |> List.choose (function Op.NewNode(_, text) -> Some text | _ -> None)

let private hasBlueletter (ops: Op list) =
    let textOk = newNodeTexts ops |> List.contains "No matches found"
    let classOk =
        ops
        |> List.exists (function
            | Op.SetClasses(_, _, classes) -> CssClass.contains "blueletter" classes
            | _ -> false)
    textOk && classOk

let private hasRedletterText (ops: Op list) (text: string) =
    let textOk = newNodeTexts ops |> List.contains text
    let classOk =
        ops
        |> List.exists (function
            | Op.SetClasses(_, _, classes) -> CssClass.contains "redletter" classes
            | _ -> false)
    textOk && classOk

let private refKids ops =
    ops
    |> List.choose (function
        | Op.Replace(_, _, kids) -> Some kids
        | _ -> None)
    |> List.tryHead
    |> Option.defaultValue []

[<Fact>]
let ``run bare RefExpr does nothing`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "//TRASH/" ] graph0
    let ops = requireOk "run" (AmbleRun.run ids.[0] graph1 "//TRASH/")
    Assert.Empty(ops)

[<Fact>]
let ``replace rejects trash owner under non-root parent`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "a" ] graph0
    match Graph.replace ids.[0] 0 [] [ ChildNode.owner Graph.trashId ] graph1 with
    | Error msg -> Assert.Contains("OWNED by a non-root parent", msg)
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``run equals materialises Ref Children`` () =
    let t = RefExprTestTree.build ()
    let ops =
        requireOk "run" (AmbleRun.run t.plainChild t.graph
            "= root descendant named \"blue\"")
    let kids = refKids ops
    let ids = kids |> List.map (fun c -> c.id)
    Assert.Contains(t.blueChild, ids)
    Assert.Contains(t.nestedBlue, ids)
    Assert.True(kids |> List.forall (fun c -> c.ref = Ownership.Ref))

[<Fact>]
let ``run name-equals renames focus and materialises`` () =
    let t = RefExprTestTree.build ()
    let focusId = t.plainChild
    let ops =
        requireOk "run" (AmbleRun.run focusId t.graph
            "todo=root descendant named \"blue\"")
    match ops |> List.tryFind (function Op.SetName _ -> true | _ -> false) with
    | Some(Op.SetName(nodeId, oldName, newName)) ->
        Assert.Equal(focusId, nodeId)
        Assert.Equal("plain", oldName)
        Assert.Equal("todo", newName)
    | _ -> Assert.Fail("expected SetName op")
    let ids = refKids ops |> List.map (fun c -> c.id)
    Assert.Contains(t.blueChild, ids)
    Assert.Contains(t.nestedBlue, ids)

[<Fact>]
let ``run name-equals unchanged name replaces children`` () =
    let t = RefExprTestTree.build ()
    let ops =
        requireOk "run" (AmbleRun.run t.blueChild t.graph
            "blue=root descendant named \"blue\"")
    let setNameOps = ops |> List.choose (function Op.SetName _ -> Some () | _ -> None)
    Assert.Empty(setNameOps)
    Assert.False((refKids ops).IsEmpty)

[<Fact>]
let ``run on special node is no-op`` () =
    let t = RefExprTestTree.build ()
    let ops = requireOk "run" (AmbleRun.run t.appFs t.graph "")
    Assert.Empty(ops)

[<Fact>]
let ``run prefix FunCall line does nothing`` () =
    let t = RefExprTestTree.build ()
    let ops = requireOk "run" (AmbleRun.run t.plainChild t.graph "text #todo")
    Assert.Empty(ops)
    let ops2 = requireOk "run" (AmbleRun.run t.blueChild t.graph "text #blue")
    Assert.Empty(ops2)

[<Fact>]
let ``run non-statement parse error does nothing`` () =
    let t = RefExprTestTree.build ()
    let ops = requireOk "run" (AmbleRun.run t.plainChild t.graph "#todo extra")
    Assert.Empty(ops)
    let line = "alpha" + System.Environment.NewLine + "beta"
    let ops2 = requireOk "run" (AmbleRun.run t.plainChild t.graph line)
    Assert.Empty(ops2)

[<Fact>]
let ``run equals parse failure writes the parse error not the input`` () =
    let t = RefExprTestTree.build ()
    let line = "= /"
    let ops = requireOk line (AmbleRun.run t.plainChild t.graph line)
    Assert.Equal<string list>([ "missing argument" ], newNodeTexts ops)
    Assert.DoesNotContain(line, newNodeTexts ops)

[<Fact>]
let ``run equals type failure writes the type error not the input`` () =
    let t = RefExprTestTree.build ()
    let line = "= text #todo"
    let ops = requireOk line (AmbleRun.run t.plainChild t.graph line)
    Assert.Equal<string list>([ "type error" ], newNodeTexts ops)
    Assert.DoesNotContain(line, newNodeTexts ops)

[<Fact>]
let ``run equals zero Answers write blueletter`` () =
    let t = RefExprTestTree.build ()
    let ops = requireOk "run" (AmbleRun.run t.plainChild t.graph "= named \"zzz\"")
    Assert.True(hasBlueletter ops)

[<Fact>]
let ``run name-equals updates graph name`` () =
    let t = RefExprTestTree.build ()
    let focusId = t.plainChild
    let ops =
        requireOk "run" (AmbleRun.run focusId t.graph
            "todo=root descendant named \"blue\"")
    let state = { graph = t.graph; history = History.empty; revision = Revision.Zero }
    let graph2 =
        ops
        |> List.fold (fun s op ->
            match Op.apply op s with
            | ApplyResult.Changed s' -> s'
            | ApplyResult.Unchanged s' -> s'
            | ApplyResult.Invalid(_, msg) -> failwith msg) state
        |> fun s -> s.graph
    Assert.Equal("todo", nameString graph2.nodes.[focusId].name)

[<Fact>]
let ``run shell eval error writes the eval message not the input`` () =
    let t = RefExprTestTree.build ()
    let line = "> python"
    let ops = requireOk "run" (AmbleRun.run t.plainChild t.graph line)
    Assert.Equal<string list>([ "Expression type not implemented" ], newNodeTexts ops)
    Assert.DoesNotContain(line, newNodeTexts ops)
    Assert.True(hasRedletterText ops "Expression type not implemented")

[<Fact>]
let ``run shell parse error writes the parse message not the input`` () =
    let t = RefExprTestTree.build ()
    let line = ">"
    let ops = requireOk "run" (AmbleRun.run t.plainChild t.graph line)
    Assert.Equal<string list>([ "empty command stage" ], newNodeTexts ops)
    Assert.DoesNotContain(line, newNodeTexts ops)
    Assert.True(hasRedletterText ops "empty command stage")

let private requirePlan label r =
    match r with
    | Ok p -> p
    | Error e -> failwith $"{label}: {e}"

let private applyOps graph ops =
    let state = { graph = graph; history = History.empty; revision = Revision.Zero }
    ops
    |> List.fold
        (fun s op ->
            match Op.apply op s with
            | ApplyResult.Changed s' -> s'
            | ApplyResult.Unchanged s' -> s'
            | ApplyResult.Invalid(_, msg) -> failwith msg)
        state
    |> fun s -> s.graph

let private addUnder parentId child graph =
    let parent = graph.nodes.[parentId]
    let nodes =
        graph.nodes
        |> Map.add child.id child
        |> Map.add parentId
            { parent with children = parent.children @ [ ChildNode.owner child.id ] }
    Graph.fromNodes graph.root nodes

let private entryOf nodeId (siteMap: SiteMap) =
    siteMap.entries
    |> Map.toSeq
    |> Seq.map snd
    |> Seq.find (fun e -> e.nodeId = nodeId)

let private focusUnderRoot () =
    let graph0 = Graph.create ()
    let focusId = NodeId.New()
    let graph =
        addUnder graph0.root (Node.Create(focusId, text = "focus", owner = graph0.root)) graph0
    graph, focusId

let private afterRun line =
    let graph, focusId = focusUnderRoot ()
    let siteMap, nextId = ViewModel.buildSiteMap graph
    Assert.False((entryOf focusId siteMap).expanded)
    let plan = requirePlan line (AmbleRun.runPlan focusId graph line)
    let graph2 = applyOps graph plan.ops
    let siteMap2, nextId2 = ViewModel.reconcileSiteMap graph2 siteMap nextId
    let siteMap3, _ =
        AmbleRun.applyUnfold plan.unfold focusId graph2 siteMap2 nextId2
    plan, entryOf focusId siteMap3

[<Fact>]
let ``runPlan materialise sets unfold`` () =
    let t = RefExprTestTree.build ()
    let plan =
        requirePlan "runPlan" (AmbleRun.runPlan t.plainChild t.graph
            "= root descendant named \"blue\"")
    Assert.True(plan.unfold)
    Assert.False(plan.ops.IsEmpty)

[<Fact>]
let ``runPlan no matches sets unfold`` () =
    let t = RefExprTestTree.build ()
    let plan =
        requirePlan "runPlan" (AmbleRun.runPlan t.plainChild t.graph
            "= named \"zzz\"")
    Assert.True(plan.unfold)
    Assert.True(hasBlueletter plan.ops)

[<Fact>]
let ``runPlan ignore does not unfold`` () =
    let t = RefExprTestTree.build ()
    let plan =
        requirePlan "runPlan" (AmbleRun.runPlan t.plainChild t.graph "text #todo")
    Assert.False(plan.unfold)
    Assert.Empty(plan.ops)

[<Fact>]
let ``run that writes Children unfolds the Run node`` () =
    let plan, entry = afterRun "= named \"zzz\""
    Assert.True(plan.unfold)
    Assert.True(entry.expanded)
    Assert.False(entry.children.IsEmpty)

[<Fact>]
let ``run Ignore leaves the Run node folded`` () =
    let plan, entry = afterRun "text #todo"
    Assert.False(plan.unfold)
    Assert.Empty(plan.ops)
    Assert.False(entry.expanded)
