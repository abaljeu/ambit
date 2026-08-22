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

[<Fact>]
let ``run TRASH ref replace succeeds under normal focus`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "//TRASH/" ] graph0
    let focusId = ids.[0]
    let ops = requireOk "run" (AmbleRun.run focusId graph1 "//TRASH/")
    match ops |> List.tryFind (function Op.Replace _ -> true | _ -> false) with
    | Some (Op.Replace(parentId, _, [ child ])) ->
        Assert.Equal(focusId, parentId)
        Assert.Equal(Graph.trashId, child.id)
        Assert.Equal(Ownership.Ref, child.ref)
    | _ -> failwith $"unexpected ops: {ops}"
    let state = { graph = graph1; history = History.empty; revision = Revision.Zero }
    match Change.apply { id = 0; changeId = System.Guid.NewGuid(); ops = ops } state with
    | ApplyResult.Changed s ->
        let children = s.graph.nodes.[focusId].children
        Assert.Single(children) |> ignore
        Assert.Equal(Ownership.Ref, children.[0].ref)
        Assert.Equal(Graph.trashId, children.[0].id)
    | other -> failwith $"expected Changed, got {other}"

[<Fact>]
let ``replace rejects trash owner under non-root parent`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "a" ] graph0
    let focusId = ids.[0]
    match
        Graph.replace focusId 0 [] [ ChildNode.owner Graph.trashId ] graph1
    with
    | Error msg -> Assert.Contains("OWNED by a non-root parent", msg)
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``run assign renames focus node`` () =
    let t = RefExprTestTree.build ()
    let focusId = t.plainChild
    let ops = requireOk "run" (AmbleRun.run focusId t.graph "todo = ^/#blue")
    match ops |> List.tryFind (function Op.SetName _ -> true | _ -> false) with
    | Some (Op.SetName(nodeId, oldName, newName)) ->
        Assert.Equal(focusId, nodeId)
        Assert.Equal("plain", oldName)
        Assert.Equal("todo", newName)
    | _ -> Assert.Fail("expected SetName op")
    match ops |> List.tryFind (function Op.Replace _ -> true | _ -> false) with
    | Some (Op.Replace(parentId, _, newChildren)) ->
        Assert.Equal(focusId, parentId)
        Assert.Equal(2, newChildren.Length)
    | _ -> Assert.Fail("expected Replace op")

[<Fact>]
let ``run assign unchanged name replaces children`` () =
    let t = RefExprTestTree.build ()
    let focusId = t.blueChild
    let ops = requireOk "run" (AmbleRun.run focusId t.graph "blue = ^/#blue")
    let setNameOps = ops |> List.choose (function Op.SetName _ -> Some () | _ -> None)
    Assert.Empty(setNameOps)
    match ops |> List.tryFind (function Op.Replace _ -> true | _ -> false) with
    | Some (Op.Replace(parentId, _, newChildren)) ->
        Assert.Equal(focusId, parentId)
        Assert.Equal(2, newChildren.Length)
    | _ -> Assert.Fail("expected Replace op")

[<Fact>]
let ``run on special node is no-op`` () =
    let t = RefExprTestTree.build ()
    let ops = requireOk "run" (AmbleRun.run t.appFs t.graph "")
    Assert.Empty(ops)

[<Fact>]
let ``run empty search creates error child`` () =
    let t = RefExprTestTree.build ()
    let focusId = t.plainChild
    let line = "text #todo"
    let ops = requireOk "run" (AmbleRun.run focusId t.graph line)
    let newTexts =
        ops |> List.choose (function Op.NewNode(_, text) -> Some text | _ -> None)
    Assert.Equal<string list>([ line ], newTexts)
    match ops |> List.tryFind (function Op.SetClasses _ -> true | _ -> false) with
    | Some (Op.SetClasses(_, old, newClasses)) ->
        Assert.Equal(CssClass.empty, old)
        Assert.True(CssClass.contains "redletter" newClasses)
    | _ -> Assert.Fail("expected SetClasses op")
    match ops |> List.tryFind (function Op.Replace _ -> true | _ -> false) with
    | Some (Op.Replace(parentId, _, newChildren)) ->
        Assert.Equal(focusId, parentId)
        Assert.Single(newChildren) |> ignore
    | _ -> Assert.Fail("expected Replace op")

[<Fact>]
let ``run text named ref creates child from node text`` () =
    let t = RefExprTestTree.build ()
    let focusId = t.blueChild
    let ops = requireOk "run" (AmbleRun.run focusId t.graph "text #blue")
    let newTexts =
        ops |> List.choose (function Op.NewNode(_, text) -> Some text | _ -> None)
    Assert.Equal<string list>([ "beta" ], newTexts)
    match ops |> List.tryFind (function Op.Replace _ -> true | _ -> false) with
    | Some (Op.Replace(parentId, _, newChildren)) ->
        Assert.Equal(focusId, parentId)
        Assert.Single(newChildren) |> ignore
    | _ -> Assert.Fail("expected Replace op")

[<Fact>]
let ``run parse error replaces children from line`` () =
    let t = RefExprTestTree.build ()
    let focusId = t.plainChild
    let ops = requireOk "run" (AmbleRun.run focusId t.graph "#todo extra")
    let newNodeOps = ops |> List.choose (function Op.NewNode _ -> Some () | _ -> None)
    let classOps =
        ops
        |> List.choose (function
            | Op.SetClasses(_, old, newClasses) when
                old = CssClass.empty && CssClass.contains "redletter" newClasses ->
                Some ()
            | _ -> None)
    Assert.Single(newNodeOps) |> ignore
    Assert.Single(classOps) |> ignore
    match ops |> List.tryFind (function Op.Replace _ -> true | _ -> false) with
    | Some (Op.Replace(parentId, _, newChildren)) ->
        Assert.Equal(focusId, parentId)
        Assert.Single(newChildren) |> ignore
    | _ -> Assert.Fail("expected Replace op")

[<Fact>]
let ``run parse error multiline replaces children`` () =
    let t = RefExprTestTree.build ()
    let focusId = t.plainChild
    let line = "alpha" + System.Environment.NewLine + "beta"
    let ops = requireOk "run" (AmbleRun.run focusId t.graph line)
    let newNodes = ops |> List.choose (function Op.NewNode(_, _) -> Some () | _ -> None)
    let classOps =
        ops
        |> List.choose (function
            | Op.SetClasses(_, old, newClasses) when
                old = CssClass.empty && CssClass.contains "redletter" newClasses ->
                Some ()
            | _ -> None)
    Assert.Equal(2, newNodes.Length)
    Assert.Equal(2, classOps.Length)
    match ops |> List.tryFind (function Op.Replace _ -> true | _ -> false) with
    | Some (Op.Replace(parentId, _, newChildren)) ->
        Assert.Equal(focusId, parentId)
        Assert.Equal(2, newChildren.Length)
    | _ -> Assert.Fail("expected Replace op")

[<Fact>]
let ``run assign updates graph name`` () =
    let t = RefExprTestTree.build ()
    let focusId = t.plainChild
    let ops = requireOk "run" (AmbleRun.run focusId t.graph "todo = ^/#blue")
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
