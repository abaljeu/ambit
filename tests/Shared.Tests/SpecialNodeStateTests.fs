module SpecialNodeStateTests

open Gambol.Shared
open GraphChildMapHelpers
open Xunit

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private requireChanged (r: ApplyResult) =
    match r with
    | ApplyResult.Changed s -> s
    | ApplyResult.Unchanged _ -> failwith "expected Changed, got Unchanged"
    | ApplyResult.Invalid(_, msg) -> failwithf "expected Changed, got Invalid: %s" msg

let private freshState () =
    { graph = Graph.create (); eventId = EventId.zero }

let private createSpecial kind name =
    let nodeId = NodeId.New()
    let state =
        Op.apply (Op.NewSpecialNode(nodeId, kind, name)) (freshState ())
        |> requireChanged
    nodeId, state.graph

[<Fact>]
let ``NewSpecialNode starts Unparsed and Persisted`` () =
    for kind, name in [ Workspace, "home"; Directory, "docs"; File, "note.txt" ] do
        let nodeId, graph = createSpecial kind name
        let node = graph.nodes.[nodeId]
        Assert.Equal(Unparsed, node.documentState)
        Assert.Equal(ParseState.Unparsed, node.parseState)
        Assert.Equal(PersistState.Persisted, node.persistState)

[<Fact>]
let ``Workspace Directory and File can set and read both axes`` () =
    for kind, name in [ Workspace, "home"; Directory, "docs"; File, "note.txt" ] do
        let nodeId, graph0 = createSpecial kind name
        let graph1 =
            Graph.setParseState nodeId ParseState.Unparsed ParseState.Parsed graph0
            |> requireOk "setParseState"
        Assert.Equal(ParseState.Parsed, graph1.nodes.[nodeId].parseState)
        let graph2 =
            Graph.setPersistState
                nodeId PersistState.Persisted PersistState.Unpersisted graph1
            |> requireOk "setPersistState"
        Assert.Equal(PersistState.Unpersisted, graph2.nodes.[nodeId].persistState)
        Assert.Equal(ParseState.Parsed, graph2.nodes.[nodeId].parseState)

[<Fact>]
let ``SetDocumentState dual-writes the parse axis`` () =
    let nodeId = NodeId.New()
    let file =
        Node.Create(
            nodeId,
            text = "note.txt",
            name = Filename.Ok "note.txt",
            kind = Special File)
    let graph0 = Graph.addDetachedNode file (Graph.create ())
    Assert.Equal(ParseState.Parsed, graph0.nodes.[nodeId].parseState)
    let graph1 =
        Graph.setDocumentState nodeId Current Unparsed graph0
        |> requireOk "to Unparsed"
    Assert.Equal(Unparsed, graph1.nodes.[nodeId].documentState)
    Assert.Equal(ParseState.Unparsed, graph1.nodes.[nodeId].parseState)
    let graph2 =
        Graph.setDocumentState nodeId Unparsed Current graph1
        |> requireOk "to Current"
    Assert.Equal(Current, graph2.nodes.[nodeId].documentState)
    Assert.Equal(ParseState.Parsed, graph2.nodes.[nodeId].parseState)
    let graph3 =
        Graph.setDocumentState nodeId Current NoServerFile graph2
        |> requireOk "to NoServerFile"
    Assert.Equal(NoServerFile, graph3.nodes.[nodeId].documentState)
    Assert.Equal(ParseState.Unparsed, graph3.nodes.[nodeId].parseState)

[<Fact>]
let ``graph edit marks nearest owning special Unpersisted only`` () =
    let wsId, dirId, fileId, bodyId =
        NodeId.New(), NodeId.New(), NodeId.New(), NodeId.New()
    let ws =
        Node.Create(
            wsId,
            text = "home",
            name = Filename.Ok "home",
            kind = Special Workspace)
    let dir =
        Node.Create(
            dirId,
            text = "docs",
            name = Filename.Ok "docs",
            kind = Special Directory,
            owner = wsId)
    let file =
        Node.Create(
            fileId,
            text = "note.txt",
            name = Filename.Ok "note.txt",
            kind = Special File,
            owner = dirId)
    let body = Node.Create(bodyId, text = "body", owner = fileId)
    let graph0 =
        Graph.create ()
        |> addDetachedMany [ ws; dir; file; body ]
        |> appendKids Graph.rootId [ ChildNode.owner wsId ]
        |> setChildren wsId [ ChildNode.owner dirId ]
        |> setChildren dirId [ ChildNode.owner fileId ]
        |> setChildren fileId [ ChildNode.owner bodyId ]
    Assert.Equal(PersistState.Persisted, graph0.nodes.[wsId].persistState)
    Assert.Equal(PersistState.Persisted, graph0.nodes.[dirId].persistState)
    Assert.Equal(PersistState.Persisted, graph0.nodes.[fileId].persistState)
    let graph1 =
        Graph.setText bodyId "body" "edited" graph0 |> requireOk "setText"
    Assert.Equal(PersistState.Unpersisted, graph1.nodes.[fileId].persistState)
    Assert.Equal(PersistState.Persisted, graph1.nodes.[dirId].persistState)
    Assert.Equal(PersistState.Persisted, graph1.nodes.[wsId].persistState)

[<Fact>]
let ``setParseState and setPersistState reject Normal and Workspaces`` () =
    let graph = Graph.create ()
    let graph1, normalId = Graph.newNode "plain" graph
    match Graph.setParseState normalId ParseState.Parsed ParseState.Unparsed graph1 with
    | Error msg -> Assert.Contains("normal", msg)
    | Ok _ -> Assert.Fail("expected Error for Normal parse")
    match
        Graph.setPersistState
            Graph.workspacesId
            PersistState.Persisted
            PersistState.Unpersisted
            graph1
    with
    | Error msg -> Assert.Contains("workspaces", msg)
    | Ok _ -> Assert.Fail("expected Error for Workspaces persist")

[<Fact>]
let ``legacy node JSON derives parse axis from documentState`` () =
    let nodeId = NodeId.New()
    let json =
        "{\"id\":\""
        + string nodeId.Value
        + "\",\"text\":\"file\",\"children\":[],"
        + "\"cssClasses\":[],\"kind\":{\"type\":\"special\",\"kind\":\"file\"},"
        + "\"documentState\":\"unparsed\"}"
    match Dec.fromString Serialization.decodeNode json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok decoded ->
        Assert.Equal(ParseState.Unparsed, decoded.parseState)
        Assert.Equal(PersistState.Persisted, decoded.persistState)

[<Fact>]
let ``parse and persist axes round-trip on a File Node`` () =
    let node =
        Node.Create(
            NodeId.New(),
            text = "note.txt",
            name = Filename.Ok "note.txt",
            kind = Special File,
            parseState = ParseState.Unparsed,
            persistState = PersistState.Unpersisted)
    let json = Enc.toString 0 (Serialization.encodeNode node)
    match Dec.fromString Serialization.decodeNode json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok decoded ->
        Assert.Equal(ParseState.Unparsed, decoded.parseState)
        Assert.Equal(PersistState.Unpersisted, decoded.persistState)
