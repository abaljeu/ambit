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

let private decodeRevisionAndGraph json =
    let decoder =
        Thoth.Json.Core.Decode.object (fun get ->
            let revision =
                get.Required.Field "revision" Serialization.decodeRevision
            let graph =
                get.Required.Field "graph" Serialization.decodeGraph
            revision.Value, graph)

    match Decode.fromString decoder json with
    | Ok pair -> pair
    | Error err -> failwith err

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
    dataDir, createClientForDir dataDir

let private findOwnedChildNamed (graph: Graph) (parentId: NodeId) (name: string) =
    graph.nodes.[parentId].children
    |> List.choose (fun c ->
        if c.ref <> Ownership.Owner then
            None
        else
            match Map.tryFind c.id graph.nodes with
            | Some node when Filename.tryValue node.name = Some name -> Some c.id
            | _ -> None)
    |> List.exactlyOne

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
    use! initialResponse = client.GetAsync("/ambit/state?scope=full") |> timeout
    let! initialJson = initialResponse.Content.ReadAsStringAsync() |> timeout
    let graph0 = decodeGraph initialJson
    let fileId = findOwnedChildNamed graph0 Graph.systemId "user.css"
    // Cold bootstrap is Directory File outline only; File bodies load on Parse.
    let parseBody = sprintf """{"fileId":"%O"}""" fileId.Value
    use parseContent = new StringContent(parseBody, Encoding.UTF8, "application/json")
    use! parseResponse = client.PostAsync("/ambit/file/parse", parseContent) |> timeout
    Assert.Equal(HttpStatusCode.OK, parseResponse.StatusCode)
    use! loadedResponse = client.GetAsync("/ambit/state?scope=full") |> timeout
    let! loadedJson = loadedResponse.Content.ReadAsStringAsync() |> timeout
    let revision, graph = decodeRevisionAndGraph loadedJson
    let cssNodeId = graph.nodes.[fileId].children |> List.exactlyOne |> fun c -> c.id
    let change =
        { id = revision
          changeId = Guid.Parse("93a26b25-272f-4c48-916b-4045a2ba37a1")
          ops = [ Op.SetText(cssNodeId, "block", "\"background\" : #fff") ] }
    let body =
        Encode.toString 0 (
            Serialization.encodeChangeBatch
                { changes = [ change ] })
    use content = new StringContent(body, Encoding.UTF8, "application/json")
    use! response = client.PostAsync("/ambit/changes", content) |> timeout
    Assert.Equal(HttpStatusCode.OK, response.StatusCode)
    Assert.Equal(
        "\"background\" : #fff" + Environment.NewLine,
        File.ReadAllText(Path.Combine(dataDir, "SYSTEM", "user.css")))
    use! stateResponse = client.GetAsync("/ambit/state") |> timeout
    Assert.Equal(HttpStatusCode.OK, stateResponse.StatusCode)
}
