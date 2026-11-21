module Gambol.Server.Tests.ApiPostCommandTests

open System
open System.Net
open System.Net.Http
open System.Text
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend
open Thoth.Json.Newtonsoft

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private encodeRequest (request: ActorStart) =
    Encode.toString 0 (EventJson.encodeStartRequest request)

let private sampleRequest runDefinitionId : ActorStart =
    { zoomId = runDefinitionId
      focusId = runDefinitionId
      runDefinitionId = runDefinitionId
      graphIds = [ runDefinitionId ]
      eventId = EventId.zero }

let private jsonContent body =
    new StringContent(body, Encoding.UTF8, "application/json")

[<Fact>]
let ``POST command is absent`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDir dataDir
    let request = sampleRequest (NodeId.New())
    use body = jsonContent (encodeRequest request)
    let! response = client.PostAsync("/ambit/command", body)
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode)
}

let private launchEvent (request: ActorStart) : Ev =
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = "Exec"
      body = EventBody.ActorStart request }

let private decodeStateGraph json =
    let decoder =
        Thoth.Json.Core.Decode.object (fun get ->
            let eventId =
                get.Required.Field "eventId" EventJson.decodeEventId
            let graph =
                get.Required.Field "graph" Serialization.decodeGraph
            eventId, graph)
    match Decode.fromString decoder json with
    | Ok pair -> pair
    | Error err -> failwith err

let private jsonPost body =
    new StringContent(body, Encoding.UTF8, "application/json")

let private getFullState (client: HttpClient) = task {
    let! resp = client.GetAsync("/ambit/state?scope=full")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    let! json = resp.Content.ReadAsStringAsync()
    return decodeStateGraph json
}

let private postEventsHttp (client: HttpClient) (events: Ev list) = task {
    let body =
        Encode.toString 0 (
            ApiResponseSerialization.encodeChangeRequest
                { events = events; want = [] })
    use content = jsonPost body
    return! client.PostAsync("/ambit/changes", content)
}

let private seedQuestionTest (client: HttpClient) rootId runDefinitionId = task {
    let seed =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body =
            EventBody.Change
                [ Op.NewNode(runDefinitionId, "?test hello")
                  Op.Replace(
                      rootId,
                      [],
                      [ ChildNode.owner runDefinitionId ]) ] }
    let! seedResp = postEventsHttp client [ seed ]
    Assert.Equal(HttpStatusCode.OK, seedResp.StatusCode)
}

let private waitStateHello
    (client: HttpClient)
    (focusId: NodeId)
    timeoutMs
    =
    task {
        let mutable found = false
        let startTime = DateTime.UtcNow
        while not found
              && (DateTime.UtcNow - startTime).TotalMilliseconds
                 < float timeoutMs do
            let! resp = client.GetAsync("/ambit/state?scope=full")
            let! json = resp.Content.ReadAsStringAsync()
            if resp.StatusCode = HttpStatusCode.OK then
                let _, graph = decodeStateGraph json
                found <-
                    match Map.tryFind focusId graph.nodes with
                    | None -> false
                    | Some node ->
                        Graph.children graph focusId
                        |> List.exists (fun child ->
                            child.ref = Ownership.Owner
                            && match Map.tryFind child.id graph.nodes with
                               | Some n -> n.text = "hello"
                               | None -> false)
            if not found then do! Task.Delay 20
        return found
    }

[<Fact>]
let ``production boot Run of ?test hello posts Owned hello child`` () =
    task {
        use client = createClientForDir (newTempDir ())
        let! _, graph = getFullState client
        let runDefinitionId = NodeId.New()
        do! seedQuestionTest client graph.root runDefinitionId
        let! afterId, _ = getFullState client
        let request: ActorStart =
            { zoomId = runDefinitionId
              focusId = runDefinitionId
              runDefinitionId = runDefinitionId
              graphIds = [ runDefinitionId ]
              eventId = afterId }
        let launch = launchEvent request
        let! commandResp = postEventsHttp client [ launch ]
        let! commandJson = commandResp.Content.ReadAsStringAsync()
        Assert.True(
            commandResp.StatusCode = HttpStatusCode.OK,
            $"events {commandResp.StatusCode}: {commandJson}")
        let! hello = waitStateHello client runDefinitionId 2000
        Assert.True(
            hello,
            "production Actor test did not post hello under Focus")
    }
