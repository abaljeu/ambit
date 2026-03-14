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
let ``Node round-trip with Some name`` () =
    let node =
        { id = NodeId.New()
          text = "hello world"
          name = Some "myname"
          children = [ NodeId.New(); NodeId.New() ]
          cssClasses = CssClass.empty }
    let decoded = roundTrip Serialization.encodeNode Serialization.decodeNode node
    Assert.Equal(node, decoded)

[<Fact>]
let ``Node round-trip with None name`` () =
    let node =
        { id = NodeId.New()
          text = "hello"
          name = None
          children = []
          cssClasses = CssClass.empty }
    let decoded = roundTrip Serialization.encodeNode Serialization.decodeNode node
    Assert.Equal(node, decoded)

[<Fact>]
let ``Graph round-trip`` () =
    let graph = ModelBuilder.createDag12 ()
    let decoded = roundTrip Serialization.encodeGraph Serialization.decodeGraph graph
    Assert.Equal(graph.root, decoded.root)
    Assert.Equal<Map<NodeId, Node>>(graph.nodes, decoded.nodes)

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
    let op = Op.Replace(NodeId.New(), 2, [ NodeId.New() ], [ NodeId.New(); NodeId.New() ])
    let decoded = roundTrip Serialization.encodeOp Serialization.decodeOp op
    Assert.Equal(op, decoded)

[<Fact>]
let ``Change round-trip`` () =
    let change =
        { id = 5
          ops =
            [ Op.NewNode(NodeId.New(), "hello")
              Op.SetText(NodeId.New(), "old", "new")
              Op.Replace(NodeId.New(), 0, [], [ NodeId.New() ]) ] }
    let decoded = roundTrip Serialization.encodeChange Serialization.decodeChange change
    Assert.Equal(change.id, decoded.id)
    Assert.Equal<Op list>(change.ops, decoded.ops)
