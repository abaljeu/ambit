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
let ``run equals parse type and zero Answers write blueletter`` () =
    let t = RefExprTestTree.build ()
    let cases = [ "= /"; "= text #todo"; "= named \"zzz\"" ]
    for line in cases do
        let ops = requireOk line (AmbleRun.run t.plainChild t.graph line)
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
let ``run shell line still plans error children`` () =
    let t = RefExprTestTree.build ()
    let line = "> python"
    let ops = requireOk "run" (AmbleRun.run t.plainChild t.graph line)
    let newTexts =
        ops |> List.choose (function Op.NewNode(_, text) -> Some text | _ -> None)
    Assert.Equal<string list>([ line ], newTexts)
    match ops |> List.tryFind (function Op.SetClasses _ -> true | _ -> false) with
    | Some(Op.SetClasses(_, old, newClasses)) ->
        Assert.Equal(CssClass.empty, old)
        Assert.True(CssClass.contains "redletter" newClasses)
    | _ -> Assert.Fail("expected SetClasses op")
