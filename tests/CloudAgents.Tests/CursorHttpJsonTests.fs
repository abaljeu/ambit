module Gambol.CloudAgents.Tests.CursorHttpJsonTests

open Xunit
open FSharp.Data
open Gambol.CloudAgents.Internal

let private sampleRequest model =
    { CursorTypes.CursorCreateRequest.prompt = { text = "hi" }
      CursorTypes.CursorCreateRequest.name = None
      CursorTypes.CursorCreateRequest.model = model
      CursorTypes.CursorCreateRequest.repos = None }

let private modelRef id parameters =
    Some
        { CursorTypes.CursorModelRef.id = id
          CursorTypes.CursorModelRef.``params`` = parameters }

[<Fact>]
let ``create JSON encodes model as an object with id`` () =
    let json =
        CursorHttp.createRequestJson
            (sampleRequest (modelRef "grok" []))
    match json.["model"] with
    | JsonValue.String _ -> failwith "model must be an object"
    | JsonValue.Record _ -> ()
    | other -> failwith (other.ToString())
    Assert.Equal("grok", json.["model"].["id"].AsString())
    Assert.True(json.["model"].TryGetProperty("params").IsNone)

[<Fact>]
let ``create JSON includes params when non-empty`` () =
    let parameters =
        [ { CursorTypes.CursorParamAssignment.id = "context"
            CursorTypes.CursorParamAssignment.value = "256k" }
          { CursorTypes.CursorParamAssignment.id =
              "reasoning_effort"
            CursorTypes.CursorParamAssignment.value = "high" } ]
    let json =
        CursorHttp.createRequestJson
            (sampleRequest (modelRef "grok-4.7" parameters))
    let ps = json.["model"].["params"].AsArray()
    Assert.Equal(2, ps.Length)
    Assert.Equal("context", ps.[0].["id"].AsString())
    Assert.Equal("256k", ps.[0].["value"].AsString())
    Assert.Equal("reasoning_effort", ps.[1].["id"].AsString())
    Assert.Equal("high", ps.[1].["value"].AsString())

[<Fact>]
let ``browser default grok create JSON sends model params`` () =
    let parameters =
        [ { CursorTypes.CursorParamAssignment.id = "context"
            CursorTypes.CursorParamAssignment.value = "256k" }
          { CursorTypes.CursorParamAssignment.id =
              "reasoning_effort"
            CursorTypes.CursorParamAssignment.value = "low" }
          { CursorTypes.CursorParamAssignment.id = "fast"
            CursorTypes.CursorParamAssignment.value = "true" } ]
    let json =
        CursorHttp.createRequestJson
            (sampleRequest (modelRef "grok-4.7" parameters))
    Assert.Equal("grok-4.7", json.["model"].["id"].AsString())
    let ps = json.["model"].["params"].AsArray()
    Assert.Equal(3, ps.Length)
    Assert.Equal("context", ps.[0].["id"].AsString())
    Assert.Equal("256k", ps.[0].["value"].AsString())
    Assert.Equal("reasoning_effort", ps.[1].["id"].AsString())
    Assert.Equal("low", ps.[1].["value"].AsString())
    Assert.Equal("fast", ps.[2].["id"].AsString())
    Assert.Equal("true", ps.[2].["value"].AsString())

[<Fact>]
let ``interpretSseDocument returns assistant text then result`` () =
    let seen = ref []
    let body =
        "event: assistant\n"
        + "data: {\"text\":\"ab\"}\n"
        + "\n"
        + "event: result\n"
        + "data: {\"result\":\"ab\"}\n"
        + "\n"
    match
        CursorHttp.interpretSseDocument
            body
            (fun text -> seen := text :: !seen)
    with
    | Error msg -> failwith msg
    | Ok parsed ->
        Assert.Equal("ab", parsed.text)
        Assert.Equal<string list>([ "ab" ], List.rev !seen)

[<Fact>]
let ``interpretSseDocument fails when the document has no terminal`` () =
    let body =
        "event: assistant\n"
        + "data: {\"text\":\"hi\"}\n"
        + "\n"
    match CursorHttp.interpretSseDocument body ignore with
    | Error "stream ended without terminal event" -> ()
    | other -> failwith $"{other}"

[<Fact>]
let ``interpretSseDocument stops on the first error event`` () =
    let body =
        "event: error\n"
        + "data: {\"message\":\"nope\"}\n"
        + "\n"
        + "event: result\n"
        + "data: {\"result\":\"later\"}\n"
        + "\n"
    match CursorHttp.interpretSseDocument body ignore with
    | Error "nope" -> ()
    | other -> failwith $"{other}"

[<Fact>]
let ``interpretSseDocument stops cancelled on a CANCELLED status`` () =
    let body =
        "event: assistant\n"
        + "data: {\"text\":\"hi\"}\n"
        + "\n"
        + "event: status\n"
        + "data: {\"status\":\"CANCELLED\"}\n"
        + "\n"
        + "event: done\n"
        + "data: {}\n"
        + "\n"
    match CursorHttp.interpretSseDocument body ignore with
    | Error "cancelled" -> ()
    | other -> failwith $"{other}"

