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
let ``special node stores parse persist and noServerFile only inside DocumentState`` () =
    let id = NodeId.New()
    let graph =
        apply [ Op.NewSpecialNode(id, File, "file.txt") ] (Graph.create ())
    let created = graph.nodes.[id]
    Assert.Equal(ParseState.Unparsed, created.documentState.parseState)
    Assert.Equal(PersistState.Persisted, created.documentState.persistState)
    Assert.False(created.documentState.noServerFile)

    let parsed = Graph.setParseState id ParseState.Parsed graph |> requireOk
    let parsedNode = parsed.nodes.[id]
    Assert.Equal(ParseState.Parsed, parsedNode.documentState.parseState)
    Assert.Equal(PersistState.Persisted, parsedNode.documentState.persistState)
    Assert.False(parsedNode.documentState.noServerFile)

    let dirty =
        Graph.setPersistState id PersistState.Unpersisted parsed |> requireOk
    let dirtyNode = dirty.nodes.[id]
    Assert.Equal(
        PersistState.Unpersisted,
        dirtyNode.documentState.persistState)
    Assert.Equal(ParseState.Parsed, dirtyNode.documentState.parseState)
    Assert.False(dirtyNode.documentState.noServerFile)

    let absent =
        apply [ Op.SetDocumentState(id, Current, NoServerFile) ] dirty
    let absentNode = absent.nodes.[id]
    Assert.True(absentNode.documentState.noServerFile)
    Assert.Equal(ParseState.Unparsed, absentNode.documentState.parseState)
    Assert.Equal(
        PersistState.Unpersisted,
        absentNode.documentState.persistState)

    let stub =
        Node.Create(
            NodeId.New(),
            text = "stub.txt",
            name = Filename.create "stub.txt",
            kind = Special File,
            documentState = NoServerFile)
    Assert.True(stub.documentState.noServerFile)
    Assert.Equal(ParseState.Unparsed, stub.documentState.parseState)
    Assert.Equal(PersistState.Persisted, stub.documentState.persistState)

[<Fact>]
let ``each special kind sets and resets both axes`` () =
    for kind, name in kinds do
        let graph, id = artifact kind name
        let unparsed = Graph.setParseState id ParseState.Unparsed graph |> requireOk
        Assert.Equal(ParseState.Unparsed, unparsed.nodes.[id].documentState.parseState)
        Assert.Equal(Unparsed, unparsed.nodes.[id].documentState.AsEnum)
        Assert.Equal(PersistState.Persisted, unparsed.nodes.[id].documentState.persistState)

        let parsed = Graph.setParseState id ParseState.Parsed unparsed |> requireOk
        Assert.Equal(ParseState.Parsed, parsed.nodes.[id].documentState.parseState)
        Assert.Equal(Current, parsed.nodes.[id].documentState.AsEnum)
        Assert.Equal(PersistState.Persisted, parsed.nodes.[id].documentState.persistState)

        let dirty = Graph.setPersistState id PersistState.Unpersisted parsed |> requireOk
        Assert.Equal(PersistState.Unpersisted, dirty.nodes.[id].documentState.persistState)
        Assert.Equal(ParseState.Parsed, dirty.nodes.[id].documentState.parseState)
        Assert.Equal(Current, dirty.nodes.[id].documentState.AsEnum)

        let clean = Graph.setPersistState id PersistState.Persisted dirty |> requireOk
        Assert.Equal(PersistState.Persisted, clean.nodes.[id].documentState.persistState)
        Assert.Equal(ParseState.Parsed, clean.nodes.[id].documentState.parseState)
        Assert.Equal(Current, clean.nodes.[id].documentState.AsEnum)

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
        Assert.Equal(Unparsed, node.documentState.AsEnum)
        Assert.Equal(ParseState.Unparsed, node.documentState.parseState)
        Assert.Equal(PersistState.Persisted, node.documentState.persistState)

