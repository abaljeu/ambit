module Gambol.Shared.Tests.GraphFocusTests

open Xunit
open Gambol.Shared

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

[<Fact>]
let ``create and fromExtracted default focus to None`` () =
    let created = Graph.create ()
    Assert.Equal(None, created.focus)
    let node = Node.Create(NodeId.New(), text = "solo")
    let extracted =
        Graph.fromExtracted node.id (Map.ofList [ node.id, node ])
    Assert.Equal(None, extracted.focus)

[<Fact>]
let ``withFocus sets Graph.focus and record update keeps it`` () =
    let graph0 = Graph.create ()
    let focused = Graph.withFocus (Some Graph.rootId) graph0
    Assert.Equal(Some Graph.rootId, focused.focus)
    let kept = { focused with nodes = focused.nodes }
    Assert.Equal(Some Graph.rootId, kept.focus)
    let cleared = Graph.withFocus None focused
    Assert.Equal(None, cleared.focus)

[<Fact>]
let ``encodeGraph omits focus and decode leaves it None`` () =
    let focused = Graph.withFocus (Some Graph.rootId) (Graph.create ())
    let json = Enc.toString 0 (Serialization.encodeGraph focused)
    Assert.DoesNotContain("\"focus\"", json)
    match Dec.fromString Serialization.decodeGraph json with
    | Error err -> failwith $"decode: {err}"
    | Ok decoded ->
        Assert.Equal(None, decoded.focus)
        Assert.Equal(focused.root, decoded.root)

[<Fact>]
let ``History projection restore drops focus`` () =
    let focused = Graph.withFocus (Some Graph.rootId) (Graph.create ())
    let restored =
        GraphProjection.graphRoundTrip focused |> requireOk "projection"
    Assert.Equal(None, restored.focus)
    Assert.Equal(focused.root, restored.root)
