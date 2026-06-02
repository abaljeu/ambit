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
        { id = NodeId.New()
          text = "hello world"
          name = Filename.create "myname"
          children =
            [ ChildNode.New()
              ChildNode.New()]
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal }
    let decoded = roundTrip Serialization.encodeNode Serialization.decodeNode node
    Assert.Equal(node, decoded)

[<Fact>]
let ``Node round-trip with Empty name`` () =
    let node =
        { id = NodeId.New()
          text = "hello"
          name = Filename.Empty
          children = []
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal }
    let decoded = roundTrip Serialization.encodeNode Serialization.decodeNode node
    Assert.Equal(node, decoded)

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
    let decoded =
        roundTrip
            DesktopCapabilities.encode
            DesktopCapabilities.decoder
            DesktopCapabilities.desktopEnabled

    Assert.Equal(DesktopCapabilities.desktopEnabled, decoded)

[<Fact>]
let ``Desktop capabilities enabled use stable file keys`` () =
    let json = Enc.toString 0 (DesktopCapabilities.encode DesktopCapabilities.desktopEnabled)

    Assert.Equal(DesktopCapabilities.desktopEnabledJson, json)

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
    let ack =
        { revision = Revision 7
          ackedChangeIds = [ System.Guid.NewGuid(); System.Guid.NewGuid() ] }
    let decoded = roundTrip Serialization.encodeChangeBatchAck Serialization.decodeChangeBatchAck ack
    Assert.Equal(ack.revision, decoded.revision)
    Assert.Equal<System.Guid list>(ack.ackedChangeIds, decoded.ackedChangeIds)

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
          changes = [ change ] }
    let decoded =
        roundTrip Serialization.encodePollResponse Serialization.decodePollResponseDecoder poll
    Assert.Equal(poll.revision, decoded.revision)
    Assert.Equal(poll.buildEpochSec, decoded.buildEpochSec)
    Assert.Equal(poll.pageBuildEpochSec, decoded.pageBuildEpochSec)
    Assert.Equal(1, decoded.changes.Length)
    Assert.Equal(change.id, decoded.changes.[0].id)
    Assert.Equal<Op list>(change.ops, decoded.changes.[0].ops)

[<Fact>]
let ``PollResponse round-trip with empty changes`` () =
    let poll =
        { revision = 5
          buildEpochSec = 0
          pageBuildEpochSec = 0
          changes = [] }
    let decoded =
        roundTrip Serialization.encodePollResponse Serialization.decodePollResponseDecoder poll
    Assert.Equal(poll.revision, decoded.revision)
    Assert.Equal<Change list>([], decoded.changes)

[<Fact>]
let ``PollResponse decoder tolerates missing changes field`` () =
    let json = """{"r":4,"b":100,"p":200}"""
    match Dec.fromString Serialization.decodePollResponseDecoder json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok decoded ->
        Assert.Equal(4, decoded.revision)
        Assert.Equal<Change list>([], decoded.changes)
