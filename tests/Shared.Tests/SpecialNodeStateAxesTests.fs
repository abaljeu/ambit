module Gambol.Shared.Tests.SpecialNodeStateAxesTests

open Xunit
open Gambol.Shared

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

let private kinds = [ Workspace, "ws"; Directory, "dir"; File, "file.txt" ]

let private requireOk (result: Result<Graph, string>) : Graph =
    match result with
    | Ok graph -> graph
    | Error msg -> failwith msg

let private apply (ops: Op list) (graph: Graph) : Graph =
    match ChangeValidation.applyOps ops { graph = graph; eventId = EventId.zero } with
    | ApplyResult.Changed state -> state.graph
    | ApplyResult.Unchanged state -> state.graph
    | ApplyResult.Invalid(_, msg) -> failwith msg

let private freshState (graph: Graph) : State =
    { graph = graph; eventId = EventId.zero }

let private artifact (kind: SpecialKind) (name: string) : Graph * NodeId =
    let id = NodeId.New()
    let graph =
        Graph.create ()
        |> Graph.addDetachedNode (
            Node.Create(
                id,
                text = name,
                name = Filename.create name,
                kind = Special kind))
    graph, id

let private place (parentId: NodeId) (childId: NodeId) (graph: Graph) : Graph =
    let kids = Graph.children graph parentId
    apply
        [ ChildListWire.insertAt parentId kids kids.Length [ ChildNode.owner childId ] ]
        graph

[<Fact>]
let ``each special kind sets and resets both axes`` () =
    for kind, name in kinds do
        let graph, id = artifact kind name
        let unparsed = Graph.setParseState id ParseState.Unparsed graph |> requireOk
        Assert.Equal(ParseState.Unparsed, unparsed.nodes.[id].parseState)
        Assert.Equal(Unparsed, unparsed.nodes.[id].documentState)
        Assert.Equal(PersistState.Persisted, unparsed.nodes.[id].persistState)

        let parsed = Graph.setParseState id ParseState.Parsed unparsed |> requireOk
        Assert.Equal(ParseState.Parsed, parsed.nodes.[id].parseState)
        Assert.Equal(Current, parsed.nodes.[id].documentState)
        Assert.Equal(PersistState.Persisted, parsed.nodes.[id].persistState)

        let dirty = Graph.setPersistState id PersistState.Unpersisted parsed |> requireOk
        Assert.Equal(PersistState.Unpersisted, dirty.nodes.[id].persistState)
        Assert.Equal(ParseState.Parsed, dirty.nodes.[id].parseState)
        Assert.Equal(Current, dirty.nodes.[id].documentState)

        let clean = Graph.setPersistState id PersistState.Persisted dirty |> requireOk
        Assert.Equal(PersistState.Persisted, clean.nodes.[id].persistState)
        Assert.Equal(ParseState.Parsed, clean.nodes.[id].parseState)
        Assert.Equal(Current, clean.nodes.[id].documentState)

[<Fact>]
let ``axes reject Normal and Workspaces nodes`` () =
    let graph, _ = artifact File "file.txt"
    let normalId = NodeId.New()
    let withNormal =
        Graph.addDetachedNode (Node.Create(normalId, text = "n")) graph
    Assert.True(Result.isError (Graph.setParseState normalId ParseState.Parsed withNormal))
    Assert.True(
        Result.isError (Graph.setPersistState normalId PersistState.Unpersisted withNormal))
    Assert.True(
        Result.isError (Graph.setParseState Graph.workspacesId ParseState.Unparsed graph))
    Assert.True(
        Result.isError (Graph.setPersistState Graph.workspacesId PersistState.Unpersisted graph))

[<Fact>]
let ``NewSpecialNode starts Unparsed and Persisted`` () =
    for kind, name in kinds do
        let id = NodeId.New()
        let graph =
            apply [ Op.NewSpecialNode(id, kind, name) ] (Graph.create ())
        let node = graph.nodes.[id]
        Assert.Equal(Unparsed, node.documentState)
        Assert.Equal(ParseState.Unparsed, node.parseState)
        Assert.Equal(PersistState.Persisted, node.persistState)