[<Fact>]
let ``SetDocumentState writes parseState and noServerFile together`` () =
    let graph, id = artifact File "file.txt"
    let dirty = Graph.setPersistState id PersistState.Unpersisted graph |> requireOk
    let unparsed =
        apply [ Op.SetDocumentState(id, Current, Unparsed) ] dirty
    Assert.Equal(Unparsed, unparsed.nodes.[id].documentState.AsEnum)
    Assert.Equal(ParseState.Unparsed, unparsed.nodes.[id].documentState.parseState)
    Assert.False(unparsed.nodes.[id].documentState.noServerFile)
    Assert.Equal(PersistState.Unpersisted, unparsed.nodes.[id].documentState.persistState)

    let parsed =
        apply [ Op.SetDocumentState(id, Unparsed, Current) ] unparsed
    Assert.Equal(Current, parsed.nodes.[id].documentState.AsEnum)
    Assert.Equal(ParseState.Parsed, parsed.nodes.[id].documentState.parseState)
    Assert.False(parsed.nodes.[id].documentState.noServerFile)
    Assert.Equal(PersistState.Unpersisted, parsed.nodes.[id].documentState.persistState)

    let absent =
        apply [ Op.SetDocumentState(id, Current, NoServerFile) ] parsed
    Assert.Equal(NoServerFile, absent.nodes.[id].documentState.AsEnum)
    Assert.Equal(ParseState.Unparsed, absent.nodes.[id].documentState.parseState)
    Assert.True(absent.nodes.[id].documentState.noServerFile)
    Assert.Equal(PersistState.Unpersisted, absent.nodes.[id].documentState.persistState)

let private markPersisted id graph =
    Graph.setPersistState id PersistState.Persisted graph |> requireOk

let private persistedOwnedFile () =
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
        |> markPersisted wsId
        |> markPersisted dirId
        |> markPersisted fileId
    graph, wsId, dirId, fileId, childId

[<Fact>]
let ``graph edit marks only the nearest owning special Unpersisted`` () =
    let graph, wsId, dirId, fileId, childId = persistedOwnedFile ()
    let edited = apply [ Op.SetText(childId, "body", "edited") ] graph
    Assert.Equal(PersistState.Unpersisted, edited.nodes.[fileId].documentState.persistState)
    Assert.Equal(PersistState.Persisted, edited.nodes.[dirId].documentState.persistState)
    Assert.Equal(PersistState.Persisted, edited.nodes.[wsId].documentState.persistState)
    Assert.Equal(ParseState.Parsed, edited.nodes.[fileId].documentState.parseState)
    Assert.Equal(ParseState.Unparsed, edited.nodes.[dirId].documentState.parseState)
    Assert.Equal(ParseState.Unparsed, edited.nodes.[wsId].documentState.parseState)

    let opened =
        edited
        |> apply [ Op.SetDocumentState(dirId, Unparsed, Current) ]
        |> apply [ Op.SetDocumentState(wsId, Unparsed, Current) ]
    let reset =
        opened
        |> markPersisted fileId
        |> markPersisted dirId
        |> markPersisted wsId
    let renamed = apply [ Op.SetText(fileId, "file.txt", "renamed") ] reset
    Assert.Equal(PersistState.Unpersisted, renamed.nodes.[fileId].documentState.persistState)
    Assert.Equal(PersistState.Persisted, renamed.nodes.[dirId].documentState.persistState)
    Assert.Equal(PersistState.Persisted, renamed.nodes.[wsId].documentState.persistState)

