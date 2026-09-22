module Gambol.CloudAgents.Tests.CursorHttpJsonTests

open Xunit
open FSharp.Data
open Gambol.CloudAgents.Internal

let private sampleRequest model =
    { CursorTypes.CursorCreateRequest.prompt = { text = "hi" }
      CursorTypes.CursorCreateRequest.name = None
      CursorTypes.CursorCreateRequest.model = model
      CursorTypes.CursorCreateRequest.repos = None }

[<Fact>]
let ``create JSON encodes model as an object with id`` () =
    let json =
        CursorHttp.createRequestJson (sampleRequest (Some "grok"))
    match json.["model"] with
    | JsonValue.String _ -> failwith "model must be an object"
    | JsonValue.Record _ -> ()
    | other -> failwith (other.ToString())
    Assert.Equal("grok", json.["model"].["id"].AsString())
    Assert.True(json.["model"].TryGetProperty("params").IsNone)

[<Fact>]
let ``create JSON omits model when none`` () =
    let json = CursorHttp.createRequestJson (sampleRequest None)
    Assert.True(json.TryGetProperty("model").IsNone)
    Assert.Equal("hi", json.["prompt"].["text"].AsString())

[<Fact>]
let ``parseModelCatalog reads items id displayName variants`` () =
    let body =
        """{"items":[{"id":"x","displayName":"X","description":"desc","aliases":["x1"],"parameters":{"k":1},"variants":[{"id":"x-fast","displayName":"Fast"}]}]}"""
    match CursorHttp.parseModelCatalog body with
    | Error msg -> failwith msg
    | Ok models ->
        Assert.Equal(1, models.Length)
        let m = models.[0]
        Assert.Equal("x", m.id)
        Assert.Equal("X", m.displayName)
        Assert.Equal(Some "desc", m.description)
        Assert.Equal<string list>([ "x1" ], m.aliases)
        Assert.True(m.parameters.IsSome)
        Assert.Equal(1, m.variants.Length)
        Assert.Equal("x-fast", m.variants.[0].id)
        Assert.Equal(Some "Fast", m.variants.[0].displayName)

[<Fact>]
let ``parseModelCatalog accepts models envelope and display_name`` () =
    let body =
        """{"models":[{"id":"y","display_name":"Why"}]}"""
    match CursorHttp.parseModelCatalog body with
    | Error msg -> failwith msg
    | Ok models ->
        Assert.Equal("y", models.[0].id)
        Assert.Equal("Why", models.[0].displayName)

[<Fact>]
let ``parseModelCatalog rejects invalid json`` () =
    match CursorHttp.parseModelCatalog "not-json" with
    | Error _ -> Assert.True(true)
    | Ok _ -> failwith "expected parse error"
