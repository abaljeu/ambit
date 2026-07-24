module Gambol.Shared.Tests.SerializationTests

open Xunit
open Gambol.Shared

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

let private roundTrip encode decode value =
    let json = Enc.toString 0 (encode value)
    match Dec.fromString decode json with
    | Ok decoded -> decoded
    | Error err -> failwith $"Decode failed: {err}"

[<Fact>]
let ``NodeId round-trip`` () =
    let nodeId = NodeId.New()
    let decoded = roundTrip Serialization.encodeNodeId Serialization.decodeNodeId nodeId
    Assert.Equal(nodeId, decoded)

[<Fact>]
let ``Revision round-trip`` () =
    let rev = Revision 42
    let decoded = roundTrip Serialization.encodeRevision Serialization.decodeRevision rev
    Assert.Equal(rev, decoded)

[<Fact>]
let ``Node round-trip with Ok name`` () =
    let node =
        Node.Create(
            NodeId.New(),
            text = "hello world",
            name = Filename.create "myname",
            children =
              [ ChildNode.New()
                ChildNode.New()],
            updateTime = System.DateTime(2024, 6, 1, 12, 0, 0, System.DateTimeKind.Utc))
    let decoded = roundTrip Serialization.encodeNode Serialization.decodeNode node
    Assert.Equal(node, decoded)

[<Fact>]
let ``Node round-trip with Empty name`` () =
    let node =
        Node.Create(NodeId.New(), text = "hello")
    let decoded = roundTrip Serialization.encodeNode Serialization.decodeNode node
    Assert.Equal(node, decoded)

[<Fact>]
let ``Node decode without updateTime uses missing sentinel`` () =
    let nodeId = NodeId.New()
    let json =
        $"""{{"id":"{nodeId.Value}","text":"legacy","children":[],"cssClasses":[],"kind":"normal"}}"""

    match Dec.fromString Serialization.decodeNode json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok decoded -> Assert.Equal(NodeUpdateTime.missing, decoded.updateTime)

[<Fact>]
let ``Node decode without documentState defaults to current`` () =
    let nodeId = NodeId.New()
    let json =
        $"""{{"id":"{nodeId.Value}","text":"legacy","children":[],"cssClasses":[],"kind":"normal"}}"""
    match Dec.fromString Serialization.decodeNode json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok decoded -> Assert.Equal(Current, decoded.documentState)

[<Fact>]
let ``Unparsed node round-trip`` () =
    let node =
        Node.Create(
            NodeId.New(),
            text = "file",
            kind = Special File,
            documentState = Unparsed)
    let decoded = roundTrip Serialization.encodeNode Serialization.decodeNode node
    Assert.Equal(Unparsed, decoded.documentState)

[<Fact>]
let ``Graph round-trip`` () =
    let graph = ModelBuilder.createDag12 ()
    let decoded = roundTrip Serialization.encodeGraph Serialization.decodeGraph graph
    Assert.Equal(graph.root, decoded.root)
    Assert.Equal<Map<NodeId, Node>>(graph.nodes, decoded.nodes)

[<Fact>]
let ``Desktop capabilities disabled round-trip`` () =
    let decoded =
        roundTrip
            DesktopCapabilities.encode
            DesktopCapabilities.decoder
            DesktopCapabilities.disabled

    Assert.Equal(DesktopCapabilities.disabled, decoded)

[<Fact>]
let ``Desktop capabilities disabled use stable file keys`` () =
    let json = Enc.toString 0 (DesktopCapabilities.encode DesktopCapabilities.disabled)

    Assert.Equal(DesktopCapabilities.disabledJson, json)

[<Fact>]
let ``Desktop capabilities enabled round-trip`` () =
    let enabled = DesktopCapabilities.desktopEnabled true
    let decoded =
        roundTrip
            DesktopCapabilities.encode
            DesktopCapabilities.decoder
            enabled

    Assert.Equal(enabled, decoded)

[<Fact>]
let ``Desktop capabilities enabled use stable file keys`` () =
    let enabled = DesktopCapabilities.desktopEnabled true
    let json = Enc.toString 0 (DesktopCapabilities.encode enabled)

    Assert.Equal(DesktopCapabilities.desktopEnabledJson true, json)

[<Fact>]
let ``Op.NewNode round-trip`` () =
    let op = Op.NewNode(NodeId.New(), "new text")
    let decoded = roundTrip Serialization.encodeOp Serialization.decodeOp op
    Assert.Equal(op, decoded)

[<Fact>]
let ``Op.SetText round-trip`` () =
    let op = Op.SetText(NodeId.New(), "old", "new")
    let decoded = roundTrip Serialization.encodeOp Serialization.decodeOp op
    Assert.Equal(op, decoded)

[<Fact>]
let ``Op.Replace round-trip`` () =
    let op = Op.Replace(NodeId.New(), 2, [ ChildNode.New() ], [ ChildNode.New(); ChildNode.New() ])
    let decoded = roundTrip Serialization.encodeOp Serialization.decodeOp op
    Assert.Equal(op, decoded)

[<Fact>]
let ``Op.Replace round-trip preserves child ownership`` () =
    let shared = NodeId.New()
    let op =
        Op.Replace(
            NodeId.New(),
            0,
            [],
            [ { ref = Ownership.Owner; id = shared }
              { ref = Ownership.Ref; id = shared } ])
    let decoded = roundTrip Serialization.encodeOp Serialization.decodeOp op
    Assert.Equal(op, decoded)

