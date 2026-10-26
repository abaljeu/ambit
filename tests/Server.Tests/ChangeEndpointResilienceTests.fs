module Gambol.Server.Tests.ChangeEndpointResilienceTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Encode = Thoth.Json.Newtonsoft.Encode

let private timeout (pending: Task<'T>) : Task<'T> =
    pending.WaitAsync(TimeSpan.FromSeconds(2.0))

/// SYSTEM always exists; user.css is optional — create it when this scenario needs it.
let private createSystemCssClient () =
    let dataDir = newTempDir ()
    let systemDir = Path.Combine(dataDir, "SYSTEM")
    Directory.CreateDirectory(systemDir) |> ignore
    File.WriteAllText(Path.Combine(dataDir, ".amb"), "^SYSTEM SYSTEM\tSystem")
    File.WriteAllText(
        Path.Combine(systemDir, ".amb"),
        "^6556583f-322d-4183-bc42-284a81044a0f user.css\tuser.css")
    File.WriteAllText(Path.Combine(systemDir, "user.css"), "block")
    let connStr = requireDbConnStr ()
    resetTestDatabase connStr |> fun t -> t.GetAwaiter().GetResult()
    match DocumentLoader.tryLoadState dataDir with
    | Error msg -> failwith msg
    | Ok state ->
        Database.rebuildFromDocumentFiles connStr state
        |> fun t -> t.GetAwaiter().GetResult()
    dataDir, createDbClientForDir connStr dataDir

let private exactRawBatch =
    """
    [
      {
        "id": 1130,
        "submissionId": "93a26b25-272f-4c48-916b-4045a2ba37a1",
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
    let wrapped = """{"events":""" + exactRawBatch + "}"
    use wrappedContent = new StringContent(wrapped, Encoding.UTF8, "application/json")
    use! wrappedResponse =
        client.PostAsync("/ambit/changes", wrappedContent) |> timeout
    Assert.Equal(HttpStatusCode.BadRequest, wrappedResponse.StatusCode)
    use! stateResponse = client.GetAsync("/ambit/state") |> timeout
    Assert.Equal(HttpStatusCode.OK, stateResponse.StatusCode)
}

[<Fact>]
let ``changes POST rejects non-zero EventId and admits zero`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDir dataDir
    let _, zeroEvent = addRootChildEvent "zero-ok"
    let zeroBody =
        Encode.toString 0 (
            ApiResponseSerialization.encodeChangeRequest
                { events = [ zeroEvent ]; want = [] })
    use zeroContent =
        new StringContent(zeroBody, Encoding.UTF8, "application/json")
    use! zeroResponse =
        client.PostAsync("/ambit/changes", zeroContent) |> timeout
    Assert.Equal(HttpStatusCode.OK, zeroResponse.StatusCode)
    let _, dirtyEvent = addRootChildEvent "nonzero"
    let dirty = { dirtyEvent with id = EventIdFixtures.storedId 4 }
    let dirtyBody =
        Encode.toString 0 (
            ApiResponseSerialization.encodeChangeRequest
                { events = [ dirty ]; want = [] })
    use dirtyContent =
        new StringContent(dirtyBody, Encoding.UTF8, "application/json")
    use! dirtyResponse =
        client.PostAsync("/ambit/changes", dirtyContent) |> timeout
    Assert.Equal(HttpStatusCode.BadRequest, dirtyResponse.StatusCode)
    let! dirtyJson = dirtyResponse.Content.ReadAsStringAsync() |> timeout
    Assert.Contains("posted EventId must be zero", dirtyJson)
}
