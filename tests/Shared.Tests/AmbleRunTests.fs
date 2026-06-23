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
let ``run assign renames focus node`` () =
    let t = RefExprTestTree.build ()
    let focusId = t.plainChild
    let ops = requireOk "run" (AmbleRun.run focusId t.graph "todo = #rugby")
    match ops with
    | [ Op.SetName(nodeId, oldName, newName) ] ->
        Assert.Equal(focusId, nodeId)
        Assert.Equal("plain", oldName)
        Assert.Equal("todo", newName)
    | other -> failwith $"unexpected ops: {other}"

[<Fact>]
let ``run assign unchanged name returns empty ops`` () =
    let t = RefExprTestTree.build ()
    let focusId = t.blueChild
    let ops = requireOk "run" (AmbleRun.run focusId t.graph "blue = #todo")
    Assert.Empty(ops)

[<Fact>]
let ``run on special node is no-op`` () =
    let t = RefExprTestTree.build ()
    let ops = requireOk "run" (AmbleRun.run t.appFs t.graph "")
    Assert.Empty(ops)

[<Fact>]
let ``run expression statement with no nodes returns empty ops`` () =
    let t = RefExprTestTree.build ()
    let ops = requireOk "run" (AmbleRun.run t.plainChild t.graph "text #todo")
    Assert.Empty(ops)

[<Fact>]
let ``run text named ref creates child from node text`` () =
    let t = RefExprTestTree.build ()
    let focusId = t.blueChild
    let ops = requireOk "run" (AmbleRun.run focusId t.graph "text #blue")
    let newTexts =
        ops |> List.choose (function Op.NewNode(_, text) -> Some text | _ -> None)
    Assert.Equal<string list>([ "beta" ], newTexts)
    match ops |> List.tryFind (function Op.Replace _ -> true | _ -> false) with
    | Some (Op.Replace(parentId, 0, _, newChildren)) ->
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
    | Some (Op.Replace(parentId, 0, _, newChildren)) ->
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
    | Some (Op.Replace(parentId, 0, _, newChildren)) ->
        Assert.Equal(focusId, parentId)
        Assert.Equal(2, newChildren.Length)
    | _ -> Assert.Fail("expected Replace op")

[<Fact>]
let ``run assign updates graph name`` () =
    let t = RefExprTestTree.build ()
    let focusId = t.plainChild
    let ops = requireOk "run" (AmbleRun.run focusId t.graph "todo = #rugby")
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