[<Fact>]
let ``SetDocumentState dual-writes the parse axis and leaves persist`` () =
    let graph, id = artifact File "file.txt"
    let dirty = Graph.setPersistState id PersistState.Unpersisted graph |> requireOk
    let unparsed =
        apply [ Op.SetDocumentState(id, Current, Unparsed) ] dirty
    Assert.Equal(Unparsed, unparsed.nodes.[id].documentState)
    Assert.Equal(ParseState.Unparsed, unparsed.nodes.[id].parseState)
    Assert.Equal(PersistState.Unpersisted, unparsed.nodes.[id].persistState)

    let parsed =
        apply [ Op.SetDocumentState(id, Unparsed, Current) ] unparsed
    Assert.Equal(Current, parsed.nodes.[id].documentState)
    Assert.Equal(ParseState.Parsed, parsed.nodes.[id].parseState)
    Assert.Equal(PersistState.Unpersisted, parsed.nodes.[id].persistState)

    let absent =
        apply [ Op.SetDocumentState(id, Current, NoServerFile) ] parsed
    Assert.Equal(NoServerFile, absent.nodes.[id].documentState)
    Assert.Equal(ParseState.Unparsed, absent.nodes.[id].parseState)
    Assert.Equal(PersistState.Unpersisted, absent.nodes.[id].persistState)

[<Fact>]
let ``graph edit marks only the nearest owning special Unpersisted`` () =
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let childId = NodeId.New()
    let graph =
        Graph.create ()
        |> apply [ Op.NewSpecialNode(wsId, Workspace, "ws") ]
        |> place Graph.workspacesId wsId
        |> apply [ Op.NewSpecialNode(dirId, Directory, "dir") ]
        |> place wsId dirId
        |> apply [ Op.NewSpecialNode(fileId, File, "file.txt") ]
        |> place dirId fileId
        |> apply [ Op.SetDocumentState(fileId, Unparsed, Current) ]
        |> apply [ Op.NewNode(childId, "body") ]
        |> place fileId childId
        |> fun g -> Graph.setPersistState wsId PersistState.Persisted g |> requireOk
        |> fun g -> Graph.setPersistState dirId PersistState.Persisted g |> requireOk
        |> fun g -> Graph.setPersistState fileId PersistState.Persisted g |> requireOk

    let edited = apply [ Op.SetText(childId, "body", "edited") ] graph
    Assert.Equal(PersistState.Unpersisted, edited.nodes.[fileId].persistState)
    Assert.Equal(PersistState.Persisted, edited.nodes.[dirId].persistState)
    Assert.Equal(PersistState.Persisted, edited.nodes.[wsId].persistState)
    Assert.Equal(ParseState.Parsed, edited.nodes.[fileId].parseState)
    Assert.Equal(ParseState.Unparsed, edited.nodes.[dirId].parseState)
    Assert.Equal(ParseState.Unparsed, edited.nodes.[wsId].parseState)

    let opened =
        edited
        |> apply [ Op.SetDocumentState(dirId, Unparsed, Current) ]
        |> apply [ Op.SetDocumentState(wsId, Unparsed, Current) ]
    let reset =
        opened
        |> fun g -> Graph.setPersistState fileId PersistState.Persisted g |> requireOk
        |> fun g -> Graph.setPersistState dirId PersistState.Persisted g |> requireOk
        |> fun g -> Graph.setPersistState wsId PersistState.Persisted g |> requireOk
    let renamed = apply [ Op.SetText(fileId, "file.txt", "renamed") ] reset
    Assert.Equal(PersistState.Unpersisted, renamed.nodes.[fileId].persistState)
    Assert.Equal(PersistState.Persisted, renamed.nodes.[dirId].persistState)
    Assert.Equal(PersistState.Persisted, renamed.nodes.[wsId].persistState)

