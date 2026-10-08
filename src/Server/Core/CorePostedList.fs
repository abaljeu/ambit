namespace Gambol.Server

open Gambol.Shared

/// One posted list, applied in order after a single credential check.
[<RequireQualifiedAccess>]
module internal CorePostedList =

    type Host = {
        admit: Caller -> Result<unit, string>
        postChange:
            Ev -> Result<Ev * CoreChangesAccepted option, string>
        launch: Ev -> Result<Ev, string>
        cancel: Ev -> Result<Ev list, string>
        eventId: unit -> Result<EventId, string>
        isReady: unit -> bool
    }

    type private Acc = {
        reversed: Ev list
        ready: bool
        external: bool
        message: string option
    }

    let private isActorStop (event: Ev) =
        match event.body with
        | EventBody.ActorStop _ -> true
        | _ -> false

    let private refuseShape (events: Ev list) =
        if List.exists isActorStop events then
            Error "ActorStop is not a client event type"
        else
            Ok ()

    let private readyOf
        (accepted: CoreChangesAccepted option)
        (previous: bool)
        =
        match accepted with
        | Some value -> value.isReady
        | None -> previous

    let private push
        (acc: Acc)
        (events: Ev list)
        (ready: bool)
        (external: bool)
        (message: string option)
        =
        let reversed =
            List.fold
                (fun items event -> event :: items)
                acc.reversed
                events
        { reversed = reversed
          ready = ready
          external = acc.external || external
          message = message |> Option.orElse acc.message }

    let private step (host: Host) (event: Ev) (acc: Acc) =
        match event.body with
        | EventBody.ActorStart _ ->
            host.launch event
            |> Result.map (fun stored ->
                push acc [ stored ] acc.ready false None)
        | EventBody.Cancel _ ->
            host.cancel event
            |> Result.map (fun stored ->
                push acc stored acc.ready false None)
        | EventBody.ActorStop _ ->
            Error "ActorStop is not a client event type"
        | _ ->
            host.postChange event
            |> Result.map (fun (stored, accepted) ->
                let external, message =
                    match accepted with
                    | Some value ->
                        value.externalChanges, value.message
                    | None -> false, None
                push
                    acc
                    [ stored ]
                    (readyOf accepted acc.ready)
                    external
                    message)

    let private fold (host: Host) (events: Ev list) =
        let rec loop (acc: Acc) rest =
            match rest with
            | [] -> Ok acc
            | event :: rest ->
                match step host event acc with
                | Error err -> Error err
                | Ok next -> loop next rest
        loop
            { reversed = []
              ready = host.isReady ()
              external = false
              message = None }
            events

    let apply
        (host: Host)
        (caller: Caller)
        (events: Ev list)
        : Result<CoreChangesAccepted, string> =
        match events with
        | [] -> Error "events must not be empty"
        | _ ->
            match host.admit caller with
            | Error err -> Error err
            | Ok () ->
                match refuseShape events with
                | Error err -> Error err
                | Ok () ->
                    match fold host events with
                    | Error err -> Error err
                    | Ok acc ->
                        match host.eventId () with
                        | Error err -> Error err
                        | Ok eventId ->
                            Ok (
                                CoreChanges.accepted
                                    eventId
                                    acc.ready
                                    (List.rev acc.reversed)
                                    acc.external
                                    acc.message)