[<Fact>]
let ``interpretSseDocument stops cancelled on a CANCELLED result`` () =
    let body =
        "event: result\n"
        + "data: {\"status\":\"CANCELLED\",\"result\":\"\"}\n"
        + "\n"
    match CursorHttp.interpretSseDocument body ignore with
    | Error "cancelled" -> ()
    | other -> failwith $"{other}"

[<Fact>]
let ``interpretSseDocument ignores a RUNNING status`` () =
    let body =
        "event: status\n"
        + "data: {\"status\":\"RUNNING\"}\n"
        + "\n"
        + "event: result\n"
        + "data: {\"status\":\"FINISHED\",\"result\":\"ok\"}\n"
        + "\n"
    match CursorHttp.interpretSseDocument body ignore with
    | Ok parsed -> Assert.Equal("ok", parsed.text)
    | Error msg -> failwith msg

[<Fact>]
let ``interpretSseDocument flushes a result that has no blank line`` () =
    let body = "event: result\ndata: {\"result\":\"z\"}"
    match CursorHttp.interpretSseDocument body ignore with
    | Ok parsed -> Assert.Equal("z", parsed.text)
    | Error msg -> failwith msg

[<Fact>]
let ``parseSseDocument reads event and data blocks`` () =
    let body =
        "event: assistant\n"
        + "data: {\"text\":\"hi\"}\n"
        + "\n"
        + "event: result\n"
        + "data: {\"result\":\"done\"}\n"
        + "\n"
    let messages = CursorHttp.parseSseDocument body
    Assert.Equal(2, messages.Length)
    Assert.Equal("assistant", messages.[0].eventType)
    Assert.Equal("{\"text\":\"hi\"}", messages.[0].data)
    Assert.Equal("result", messages.[1].eventType)

[<Fact>]
let ``create JSON omits model when none`` () =
    let json = CursorHttp.createRequestJson (sampleRequest None)
    Assert.True(json.TryGetProperty("model").IsNone)
    Assert.Equal("hi", json.["prompt"].["text"].AsString())

[<Fact>]
let ``parseModelCatalog reads structured parameters`` () =
    let body =
        """{"items":[{"id":"x","displayName":"X","description":"desc","aliases":["x1"],"parameters":[{"id":"context","displayName":"Context","values":[{"value":"256k","displayName":"256K"},{"value":"500k","displayName":"500K"}]}],"variants":[{"id":"x-fast","displayName":"Fast"}]}]}"""
    match CursorHttp.parseModelCatalog body with
    | Error msg -> failwith msg
    | Ok models ->
        Assert.Equal(1, models.Length)
        let m = models.[0]
        Assert.Equal("x", m.id)
        Assert.Equal("X", m.displayName)
        Assert.Equal(Some "desc", m.description)
        Assert.Equal<string list>([ "x1" ], m.aliases)
        Assert.Equal(1, m.parameters.Length)
        Assert.Equal("context", m.parameters.[0].id)
        Assert.Equal(Some "Context", m.parameters.[0].displayName)
        Assert.Equal(2, m.parameters.[0].values.Length)
        Assert.Equal("256k", m.parameters.[0].values.[0].value)
        Assert.Equal(
            Some "256K",
            m.parameters.[0].values.[0].displayName)
        Assert.Equal(1, m.variants.Length)
        Assert.Equal("x-fast", m.variants.[0].id)
        Assert.Equal(Some "Fast", m.variants.[0].displayName)
        Assert.Equal(0, m.variants.[0].``params``.Length)

[<Fact>]
let ``parseModelCatalog reads variant param bindings`` () =
    let body =
        """{"models":[{"id":"grok","variants":[{"id":"v1","displayName":"V1","params":[{"id":"context","value":"256k"}]}]}]}"""
    match CursorHttp.parseModelCatalog body with
    | Error msg -> failwith msg
    | Ok models ->
        let v = models.[0].variants.[0]
        Assert.Equal(1, v.``params``.Length)
        Assert.Equal("context", v.``params``.[0].id)
        Assert.Equal("256k", v.``params``.[0].value)

[<Fact>]
let ``parseModelCatalog ignores non-array parameters`` () =
    let body =
        """{"items":[{"id":"x","parameters":{"k":1}}]}"""
    match CursorHttp.parseModelCatalog body with
    | Error msg -> failwith msg
    | Ok models ->
        Assert.Equal(0, models.[0].parameters.Length)

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

[<Fact>]
let ``CursorModelsFile loads checked-in catalog`` () =
    match CursorModelsFile.loadFromFile () with
    | Error msg -> failwith msg
    | Ok models ->
        Assert.True(models.Length >= 1)
        Assert.True(
            models |> List.exists (fun m -> m.id = "default"))