[<Fact>]
let ``failed edit of an Unparsed special does not mark Unpersisted`` () =
    let id = NodeId.New()
    let graph = apply [ Op.NewSpecialNode(id, File, "file.txt") ] (Graph.create ())
    Assert.Equal(PersistState.Persisted, graph.nodes.[id].documentState.persistState)
    match Op.apply (Op.SetText(id, "file.txt", "changed")) (freshState graph) with
    | ApplyResult.Invalid(state, _) ->
        Assert.Equal(PersistState.Persisted, state.graph.nodes.[id].documentState.persistState)
        Assert.Equal(ParseState.Unparsed, state.graph.nodes.[id].documentState.parseState)
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
    Assert.Equal(PersistState.Unpersisted, edited.nodes.[dirId].documentState.persistState)
    Assert.Equal(PersistState.Persisted, edited.nodes.[wsId].documentState.persistState)
    Assert.Equal(ParseState.Parsed, edited.nodes.[dirId].documentState.parseState)

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
                persistState = PersistState.Unpersisted)
        let graph = Graph.create () |> Graph.addDetachedNode node
        match GraphProjection.graphRoundTrip graph with
        | Error msg -> failwith msg
        | Ok loaded ->
            let back = loaded.nodes.[id]
            Assert.Equal(documentState, back.documentState.AsEnum)
            Assert.Equal(ParseState.ofDocumentState documentState, back.documentState.parseState)
            Assert.Equal(PersistState.Persisted, back.documentState.persistState)

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
        Assert.Equal(NoServerFile, node.documentState.AsEnum)
        Assert.Equal(ParseState.Unparsed, node.documentState.parseState)
        Assert.Equal(PersistState.Persisted, node.documentState.persistState)

[<Fact>]
let ``graph update sets persist state`` () =
    let graph, id = artifact File "file.txt"
    let dirty =
        Graph.setPersistState id PersistState.Unpersisted graph |> requireOk
    Assert.Equal(PersistState.Unpersisted, dirty.nodes.[id].documentState.persistState)
    Assert.Empty(PersistStamp.opsBetween graph dirty)
    let clean =
        Graph.setPersistState id PersistState.Persisted dirty |> requireOk
    Assert.Equal(PersistState.Persisted, clean.nodes.[id].documentState.persistState)

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
    Assert.Equal(PersistState.Persisted, edited.nodes.[ambId].documentState.persistState)
    Assert.Equal(PersistState.Unpersisted, edited.nodes.[dirId].documentState.persistState)
    Assert.Equal(PersistState.Persisted, edited.nodes.[wsId].documentState.persistState)
    Assert.Equal(ParseState.Parsed, edited.nodes.[ambId].documentState.parseState)
    let stated = apply [ Op.SetDocumentState(ambId, Current, Unparsed) ] graph
    Assert.Equal(Unparsed, stated.nodes.[ambId].documentState.AsEnum)
    Assert.Equal(ParseState.Unparsed, stated.nodes.[ambId].documentState.parseState)
    Assert.Equal(PersistState.Persisted, stated.nodes.[ambId].documentState.persistState)

let private applyDiskParse (ops: Op list) (graph: Graph) : Graph =
    let state = { graph = graph; eventId = EventId.zero }
    let result, _, _ = ChangeAmendment.applyForCommand "Parse" ops state
    match result with
    | ApplyResult.Changed next
    | ApplyResult.Unchanged next -> next.graph
    | ApplyResult.Invalid(_, msg) -> failwith msg

[<Fact>]
let ``disk parse ops set Parsed and keep PersistState`` () =
    for persist in [ PersistState.Persisted; PersistState.Unpersisted ] do
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
            |> fun g -> Graph.setParseState fileId ParseState.Unparsed g |> requireOk
            |> fun g -> Graph.setPersistState fileId persist g |> requireOk
        let ops =
            match
                ImportDocument.planParseFile
                    graph
                    fileId
                    ("beta" + System.Environment.NewLine)
            with
            | Ok planned -> planned
            | Error err -> failwith err
        Assert.False(
            ops
            |> List.exists (function
                | Op.SetDocumentState _ -> true
                | _ -> false))
        let opened =
            Graph.setParseState fileId ParseState.Parsed graph |> requireOk
        let after = applyDiskParse ops opened
        Assert.Equal(Current, after.nodes.[fileId].documentState.AsEnum)
        Assert.Equal(ParseState.Parsed, after.nodes.[fileId].documentState.parseState)
        Assert.Equal(persist, after.nodes.[fileId].documentState.persistState)
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
        Assert.Equal(ParseState.Unparsed, decoded.documentState.parseState)
        Assert.Equal(PersistState.Unpersisted, decoded.documentState.persistState)
        Assert.Equal(Unparsed, decoded.documentState.AsEnum)
