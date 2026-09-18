module Gambol.Shared.Tests.SerializationTests

open Xunit
open Gambol.Shared
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
let ``Node JSON omits lock-present`` () =
    let node = Node.Create(NodeId.New(), text = "locked", lockPresent = true)
    let json = Enc.toString 0 (Serialization.encodeNode node)
    Assert.DoesNotContain("lockPresent", json)
    Assert.DoesNotContain("lock-present", json)
    let decoded = roundTrip Serialization.encodeNode Serialization.decodeNode node
    Assert.False(decoded.lockPresent)

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

let private alanSpecialDirectoryJson =
    "{"
    + "\"id\":\"fde5ee56-f6d5-44e0-bff5-75c19924afa4\","
    + "\"text\":\"Example\",\"name\":\"Example\","
    + "\"children\":[{\"ref\":\"ref\","
    + "\"id\":\"f456d9ef-7bab-4fd6-b2fa-af19724c6141\"}],"
    + "\"childrenStatus\":\"loaded\",\"cssClasses\":[],"
    + "\"kind\":{\"type\":\"special\",\"kind\":\"directory\"},"
    + "\"documentState\":\"current\","
    + "\"updateTime\":\"639204537026026480\"}"

let private alanNormalNullNameJson =
    "{"
    + "\"id\":\"ffab5839-cc99-4036-a967-0ae70a779969\","
    + "\"text\":\"            HttpMethods.IsPost context.Request.Method\","
    + "\"name\":null,\"children\":[],"
    + "\"childrenStatus\":\"loaded\",\"cssClasses\":[],"
    + "\"kind\":\"normal\",\"documentState\":\"current\","
    + "\"updateTime\":\"0\"}"

let private decodeNodeOrFail json =
    match Dec.fromString Serialization.decodeNode json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok node -> node

[<Fact>]
let ``Node decode accepts mixed kind null name and string updateTime`` () =
    let special = decodeNodeOrFail alanSpecialDirectoryJson
    Assert.Equal(
        System.Guid.Parse "fde5ee56-f6d5-44e0-bff5-75c19924afa4",
        special.id.Value)
    Assert.Equal(Special Directory, special.kind)
    Assert.Equal(Filename.create "Example", special.name)
    Assert.Equal(1, special.children.Length)
    Assert.Equal(Ownership.Ref, special.children.Head.ref)
    Assert.Equal(
        System.DateTime(639204537026026480L, System.DateTimeKind.Utc),
        special.updateTime)

    let normal = decodeNodeOrFail alanNormalNullNameJson
    Assert.Equal(Normal, normal.kind)
    Assert.Equal(Filename.Empty, normal.name)
    Assert.Equal(NodeUpdateTime.missing, normal.updateTime)

[<Fact>]
let ``StateResponse decode accepts Alan sample node shapes`` () =
    let rootId = Graph.rootId.Value.ToString()
    let rootJson =
        "{"
        + $"\"id\":\"{rootId}\",\"text\":\"ROOT\",\"name\":null,"
        + "\"children\":[],\"childrenStatus\":\"loaded\","
        + "\"cssClasses\":[],"
        + "\"kind\":{\"type\":\"special\",\"kind\":\"workspace\"},"
        + "\"documentState\":\"current\",\"updateTime\":\"0\"}"
    let json =
        "{\"eventId\":1,\"ready\":true,\"graph\":{"
        + $"\"root\":\"{rootId}\",\"nodes\":["
        + rootJson
        + ","
        + alanSpecialDirectoryJson
        + ","
        + alanNormalNullNameJson
        + "]}}"
    match Dec.fromString ApiResponseSerialization.decodeStateResponseDecoder json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok response ->
        Assert.Equal(EventId.fromJson 1, response.eventId)
        let specialId =
            NodeId(System.Guid.Parse "fde5ee56-f6d5-44e0-bff5-75c19924afa4")
        let normalId =
            NodeId(System.Guid.Parse "ffab5839-cc99-4036-a967-0ae70a779969")
        Assert.True(Map.containsKey specialId response.graph.nodes)
        Assert.True(Map.containsKey normalId response.graph.nodes)

[<Fact>]
let ``Graph decode missing root is an Error not a throw`` () =
    let rootId = Graph.rootId.Value.ToString()
    let json =
        "{\"root\":\""
        + rootId
        + "\",\"nodes\":["
        + alanSpecialDirectoryJson
        + ","
        + alanNormalNullNameJson
        + "]}"
    let result =
        try
            Dec.fromString Serialization.decodeGraph json
        with ex ->
            failwith $"decode threw: {ex.Message}"
    match result with
    | Ok _ -> failwith "expected decode failure"
    | Error err -> Assert.Contains("missing canonical root", err)

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
    let parentId = NodeId.New()
    let oldChildren = [ ChildNode.New() ]
    let newChildren = [ ChildNode.New(); ChildNode.New() ]
    let op = Op.Replace(parentId, oldChildren, newChildren)
    let json = Enc.toString 0 (Serialization.encodeOp op)
    Assert.DoesNotContain("\"index\"", json)
    let decoded = roundTrip Serialization.encodeOp Serialization.decodeOp op
    Assert.Equal(op, decoded)

