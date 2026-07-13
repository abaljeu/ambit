module Gambol.Shared.Tests.CommandEntryTests

open FSharp.Reflection
open Xunit
open Gambol.Shared
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

[<Fact>]
let ``parse or upload owns Ctrl Shift greater-than`` () =
    let entry = commandFor ParseOrPush |> Option.get
    Assert.Equal<string list>([ "Ctrl+Shift+>" ], entry.keys)
    Assert.DoesNotContain(allCommands, fun command -> command.name = "Import")

[<Fact>]
let ``pull to desktop owns Ctrl Shift less-than`` () =
    let entry = commandFor GitPull |> Option.get
    Assert.Equal("Git Pull to Desktop", entry.name)
    Assert.Equal<string list>([ "Ctrl+Shift+<" ], entry.keys)
    Assert.DoesNotContain(allCommands, fun command -> command.name = "Export")

let private contextualGraph () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let childId = NodeId.New()
    let workspaceId = NodeId.New()
    let refHolderId = NodeId.New()
    let owned id = { ref = Ownership.Owner; id = id }
    let file =
        Node.Create(
            fileId,
            name = Filename.create "note.txt",
            children = [ owned childId ],
            kind = Special File,
            documentState = Unparsed)
    let child = Node.Create(childId, owner = fileId)
    let workspace =
        Node.Create(
            workspaceId,
            name = Filename.create "home",
            kind = Special Workspace)
    let holder =
        Node.Create(
            refHolderId,
            children = [ { ref = Ownership.Ref; id = fileId } ])
    let root = graph0.nodes.[Graph.rootId]
    let rootChildren =
        root.children @ [ owned fileId; owned workspaceId; owned refHolderId ]
    let graph =
        graph0.nodes
        |> Map.add Graph.rootId { root with children = rootChildren }
        |> Map.add fileId file
        |> Map.add childId child
        |> Map.add workspaceId workspace
        |> Map.add refHolderId holder
        |> Graph.fromNodes graph0.root
    graph, fileId, workspaceId, refHolderId

[<Fact>]
let ``context command parses owning unparsed file from focused owner occurrence`` () =
    let graph, fileId, _, _ = contextualGraph ()
    let rootIndex =
        graph.nodes.[Graph.rootId].children
        |> List.findIndex (fun child -> child.id = fileId)
    Assert.Equal(
        Some(ParseFile fileId),
        contextualTarget graph Graph.rootId rootIndex)
    Assert.Equal(
        Some(ParseFile fileId),
        contextualTarget graph fileId 0)

[<Fact>]
let ``context command pushes workspace and ignores ref occurrence`` () =
    let graph, fileId, workspaceId, refHolderId = contextualGraph ()
    Assert.Equal(
        Some(PushWorkspace workspaceId),
        contextualTarget graph Graph.rootId
            (graph.nodes.[Graph.rootId].children
             |> List.findIndex (fun child -> child.id = workspaceId)))
    Assert.Equal(None, contextualTarget graph refHolderId 0)
    let currentGraph =
        { graph with
            nodes =
                graph.nodes
                |> Map.add fileId
                    { graph.nodes.[fileId] with documentState = Current } }
    Assert.Equal(None, contextualTarget currentGraph fileId 0)
