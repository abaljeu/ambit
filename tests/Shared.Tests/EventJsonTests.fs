module EventJsonTests

open System
open Xunit
open Gambol.Shared
open Gambol.Shared.Events

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

let private roundTrip (event: Event) : Event =
    let json = Enc.toString 0 (EventLog.encode event)
    match Dec.fromString EventLog.decode json with
    | Ok decoded -> decoded
    | Error err -> failwith $"Decode failed: {err}"

let private changeEvent : Event =
    let nodeId = NodeId(Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"))
    { id = EventId 3
      submissionId = Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
      authority = Authority "Browser"
      commandName = "Set text"
      body = EventBody.Change [ Op.SetText(nodeId, "old", "new") ] }

[<Fact>]
let ``Event JSON round-trips a Change`` () =
    let decoded = roundTrip changeEvent
    Assert.Equal(changeEvent, decoded)

[<Fact>]
let ``Event JSON round-trips name-only Undo`` () =
    let event =
        { changeEvent with
            id = EventId 4
            commandName = "Undo"
            body = EventBody.Undo(EventId 3, []) }
    Assert.Equal(event, roundTrip event)

[<Fact>]
let ``Event JSON round-trips ActorStart and ActorStop`` () =
    let zoom = NodeId(Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"))
    let start =
        { zoomId = zoom
          focusId = NodeId(Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"))
          commandId = NodeId(Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"))
          graphIds = [ zoom ]
          revision = EventId 4 }
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