[<Fact>]
let ``Op.Replace round-trip preserves child ownership`` () =
    let shared = NodeId.New()
    let op =
        Op.Replace(
            NodeId.New(),
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
let ``EventBatch round-trip`` () =
    let change =
        { id = EventId.fromJson 5
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(NodeId.New(), "old", "new") ] }
    let event: Ev =
        { id = EventId.zero
          submissionId = change.submissionId
          authority = Gambol.Shared.Authority ""
          commandName = ""
          body = change.body }
    let batch = { events = [ event ] }
    let decoded = roundTrip EventJson.encodeEventBatch EventJson.decodeEventBatch batch
    Assert.Equal<Ev list>(batch.events, decoded.events)

[<Fact>]
let ``EventBatch round-trip preserves request order`` () =
    let first =
        { id = EventId.fromJson 5
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(NodeId.New(), "x", "y") ] }
    let firstEvent: Ev =
        { id = EventId.zero
          submissionId = first.submissionId
          authority = Gambol.Shared.Authority ""
          commandName = ""
          body = first.body }
    let second =
        { id = EventId.fromJson 6
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(NodeId.New(), "a", "b") ] }
    let secondEvent: Ev =
        { id = EventId.zero
          submissionId = second.submissionId
          authority = Gambol.Shared.Authority ""
          commandName = ""
          body = second.body }
    let batch = { events = [ firstEvent; secondEvent ] }
    let json = Enc.toString 0 (EventJson.encodeEventBatch batch)
    Assert.DoesNotContain("\"action\":\"undo\"", json)
    Assert.DoesNotContain("\"action\":\"redo\"", json)
    let decoded =
        roundTrip EventJson.encodeEventBatch EventJson.decodeEventBatch batch
    Assert.Equal<Ev list>([ firstEvent; secondEvent ], decoded.events)

[<Fact>]
let ``EventBatch decoder rejects empty events`` () =
    let json = """{"events":[]}"""
    match Dec.fromString EventJson.decodeEventBatch json with
    | Ok _ -> failwith "Expected empty batch to fail decoding"
    | Error _ -> ()

[<Fact>]
let ``EventBatch decoder rejects explicit Undo JSON`` () =
    let json =
        """{"events":[{"id":0,"submissionId":"00000000-0000-0000-0000-000000000001","authority":"","commandName":"","body":{"Undo":{"Item1":0,"Item2":[]}}}]}"""
    match Dec.fromString EventJson.decodeEventBatch json with
    | Ok _ -> failwith "Expected explicit Undo JSON to fail decoding"
    | Error _ -> ()

[<Fact>]
let ``EventBatch decoder rejects explicit Redo JSON`` () =
    let json =
        """{"events":[{"id":0,"submissionId":"00000000-0000-0000-0000-000000000001","authority":"","commandName":"","body":{"Redo":{"Item1":0,"Item2":[]}}}]}"""
    match Dec.fromString EventJson.decodeEventBatch json with
    | Ok _ -> failwith "Expected explicit Redo JSON to fail decoding"
    | Error _ -> ()

[<Fact>]
let ``ChangeSuccessResponse round-trip with non-empty Changes`` () =
    let change =
        { id = EventId.fromJson 3
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(NodeId.New(), "old", "new") ] }
    let response: ChangeSuccessResponse =
        { eventId = EventId.fromJson 7
          buildEpochSec = 100
          pageBuildEpochSec = 200
          apiVersion = ApiVersion.current
          isReady = false
          externalChanges = true
          events = [ change ]
          message = Some "stable file update failed"
          bootstrapHash = None }
    let decoded =
        roundTrip
            ApiResponseSerialization.encodeChangeSuccessResponse
            ApiResponseSerialization.decodeChangeSuccessResponseDecoder
            response
    Assert.Equal(response.eventId, decoded.eventId)
    Assert.Equal(response.buildEpochSec, decoded.buildEpochSec)
    Assert.Equal(response.pageBuildEpochSec, decoded.pageBuildEpochSec)
    Assert.Equal(response.apiVersion, decoded.apiVersion)
    Assert.False(decoded.isReady)
    Assert.True(decoded.externalChanges)
    Assert.Equal(1, decoded.events.Length)
    Assert.Equal(change.id, decoded.events.[0].id)
    Assert.Equal<Op list>(
        SpecialNodeTestHelpers.eventOps change,
        Ev.ops decoded.events.[0] |> Option.defaultValue [])
    Assert.Equal(response.message, decoded.message)

