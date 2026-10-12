namespace Gambol.Server

open Gambol.Shared

[<RequireQualifiedAccess>]
module CoreDoor =

    let private readOnlyMsg =
        "Database persistence is unavailable; file fallback is read-only."

    let private readOnlyResult () =
        async.Return(Error readOnlyMsg)

    let create writable host caller : CoreChangesDoor =
        { host = host
          caller = caller
          writable = writable }

    let forCaller caller (door: CoreChangesDoor) =
        { door with caller = caller }

    let getState door =
        CoreMailboxApi.getState door.host

    let getEventId door =
        CoreMailboxApi.getEventId door.host

    let getEventsSince door after =
        CoreMailboxApi.getEventsSince door.host after

    let isReady door =
        CoreMailboxApi.isReady door.host

    let callIfReady x door events = 
        if door.writable then
            x door.host door.caller events
        else
            readOnlyResult ()
    let postEvents door events =
        callIfReady CoreMailboxApi.postEvents door events

    let postGraphOnly door event =
        callIfReady CoreMailboxApi.postGraphOnly door event

    let actorStop door result =
        callIfReady CoreMailboxApi.actorStop door result

    let loadNode door subject =
        callIfReady CoreMailboxApi.mbLoadNode door subject
