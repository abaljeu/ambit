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

    /// Authority Actor with the minted pool secret. Name stays empty.
    let actorCaller (secret: Credential) : Caller =
        { authority = Authority "Actor"
          name = ""
          secret = secret }

    let forCaller caller (door: CoreChangesDoor) =
        { door with caller = caller }

    let graphAtEvent door =
        CoreMailboxApi.graphAtEvent door.host

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

    let private acceptedUnit result =
        result |> Result.map (fun (_: CoreChangesAccepted) -> ())

    /// Shared door for an ActorFunction. The pool door stays CoreChangesDoor.
    let actorChanges (door: CoreChangesDoor) : ActorChanges =
        let post evs =
            async {
                let! result = postEvents door evs
                return acceptedUnit result
            }
        let stop result = actorStop door result
        let graph () = graphAtEvent door
        let load nodeId = loadNode door nodeId
        { postChanges = post
          actorStop = stop
          graphAtEvent = graph
          loadNode = load }