[<Fact>]
let ``failed edit of an Unparsed special does not mark Unpersisted`` () =
    let id = NodeId.New()
    let graph = apply [ Op.NewSpecialNode(id, File, "file.txt") ] (Graph.create ())
    Assert.Equal(PersistState.Persisted, graph.nodes.[id].persistState)
    match Op.apply (Op.SetText(id, "file.txt", "changed")) (freshState graph) with
    | ApplyResult.Invalid(state, _) ->
        Assert.Equal(PersistState.Persisted, state.graph.nodes.[id].persistState)
        Assert.Equal(ParseState.Unparsed, state.graph.nodes.[id].parseState)
    | _ -> failwith "expected Invalid"

[<Fact>]
let ``directory Replace marks that directory only`` () =
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let noteId = NodeId.New()
    let graph =
        Graph.create ()
        |> apply [ Op.NewSpecialNode(wsId, Workspace, "ws") ]
        |> place Graph.workspacesId wsId
        |> apply [ Op.NewSpecialNode(dirId, Directory, "dir") ]
        |> place wsId dirId
        |> apply [ Op.SetDocumentState(dirId, Unparsed, Current) ]
        |> fun g -> Graph.setPersistState wsId PersistState.Persisted g |> requireOk
        |> fun g -> Graph.setPersistState dirId PersistState.Persisted g |> requireOk
        |> apply [ Op.NewNode(noteId, "note") ]
    let edited = place dirId noteId graph
    Assert.Equal(PersistState.Unpersisted, edited.nodes.[dirId].persistState)
    Assert.Equal(PersistState.Persisted, edited.nodes.[wsId].persistState)
    Assert.Equal(ParseState.Parsed, edited.nodes.[dirId].parseState)

[<Fact>]
let ``SQL reload derives parse state and defaults persist to Persisted`` () =
    let cases = [ Current; Unparsed; NoServerFile ]
    for documentState in cases do
        let id = NodeId.New()
        let node =
            Node.Create(
                id,
                text = "file.txt",
                name = Filename.create "file.txt",
                kind = Special File,
                documentState = documentState,
                parseState = ParseState.Parsed,
                persistState = PersistState.Unpersisted)
        let graph = Graph.create () |> Graph.addDetachedNode node
        match GraphProjection.graphRoundTrip graph with
        | Error msg -> failwith msg
        | Ok loaded ->
            let back = loaded.nodes.[id]
            Assert.Equal(documentState, back.documentState)
            Assert.Equal(ParseState.ofDocumentState documentState, back.parseState)
            Assert.Equal(PersistState.Persisted, back.persistState)

[<Fact>]
let ``JSON without axes derives parse state and defaults persist`` () =
    let id = NodeId.New()
    let json =
        "{\"id\":\""
        + string id.Value
        + "\",\"text\":\"file\",\"documentState\":\"noServerFile\","
        + "\"kind\":{\"type\":\"special\",\"kind\":\"file\"}}"
    match Dec.fromString Serialization.decodeNode json with
    | Error msg -> failwith msg
    | Ok node ->
        Assert.Equal(NoServerFile, node.documentState)
        Assert.Equal(ParseState.Unparsed, node.parseState)
        Assert.Equal(PersistState.Persisted, node.persistState)

[<Fact>]
let ``SetPersistState op sets the axis and undo restores it`` () =
    let graph, id = artifact File "file.txt"
    let op =
        Op.SetPersistState(id, PersistState.Persisted, PersistState.Unpersisted)
    match Op.apply op (freshState graph) with
    | ApplyResult.Changed next ->
        Assert.Equal(PersistState.Unpersisted, next.graph.nodes.[id].persistState)
        match Op.undo op next with
        | ApplyResult.Changed restored ->
            Assert.Equal(
                PersistState.Persisted,
                restored.graph.nodes.[id].persistState)
        | _ -> failwith "expected undo"
    | _ -> failwith "expected Changed"

