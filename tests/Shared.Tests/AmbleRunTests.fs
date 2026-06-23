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
let ``run rejects expression statement`` () =
    let t = RefExprTestTree.build ()
    match AmbleRun.run t.plainChild t.graph "text #todo" with
    | Error msg -> Assert.Contains("not yet supported", msg)
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``run propagates parse error`` () =
    let t = RefExprTestTree.build ()
    match AmbleRun.run t.plainChild t.graph "#todo extra" with
    | Error _ -> ()
    | Ok _ -> Assert.Fail("expected Error")

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
