namespace Gambol.Server

open System
open System.Threading.Tasks
open Gambol.Shared

type private AuthorityEntry = {
    generation: Guid
    admitting: bool
    closed: bool
    routes: Set<Guid>
    routesSettled: TaskCompletionSource<unit> option
}

type private CallerSlot = {
    live: AuthorityEntry
    prior: AuthorityEntry option
}

type Session =
    private {
        gate: obj
        authorities: Map<Caller, CallerSlot> ref
    }

type SessionRoute =
    private {
        session: Session
        caller: Caller
        generation: Guid
        routeId: Guid
    }

type StoppedAdmission =
    private {
        session: Session
        caller: Caller
        generation: Guid
        routesSettled: Task
    }

[<RequireQualifiedAccess>]
module Session =

    let private openAuthority () = {
        generation = Guid.NewGuid()
        admitting = true
        closed = false
        routes = Set.empty
        routesSettled = None
    }

    let private openSlot () = {
        live = openAuthority ()
        prior = None
    }

    let create (credentials: CoreCredentials) =
        let authorities =
            CoreCredentials.callers credentials
            |> Seq.map (fun caller -> caller, openSlot ())
            |> Map.ofSeq
        { gate = obj (); authorities = ref authorities }

    /// An open admission stays. Any other login installs a new generation.
    let login session caller : Result<unit, string> =
        lock session.gate (fun () ->
            match Map.tryFind caller session.authorities.Value with
            | Some slot when slot.live.admitting -> ()
            | Some slot ->
                session.authorities.Value <-
                    Map.add
                        caller
                        { live = openAuthority ()
                          prior = Some slot.live }
                        session.authorities.Value
            | None ->
                session.authorities.Value <-
                    Map.add
                        caller
                        (openSlot ())
                        session.authorities.Value
            Ok ())

    let admissionIsOpen session caller =
        lock session.gate (fun () ->
            match Map.tryFind caller session.authorities.Value with
            | Some slot -> slot.live.admitting
            | None -> false)

    let hasAuthority session caller =
        lock session.gate (fun () ->
            match Map.tryFind caller session.authorities.Value with
            | Some slot -> not slot.live.closed
            | None -> false)

    let wasDropped session caller =
        lock session.gate (fun () ->
            match Map.tryFind caller session.authorities.Value with
            | Some slot -> slot.live.closed
            | None -> false)

    let beginAdmittedRoute session caller =
        lock session.gate (fun () ->
            match Map.tryFind caller session.authorities.Value with
            | Some slot when slot.live.admitting ->
                let routeId = Guid.NewGuid()
                let next = {
                    slot.live with
                        routes = Set.add routeId slot.live.routes
                }
                session.authorities.Value <-
                    Map.add
                        caller
                        { slot with live = next }
                        session.authorities.Value
                Some {
                    session = session
                    caller = caller
                    generation = slot.live.generation
                    routeId = routeId
                }
            | _ -> None)

    let private signalSettled
        (source: TaskCompletionSource<unit> option)
        =
        source
        |> Option.iter (fun waiter -> waiter.TrySetResult() |> ignore)

    let private withoutRoute (entry: AuthorityEntry) (route: SessionRoute) =
        if entry.generation <> route.generation
            || not (Set.contains route.routeId entry.routes) then
            None
        else
            let routes = Set.remove route.routeId entry.routes
            let settledNow =
                if Set.isEmpty routes then entry.routesSettled else None
            let next = {
                entry with
                    routes = routes
                    routesSettled =
                        if Set.isEmpty routes then None
                        else entry.routesSettled
            }
            Some (next, settledNow)

    let private finishSlot (slot: CallerSlot) (route: SessionRoute) =
        match withoutRoute slot.live route with
        | Some (next, settledNow) ->
            Some ({ slot with live = next }, settledNow)
        | None ->
            match slot.prior |> Option.bind (fun prior ->
                withoutRoute prior route) with
            | Some (next, settledNow) ->
                Some ({ slot with prior = Some next }, settledNow)
            | None -> None

    let finishAdmittedRoute (route: SessionRoute) =
        let settled =
            lock route.session.gate (fun () ->
                let authorities = route.session.authorities.Value
                match Map.tryFind route.caller authorities with
                | Some slot ->
                    match finishSlot slot route with
                    | Some (next, settledNow) ->
                        route.session.authorities.Value <-
                            Map.add route.caller next authorities
                        settledNow
                    | None -> None
                | None -> None)
        signalSettled settled

    let private settledTask (entry: AuthorityEntry) =
        match entry.routesSettled with
        | Some source -> source.Task
        | None -> Task.FromResult()

    let private closeAuthority (entry: AuthorityEntry) =
        let source =
            if Set.isEmpty entry.routes then None
            else
                Some (
                    TaskCompletionSource<unit>(
                        TaskCreationOptions.RunContinuationsAsynchronously))
        { entry with admitting = false; routesSettled = source }

    let private asStopped session caller (entry: AuthorityEntry) = {
        session = session
        caller = caller
        generation = entry.generation
        routesSettled = settledTask entry
    }

    /// Closes admission and keeps the authority until removeAuthority.
    let stopAdmitting session caller =
        lock session.gate (fun () ->
            match Map.tryFind caller session.authorities.Value with
            | None -> None
            | Some slot when not slot.live.admitting ->
                Some (asStopped session caller slot.live)
            | Some slot ->
                let closed = closeAuthority slot.live
                session.authorities.Value <-
                    Map.add
                        caller
                        { slot with live = closed }
                        session.authorities.Value
                Some (asStopped session caller closed))

    let waitForAdmittedRoutes (stopped: StoppedAdmission) = async {
        let! cancellationToken = Async.CancellationToken
        do!
            stopped.routesSettled.WaitAsync(cancellationToken)
            |> Async.AwaitTask
    }

    let private closeGeneration (slot: CallerSlot) (generation: Guid) =
        if slot.live.generation = generation then
            Some {
                slot with
                    live = {
                        slot.live with
                            closed = true
                            admitting = false
                    }
            }
        else
            match slot.prior with
            | Some prior when prior.generation = generation ->
                Some {
                    slot with
                        prior = Some {
                            prior with
                                closed = true
                                admitting = false
                        }
                }
            | _ -> None

    let removeAuthority (stopped: StoppedAdmission) =
        lock stopped.session.gate (fun () ->
            let authorities = stopped.session.authorities.Value
            match Map.tryFind stopped.caller authorities with
            | Some slot ->
                match closeGeneration slot stopped.generation with
                | Some next ->
                    stopped.session.authorities.Value <-
                        Map.add stopped.caller next authorities
                | None -> ()
            | None -> ())
