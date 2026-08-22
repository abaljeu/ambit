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
let ``Node childrenStatus Unloaded round-trip`` () =
    let node = Node.Create(NodeId.New(), text = "hollow", childrenStatus = Unloaded)
    let decoded = roundTrip Serialization.encodeNode Serialization.decodeNode node
    Assert.Equal(Unloaded, decoded.childrenStatus)
    Assert.Equal(node, decoded)

[<Fact>]
let ``Node decode without childrenStatus defaults to Loaded`` () =
    let nodeId = NodeId.New()
    let json =
        $"""{{"id":"{nodeId.Value}","text":"legacy","children":[],"cssClasses":[],"kind":"normal"}}"""
    match Dec.fromString Serialization.decodeNode json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok decoded -> Assert.Equal(Loaded, decoded.childrenStatus)

[<Fact>]
let ``Node decode rejects Unloaded with non-empty children`` () =
    let nodeId = NodeId.New()
    let childId = NodeId.New()
    let json =
        $"""{{"id":"{nodeId.Value}","text":"bad","children":[{{"ref":"owner","id":"{childId.Value}"}}],"childrenStatus":"unloaded","cssClasses":[],"kind":"normal"}}"""
    match Dec.fromString Serialization.decodeNode json with
    | Ok _ -> failwith "expected decode failure"
    | Error err -> Assert.Contains("Unloaded", err)

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
let ``NoServerFile node and state op round-trip`` () =
    let node =
        Node.Create(
            NodeId.New(),
            text = "file",
            kind = Special File,
            documentState = NoServerFile)
    let decoded = roundTrip Serialization.encodeNode Serialization.decodeNode node
    Assert.Equal(NoServerFile, decoded.documentState)

    let op =
        Op.SetDocumentState(node.id, NoServerFile, Unparsed)
    Assert.Equal(op, roundTrip Serialization.encodeOp Serialization.decodeOp op)

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
            [ ChildNode.owner shared
              ChildNode.reference shared ])
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
let ``ChangeBatch round-trip preserves request order`` () =
    let first =
        { id = 5
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(NodeId.New(), "old", "new") ] }
    let second =
        { id = 6
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(NodeId.New(), "a", "b") ] }
    let batch = { changes = [ first; second ] }
    let json = Enc.toString 0 (Serialization.encodeChangeBatch batch)
    Assert.DoesNotContain("\"action\":\"undo\"", json)
    Assert.DoesNotContain("\"action\":\"redo\"", json)
    let decoded =
        roundTrip Serialization.encodeChangeBatch Serialization.decodeChangeBatch batch
    Assert.Equal<Change list>([ first; second ], decoded.changes)

[<Fact>]
let ``ChangeBatch decoder rejects empty changes`` () =
    let json = """{"changes":[]}"""
    match Dec.fromString Serialization.decodeChangeBatch json with
    | Ok _ -> failwith "Expected empty batch to fail decoding"
    | Error _ -> ()

[<Fact>]
let ``ChangeBatch decoder rejects explicit Undo JSON`` () =
    let json =
        """{"changes":[{"action":"undo","id":1,"changeId":"00000000-0000-0000-0000-000000000001"}]}"""
    match Dec.fromString Serialization.decodeChangeBatch json with
    | Ok _ -> failwith "Expected explicit Undo JSON to fail decoding"
    | Error _ -> ()

[<Fact>]
let ``ChangeBatch decoder rejects explicit Redo JSON`` () =
    let json =
        """{"changes":[{"action":"redo","id":1,"changeId":"00000000-0000-0000-0000-000000000001"}]}"""
    match Dec.fromString Serialization.decodeChangeBatch json with
    | Ok _ -> failwith "Expected explicit Redo JSON to fail decoding"
    | Error _ -> ()

