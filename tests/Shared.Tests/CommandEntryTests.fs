module Gambol.Shared.Tests.CommandEntryTests

open FSharp.Reflection
open Xunit
open Gambol.Shared.CommandEntry

[<Fact>]
let ``inKeyScope respects selection context`` () =
    Assert.True(inKeyScope true SelectionOnly)
    Assert.False(inKeyScope false SelectionOnly)
    Assert.False(inKeyScope true EditingOnly)
    Assert.True(inKeyScope false EditingOnly)
    Assert.True(inKeyScope true SelectionOrEditing)
    Assert.True(inKeyScope false SelectionOrEditing)

[<Fact>]
let ``scopeInSelection includes selection scopes only`` () =
    Assert.True(scopeInSelection SelectionOnly)
    Assert.False(scopeInSelection EditingOnly)
    Assert.True(scopeInSelection SelectionOrEditing)

[<Fact>]
let ``scopeInEditing includes editing scopes only`` () =
    Assert.False(scopeInEditing SelectionOnly)
    Assert.True(scopeInEditing EditingOnly)
    Assert.True(scopeInEditing SelectionOrEditing)

[<Fact>]
let ``allCommands has unique ids for every CommandId case`` () =
    let unionCases =
        FSharpType.GetUnionCases typeof<CommandId>
        |> Array.map (fun c -> unbox<CommandId> (FSharpValue.MakeUnion(c, [||])))
        |> Set.ofArray
    Assert.Equal(unionCases.Count, allCommands.Length)
    let ids = allCommands |> List.map (fun e -> e.id)
    Assert.Equal(allCommands.Length, List.distinct ids |> List.length)
    let tableIds = ids |> Set.ofList
    Assert.True((unionCases = tableIds))

[<Fact>]
let ``commandFor returns every command`` () =
    for id in allCommands |> List.map (fun e -> e.id) do
        match commandFor id with
        | None -> Assert.Fail($"missing metadata for {id}")
        | Some e -> Assert.Equal(id, e.id)

[<Fact>]
let ``displayName matches metadata name`` () =
    for e in allCommands do
        let name = displayName e.id
        Assert.False(System.String.IsNullOrWhiteSpace name)
        Assert.Equal(e.name, name)