[<Fact>]
let ``directory file amb node is excluded from state axes`` () =
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let ambId = NodeId.New()
    let graph =
        Graph.create ()
        |> apply [ Op.NewSpecialNode(wsId, Workspace, "ws") ]
        |> place Graph.workspacesId wsId
        |> apply [ Op.NewSpecialNode(dirId, Directory, "dir") ]
        |> place wsId dirId
        |> Graph.addDetachedNode (
            Node.Create(
                ambId,
                text = ".amb",
                name = Filename.create ".amb",
                kind = Special File))
        |> place dirId ambId
        |> apply [ Op.SetDocumentState(dirId, Unparsed, Current) ]
        |> apply [ Op.SetDocumentState(wsId, Unparsed, Current) ]
        |> fun g -> Graph.setPersistState dirId PersistState.Persisted g |> requireOk
        |> fun g -> Graph.setPersistState wsId PersistState.Persisted g |> requireOk
    Assert.True(Result.isError (Graph.setParseState ambId ParseState.Unparsed graph))
    Assert.True(
        Result.isError (Graph.setPersistState ambId PersistState.Unpersisted graph))
    let edited = apply [ Op.SetText(ambId, ".amb", "changed") ] graph
    Assert.Equal(PersistState.Persisted, edited.nodes.[ambId].persistState)
    Assert.Equal(PersistState.Unpersisted, edited.nodes.[dirId].persistState)
    Assert.Equal(PersistState.Persisted, edited.nodes.[wsId].persistState)
    Assert.Equal(ParseState.Parsed, edited.nodes.[ambId].parseState)
    let stated = apply [ Op.SetDocumentState(ambId, Current, Unparsed) ] graph
    Assert.Equal(Unparsed, stated.nodes.[ambId].documentState)
    Assert.Equal(ParseState.Parsed, stated.nodes.[ambId].parseState)
    Assert.Equal(PersistState.Persisted, stated.nodes.[ambId].persistState)

[<Fact>]
let ``disk parse leaves the file Parsed and Persisted`` () =
    let fileId = NodeId.New()
    let lineId = NodeId.New()
    let graph =
        Graph.create ()
        |> Graph.addDetachedNode (
            Node.Create(
                fileId,
                text = "notes.txt",
                name = Filename.create "notes.txt",
                kind = Special File))
        |> Graph.addDetachedNode (Node.Create(lineId, text = "alpha"))
        |> place fileId lineId
        |> apply [ Op.SetDocumentState(fileId, Current, Unparsed) ]
        |> fun g -> Graph.setPersistState fileId PersistState.Unpersisted g |> requireOk
    let ops =
        match
            ImportDocument.planParseFile
                graph
                fileId
                ("beta" + System.Environment.NewLine)
        with
        | Ok planned -> planned
        | Error err -> failwith err
    let after = apply ops graph
    Assert.Equal(Current, after.nodes.[fileId].documentState)
    Assert.Equal(ParseState.Parsed, after.nodes.[fileId].parseState)
    Assert.Equal(PersistState.Persisted, after.nodes.[fileId].persistState)
    Assert.Equal("beta", after.nodes.[(Graph.children after fileId).Head.id].text)

[<Fact>]
let ``JSON round-trip keeps an explicit Unpersisted axis`` () =
    let id = NodeId.New()
    let node =
        Node.Create(
            id,
            text = "file.txt",
            name = Filename.create "file.txt",
            kind = Special File,
            documentState = Unparsed,
            parseState = ParseState.Unparsed,
            persistState = PersistState.Unpersisted)
    let json = Enc.toString 0 (Serialization.encodeNode node)
    match Dec.fromString Serialization.decodeNode json with
    | Error msg -> failwith msg
    | Ok decoded ->
        Assert.Equal(ParseState.Unparsed, decoded.parseState)
        Assert.Equal(PersistState.Unpersisted, decoded.persistState)
        Assert.Equal(Unparsed, decoded.documentState)
