module Gambol.Server.Tests.ChangeEndpointResilienceTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Threading.Tasks
open Xunit
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private timeout (pending: Task<'T>) : Task<'T> =
    pending.WaitAsync(TimeSpan.FromSeconds(2.0))

let private decodeGraph json =
    let decoder =
        Thoth.Json.Core.Decode.object (fun get ->
            get.Required.Field "graph" Serialization.decodeGraph)

    match Decode.fromString decoder json with
    | Ok graph -> graph
    | Error err -> failwith err

let private createSystemCssClient () =
    let dataDir = newTempDir ()
    let systemDir = Path.Combine(dataDir, "SYSTEM")
    Directory.CreateDirectory(systemDir) |> ignore
    File.WriteAllText(Path.Combine(dataDir, ".amb"), "^SYSTEM SYSTEM\tSystem")
    File.WriteAllText(
        Path.Combine(systemDir, ".amb"),
        "^6556583f-322d-4183-bc42-284a81044a0f user.css\tuser.css")
    File.WriteAllText(Path.Combine(systemDir, "user.css"), "block")
    dataDir, createClientForDir dataDir

let private exactRawBatch =
    """
    [
      {
        "id": 1130,
        "changeId": "93a26b25-272f-4c48-916b-4045a2ba37a1",
        "ops": [
          {
            "type": "SetText",
            "nodeId": "2e487e15-e35d-43cd-9c31-cd90b4c174d9",
            "oldText": "",
            "newText": "\"background\" : #fff"
          }
        ]
      }
    ]
    """

[<Fact>]
let ``exact raw change array is rejected and server remains responsive`` () = task {
    let _, client = createSystemCssClient ()
    use client = client
    use content = new StringContent(exactRawBatch, Encoding.UTF8, "application/json")
    use! response = client.PostAsync("/ambit/changes", content) |> timeout
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode)
    let wrapped = """{"changes":""" + exactRawBatch + "}"
    use wrappedContent = new StringContent(wrapped, Encoding.UTF8, "application/json")
    use! wrappedResponse =
        client.PostAsync("/ambit/changes", wrappedContent) |> timeout
    Assert.Equal(HttpStatusCode.BadRequest, wrappedResponse.StatusCode)
    use! stateResponse = client.GetAsync("/ambit/state") |> timeout
    Assert.Equal(HttpStatusCode.OK, stateResponse.StatusCode)
}

[<Fact>]
let ``SetText persists SYSTEM user css and server remains responsive`` () = task {
    let dataDir, client = createSystemCssClient ()
    use client = client
    use! initialResponse = client.GetAsync("/ambit/state") |> timeout
    let! initialJson = initialResponse.Content.ReadAsStringAsync() |> timeout
    let graph = decodeGraph initialJson
    let fileId = graph.nodes.[Graph.systemId].children |> List.exactlyOne |> fun c -> c.id
    let cssNodeId = graph.nodes.[fileId].children |> List.exactlyOne |> fun c -> c.id
    let change =
        { id = 0
          changeId = Guid.Parse("93a26b25-272f-4c48-916b-4045a2ba37a1")
          ops = [ Op.SetText(cssNodeId, "block", "\"background\" : #fff") ] }
    let body =
        Encode.toString 0 (
            Serialization.encodeChangeBatch
                { changes = [ ChangeRequest.Change change ] })
    use content = new StringContent(body, Encoding.UTF8, "application/json")
    use! response = client.PostAsync("/ambit/changes", content) |> timeout
    Assert.Equal(HttpStatusCode.OK, response.StatusCode)
    Assert.Equal(
        "\"background\" : #fff" + Environment.NewLine,
        File.ReadAllText(Path.Combine(dataDir, "SYSTEM", "user.css")))
    use! stateResponse = client.GetAsync("/ambit/state") |> timeout
    Assert.Equal(HttpStatusCode.OK, stateResponse.StatusCode)
}