[<Fact>]
let ``ChangeSuccessResponse round-trip with non-empty Changes`` () =
    let change =
        { id = 3
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(NodeId.New(), "old", "new") ] }
    let response: ChangeSuccessResponse =
        { revision = Revision 7
          buildEpochSec = 100
          pageBuildEpochSec = 200
          isReady = false
          externalChanges = true
          changes = [ change ]
          message = Some "stable file update failed" }
    let decoded =
        roundTrip
            ApiResponseSerialization.encodeChangeSuccessResponse
            ApiResponseSerialization.decodeChangeSuccessResponseDecoder
            response
    Assert.Equal(response.revision, decoded.revision)
    Assert.Equal(response.buildEpochSec, decoded.buildEpochSec)
    Assert.Equal(response.pageBuildEpochSec, decoded.pageBuildEpochSec)
    Assert.False(decoded.isReady)
    Assert.True(decoded.externalChanges)
    Assert.Equal(1, decoded.changes.Length)
    Assert.Equal(change.id, decoded.changes.[0].id)
    Assert.Equal<Op list>(change.ops, decoded.changes.[0].ops)
    Assert.Equal(response.message, decoded.message)

[<Fact>]
let ``ChangeSuccessResponse round-trip with empty Changes`` () =
    let response: ChangeSuccessResponse =
        { revision = Revision 5
          buildEpochSec = 0
          pageBuildEpochSec = 0
          isReady = true
          externalChanges = false
          changes = []
          message = None }
    let decoded =
        roundTrip
            ApiResponseSerialization.encodeChangeSuccessResponse
            ApiResponseSerialization.decodeChangeSuccessResponseDecoder
            response
    Assert.Equal(response.revision, decoded.revision)
    Assert.False(decoded.externalChanges)
    Assert.Equal<Change list>([], decoded.changes)
    Assert.Equal(None, decoded.message)

[<Fact>]
let ``LoadRequest round-trip`` () =
    let request: LoadRequest =
        { revision = 11
          targets =
            [ { targetId = NodeId.New(); includeWorkspace = true }
              { targetId = NodeId.New(); includeWorkspace = false } ] }
    let decoded =
        roundTrip
            ApiResponseSerialization.encodeLoadRequest
            ApiResponseSerialization.decodeLoadRequestDecoder
            request
    Assert.Equal(request.revision, decoded.revision)
    Assert.Equal(2, decoded.targets.Length)
    Assert.Equal(request.targets.[0].targetId, decoded.targets.[0].targetId)
    Assert.True(decoded.targets.[0].includeWorkspace)
    Assert.False(decoded.targets.[1].includeWorkspace)

[<Fact>]
let ``LoadResponse round-trip with packages`` () =
    let node =
        Node.Create(NodeId.New(), text = "ws child", owner = Graph.rootId)
    let change =
        { id = 2
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(node.id, "a", "b") ] }
    let response: LoadResponse =
        { revision = 8
          buildEpochSec = 10
          pageBuildEpochSec = 20
          isReady = false
          changes = [ change ]
          packages = [ node ] }
    let decoded =
        roundTrip
            ApiResponseSerialization.encodeLoadResponse
            ApiResponseSerialization.decodeLoadResponseDecoder
            response
    Assert.Equal(response.revision, decoded.revision)
    Assert.Equal(response.buildEpochSec, decoded.buildEpochSec)
    Assert.Equal(response.pageBuildEpochSec, decoded.pageBuildEpochSec)
    Assert.False(decoded.isReady)
    Assert.Equal(1, decoded.changes.Length)
    Assert.Equal(1, decoded.packages.Length)
    Assert.Equal(node.id, decoded.packages.[0].id)

[<Fact>]
let ``LoadResponse decoder tolerates missing packages`` () =
    let json = """{"r":4,"b":100,"p":200,"ready":true,"c":[]}"""
    match Dec.fromString ApiResponseSerialization.decodeLoadResponseDecoder json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok (decoded: LoadResponse) ->
        Assert.Equal(4, decoded.revision)
        Assert.Empty(decoded.packages)

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