[<Fact>]
let ``Op.SetDocumentState round-trip`` () =
    let op = Op.SetDocumentState(NodeId.New(), Current, Unparsed)
    let decoded = roundTrip Serialization.encodeOp Serialization.decodeOp op
    Assert.Equal(op, decoded)

[<Fact>]
let ``Op.SetUpdateTime round-trip`` () =
    let stamp = System.DateTime(2026, 7, 22, 12, 0, 0, System.DateTimeKind.Utc)
    let op =
        Op.SetUpdateTime(
            NodeId.New(),
            NodeUpdateTime.missing,
            NodeUpdateTime.toDbPrecision stamp)
    let decoded = roundTrip Serialization.encodeOp Serialization.decodeOp op
    Assert.Equal(op, decoded)

[<Fact>]
let ``Change round-trip`` () =
    let change =
        { id = 5
          changeId = System.Guid.NewGuid()
          ops =
            [ Op.NewNode(NodeId.New(), "hello")
              Op.SetText(NodeId.New(), "old", "new")
              Op.Replace(NodeId.New(), 0, [], [ ChildNode.New() ]) ] }
    let decoded = roundTrip Serialization.encodeChange Serialization.decodeChange change
    Assert.Equal(change.id, decoded.id)
    Assert.Equal<Op list>(change.ops, decoded.ops)

[<Fact>]
let ``ChangeBatch round-trip`` () =
    let change =
        { id = 5
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(NodeId.New(), "old", "new") ] }
    let batch = { changes = [ change ] }
    let decoded = roundTrip Serialization.encodeChangeBatch Serialization.decodeChangeBatch batch
    Assert.Equal<Change list>(batch.changes, decoded.changes)

[<Fact>]
let ``ChangeBatch decoder rejects empty changes`` () =
    let json = """{"changes":[]}"""
    match Dec.fromString Serialization.decodeChangeBatch json with
    | Ok _ -> failwith "Expected empty batch to fail decoding"
    | Error _ -> ()

[<Fact>]
let ``ChangeBatchAck round-trip`` () =
    let stamp = System.DateTime(2026, 7, 22, 12, 0, 0, System.DateTimeKind.Utc)
    let ack =
        { revision = Revision 7
          ackedChangeIds = [ System.Guid.NewGuid(); System.Guid.NewGuid() ]
          stampOps =
              [ Op.SetUpdateTime(
                    NodeId.New(),
                    NodeUpdateTime.missing,
                    NodeUpdateTime.toDbPrecision stamp) ] }
    let decoded = roundTrip Serialization.encodeChangeBatchAck Serialization.decodeChangeBatchAck ack
    Assert.Equal(ack.revision, decoded.revision)
    Assert.Equal<System.Guid list>(ack.ackedChangeIds, decoded.ackedChangeIds)
    Assert.Equal<Op list>(ack.stampOps, decoded.stampOps)

[<Fact>]
let ``ChangeBatchAck omits stampOps when decoding legacy JSON`` () =
    let json = """{"revision":3,"ackedChangeIds":[]}"""
    match Dec.fromString Serialization.decodeChangeBatchAck json with
    | Error e -> failwith e
    | Ok ack ->
        Assert.Equal(Revision 3, ack.revision)
        Assert.True(ack.stampOps.IsEmpty)

[<Fact>]
let ``PollResponse round-trip with non-empty changes`` () =
    let change =
        { id = 3
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(NodeId.New(), "old", "new") ] }
    let poll =
        { revision = 7
          buildEpochSec = 100
          pageBuildEpochSec = 200
          isReady = false
          changes = [ change ] }
    let decoded =
        roundTrip
            ApiResponseSerialization.encodePollResponse
            ApiResponseSerialization.decodePollResponseDecoder
            poll
    Assert.Equal(poll.revision, decoded.revision)
    Assert.Equal(poll.buildEpochSec, decoded.buildEpochSec)
    Assert.Equal(poll.pageBuildEpochSec, decoded.pageBuildEpochSec)
    Assert.False(decoded.isReady)
    Assert.Equal(1, decoded.changes.Length)
    Assert.Equal(change.id, decoded.changes.[0].id)
    Assert.Equal<Op list>(change.ops, decoded.changes.[0].ops)

[<Fact>]
let ``PollResponse round-trip with empty changes`` () =
    let poll =
        { revision = 5
          buildEpochSec = 0
          pageBuildEpochSec = 0
          isReady = true
          changes = [] }
    let decoded =
        roundTrip
            ApiResponseSerialization.encodePollResponse
            ApiResponseSerialization.decodePollResponseDecoder
            poll
    Assert.Equal(poll.revision, decoded.revision)
    Assert.Equal<Change list>([], decoded.changes)

[<Fact>]
let ``PollResponse decoder tolerates missing changes field`` () =
    let json = """{"r":4,"b":100,"p":200}"""
    match Dec.fromString ApiResponseSerialization.decodePollResponseDecoder json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok decoded ->
        Assert.Equal(4, decoded.revision)
        Assert.True(decoded.isReady)
        Assert.Equal<Change list>([], decoded.changes)

[<Fact>]
let ``StateResponse round-trip preserves startup readiness`` () =
    let response =
        { graph = Graph.create ()
          revision = Revision 3
          isReady = false }
        : StateResponse
    let decoded =
        roundTrip
            ApiResponseSerialization.encodeStateResponse
            ApiResponseSerialization.decodeStateResponseDecoder
            response

    Assert.Equal(response.revision, decoded.revision)
    Assert.False(decoded.isReady)
    Assert.True(GraphProjection.graphEquals response.graph decoded.graph)