[<Fact>]
let ``ChangeSuccessResponse round-trip with empty Changes`` () =
    let response: ChangeSuccessResponse =
        { eventId = EventId.fromJson 5
          buildEpochSec = 0
          pageBuildEpochSec = 0
          apiVersion = ApiVersion.current
          isReady = true
          externalChanges = false
          events = []
          message = None
          bootstrapHash = None }
    let decoded =
        roundTrip
            ApiResponseSerialization.encodeChangeSuccessResponse
            ApiResponseSerialization.decodeChangeSuccessResponseDecoder
            response
    Assert.Equal(response.eventId, decoded.eventId)
    Assert.False(decoded.externalChanges)
    Assert.Equal(response.apiVersion, decoded.apiVersion)
    Assert.Equal<Ev list>([], decoded.events)
    Assert.Equal(None, decoded.message)
    Assert.Equal(None, decoded.bootstrapHash)

[<Fact>]
let ``ChangeSuccessResponse omits bootstrapHash and still decodes`` () =
    let json = """{"r":3,"b":0,"p":0,"ready":true,"externalChanges":false,"c":[]}"""
    match Dec.fromString ApiResponseSerialization.decodeChangeSuccessResponseDecoder json with
    | Error err -> failwith err
    | Ok decoded ->
        Assert.Equal(3, decoded.eventId.Value)
        Assert.Equal(0, decoded.apiVersion)
        Assert.Equal(None, decoded.bootstrapHash)

[<Fact>]
let ``ChangeSuccessResponse round-trip with bootstrapHash`` () =
    let response: ChangeSuccessResponse =
        { eventId = EventId.fromJson 3
          buildEpochSec = 0
          pageBuildEpochSec = 0
          apiVersion = ApiVersion.current
          isReady = true
          externalChanges = false
          events = []
          message = None
          bootstrapHash = Some "deadbeef" }
    let decoded =
        roundTrip
            ApiResponseSerialization.encodeChangeSuccessResponse
            ApiResponseSerialization.decodeChangeSuccessResponseDecoder
            response
    Assert.Equal(Some "deadbeef", decoded.bootstrapHash)

[<Fact>]
let ``LoadRequest round-trip`` () =
    let request: LoadRequest =
        { eventId = EventId.fromJson 11
          targets =
            [ { targetId = NodeId.New(); includeWorkspace = true }
              { targetId = NodeId.New(); includeWorkspace = false } ] }
    let decoded =
        roundTrip
            ApiResponseSerialization.encodeLoadRequest
            ApiResponseSerialization.decodeLoadRequestDecoder
            request
    Assert.Equal(request.eventId, decoded.eventId)
    Assert.Equal(2, decoded.targets.Length)
    Assert.Equal(request.targets.[0].targetId, decoded.targets.[0].targetId)
    Assert.True(decoded.targets.[0].includeWorkspace)
    Assert.False(decoded.targets.[1].includeWorkspace)

[<Fact>]
let ``LoadResponse round-trip with packages`` () =
    let node =
        Node.Create(NodeId.New(), text = "ws child", owner = Graph.rootId)
    let change =
        { id = EventId.fromJson 2
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(node.id, "a", "b") ] }
    let response: LoadResponse =
        { eventId = EventId.fromJson 8
          buildEpochSec = 10
          pageBuildEpochSec = 20
          apiVersion = ApiVersion.current
          isReady = false
          events = [ change ]
          packages = [ node ] }
    let decoded =
        roundTrip
            ApiResponseSerialization.encodeLoadResponse
            ApiResponseSerialization.decodeLoadResponseDecoder
            response
    Assert.Equal(response.eventId, decoded.eventId)
    Assert.Equal(response.buildEpochSec, decoded.buildEpochSec)
    Assert.Equal(response.pageBuildEpochSec, decoded.pageBuildEpochSec)
    Assert.Equal(response.apiVersion, decoded.apiVersion)
    Assert.False(decoded.isReady)
    Assert.Equal(1, decoded.events.Length)
    Assert.Equal(1, decoded.packages.Length)
    Assert.Equal(node.id, decoded.packages.[0].id)

[<Fact>]
let ``LoadResponse decoder tolerates missing packages`` () =
    let json = """{"r":4,"b":100,"p":200,"ready":true,"c":[]}"""
    match Dec.fromString ApiResponseSerialization.decodeLoadResponseDecoder json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok (decoded: LoadResponse) ->
        Assert.Equal(EventId.fromJson 4, decoded.eventId)
        Assert.Empty(decoded.packages)

[<Fact>]
let ``StateResponse round-trip preserves startup readiness`` () =
    let response =
        { graph = Graph.create ()
          eventId = EventId.fromJson 3
          isReady = false }
        : StateResponse
    let decoded =
        roundTrip
            ApiResponseSerialization.encodeStateResponse
            ApiResponseSerialization.decodeStateResponseDecoder
            response

    Assert.Equal(response.eventId, decoded.eventId)
    Assert.False(decoded.isReady)
    Assert.True(GraphProjection.graphEquals response.graph decoded.graph)
