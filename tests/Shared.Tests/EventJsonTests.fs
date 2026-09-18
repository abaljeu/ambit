module EventJsonTests

open System
open Xunit
open Gambol.Shared
open Gambol.Shared

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

let private roundTrip (event: Ev) : Ev =
    let json = Enc.toString 0 (EventJson.encode event)
    match Dec.fromString EventJson.decode json with
    | Ok decoded -> decoded
    | Error err -> failwith $"Decode failed: {err}"

let private changeEvent : Ev =
    let nodeId = NodeId(Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"))
    { id = EventIdFixtures.storedId 3
      submissionId = Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
      authority = Authority "Browser"
      commandName = "Set text"
      body = EventBody.Change [ Op.SetText(nodeId, "old", "new") ] }

[<Fact>]
let ``Ev JSON uses eventId key`` () =
    let json = Enc.toString 0 (EventJson.encode changeEvent)
    Assert.Contains($"\"eventId\":{EventId.toJson changeEvent.id}", json)
    Assert.DoesNotContain("\"revision\"", json)

[<Fact>]
let ``Ev JSON round-trips a Change`` () =
    let decoded = roundTrip changeEvent
    Assert.Equal(changeEvent, decoded)

[<Fact>]
let ``Ev JSON round-trips name-only Undo`` () =
    let event =
        { changeEvent with
            id = EventIdFixtures.storedId 4
            commandName = "Undo"
            body = EventBody.Undo(EventIdFixtures.storedId 3, []) }
    Assert.Equal(event, roundTrip event)

[<Fact>]
let ``Ev JSON round-trips ActorStart and ActorStop`` () =
    let zoom = NodeId(Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"))
    let start =
        { zoomId = zoom
          focusId = NodeId(Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"))
          commandId = NodeId(Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"))
          graphIds = [ zoom ]
          eventId = EventIdFixtures.storedId 4 }
    let started =
        { changeEvent with
            commandName = ""
            body = EventBody.ActorStart start }
    let stopped =
        { changeEvent with
            commandName = ""
            body = EventBody.ActorStop(start.focusId, ActorSucceeded) }
    Assert.Equal(started, roundTrip started)
    Assert.Equal(stopped, roundTrip stopped)

[<Fact>]
let ``mintChange encodes eventId 0 on the wire`` () =
    let event = ClientHistory.mintChange "Edit node" []
    Assert.Equal(EventId.zero, event.id)
    let json = Enc.toString 0 (EventJson.encode event)
    Assert.Contains("\"eventId\":0", json)

[<Fact>]
let ``toWireBatch forces EventId.zero on a dirty new client Event`` () =
    let minted = ClientHistory.mintChange "Edit node" []
    let dirty = { minted with id = EventIdFixtures.storedId 7 }
    let wire = SyncBatch.toWireBatch [ dirty ]
    Assert.Equal(EventId.zero, wire.Head.id)
    Assert.Equal(minted.submissionId, wire.Head.submissionId)
    let json =
        Enc.toString 0 (EventJson.encodeEventBatch { events = wire })
    Assert.Contains("\"eventId\":0", json)
    Assert.DoesNotContain($"\"eventId\":{EventId.toJson dirty.id}", json)
