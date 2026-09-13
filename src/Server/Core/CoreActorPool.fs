namespace Gambol.Server

open System
open System.Threading
open System.Threading.Tasks
open Gambol.Shared

type ActorName = ActorName of string

type PublicNumber = PublicNumber of int

type LaunchRequest =
    { name: ActorName
      revision: Revision
      span: NodeRange }

type ActorFn =
    NodeId -> Graph -> Credential -> CoreChanges -> Async<unit>

type StartActorRequest =
    { caller: Authority
      secret: Credential
      name: ActorName
      focus: NodeId }

type CoreActorPool =
    { register: ActorName -> ActorFn -> unit
      launch:
        CoreChanges -> LaunchRequest -> Async<Result<PublicNumber, string>>
      query: PublicNumber -> Result<LaunchRequest, string>
      lockedIds: unit -> Set<NodeId>
      withLocks: CoreChanges -> CoreChanges
      startActor:
        ActorName -> NodeId -> Graph -> Revision ->
        (Authority -> Credential -> CoreChanges) ->
        EventLog -> Result<ActorStarted, string>
      admit: Authority -> Credential -> Result<unit, string>
      drop: Authority -> unit
      isLive: Authority -> bool }

[<RequireQualifiedAccess>]
module CoreActorPool =

    let unknownActor = CoreAdmissionError.text UnknownActor

    let unknownJob = CoreAdmissionError.text UnknownJob

    let overlap = CoreAdmissionError.text Overlap

    let unknownFocus = "unknown focus"

    let focusBusy = "focus has a live actor"

    type private Job =
        { credential: Credential
          span: NodeRange
          spanIds: Set<NodeId>
          revision: Revision
          name: ActorName
          authority: Authority
          focus: NodeId
          terminate: CancellationTokenSource
          body: Task option }

    type private Model =
        { next: int
          defs: Map<string, ActorFn>
          jobs: Map<int, Job>
          locked: Set<NodeId> }

    type private LaunchPlan =
        { number: int
          actor: ActorFn
          subgraph: Graph
          credential: Credential
          job: Job }

    type private StartPlan =
        { number: int
          actorFn: ActorFn
          publicId: Authority
          secret: Credential
          cts: CancellationTokenSource }

    let private tracked (job: Job) : LaunchRequest =
        { name = job.name
          revision = job.revision
          span = job.span }

    let private nameKey (ActorName name) = name

    let private newSecret () =
        Credential(Guid.NewGuid().ToString("N"))

    let private newTerminate () = new CancellationTokenSource()

    let private jobAuthority number =
        Authority($"actor-{number}")

    let private emptySpan focus : NodeRange =
        { pnode = focus; start = 0; endd = 0 }

    let private focusTaken (jobs: Map<int, Job>) focus =
        jobs |> Map.exists (fun _ job -> job.focus = focus)

    let private findByAuthority
        (jobs: Map<int, Job>)
        (authority: Authority)
        : (int * Job) option =
        jobs
        |> Map.tryPick (fun number (job: Job) ->
            if job.authority = authority then
                Some(number, job)
            else
                None)

    let private planLaunch
        (model: Model)
        (graph: Graph)
        (request: LaunchRequest)
        : Result<LaunchPlan, string> =
        match Map.tryFind (nameKey request.name) model.defs with
        | None -> Error unknownActor
        | Some actor ->
            match GraphSpan.spanIds graph request.span with
            | Error err -> Error err
            | Ok ids when not (Set.isEmpty (Set.intersect ids model.locked)) ->
                Error overlap
            | Ok ids ->
                match GraphSpan.extract graph request.span with
                | Error err -> Error err
                | Ok subgraph ->
                    let cred = newSecret ()
                    let number = model.next
                    Ok
                        { number = number
                          actor = actor
                          subgraph = subgraph
                          credential = cred
                          job =
                            { credential = cred
                              span = request.span
                              spanIds = ids
                              revision = request.revision
                              name = request.name
                              authority = jobAuthority number
                              focus = request.span.pnode
                              terminate = newTerminate ()
                              body = None } }

    let private applyPlan (model: Model) (plan: LaunchPlan) : Model =
        { next = model.next + 1
          defs = model.defs
          jobs = Map.add plan.number plan.job model.jobs
          locked = Set.union model.locked plan.job.spanIds }

    let private planStart
        (model: Model)
        (name: ActorName)
        (focus: NodeId)
        (graph: Graph)
        (revision: Revision)
        : Result<StartPlan * Job, string> =
        match Map.tryFind (nameKey name) model.defs with
        | None -> Error unknownActor
        | Some _ when Map.tryFind focus graph.nodes |> Option.isNone ->
            Error unknownFocus
        | Some _ when focusTaken model.jobs focus -> Error focusBusy
        | Some actor ->
            let cred = newSecret ()
            let number = model.next
            let terminate = newTerminate ()
            let authority = jobAuthority number
            let job =
                { credential = cred
                  span = emptySpan focus
                  spanIds = Set.empty
                  revision = revision
                  name = name
                  authority = authority
                  focus = focus
                  terminate = terminate
                  body = None }
            Ok(
                { number = number
                  actorFn = actor
                  publicId = authority
                  secret = cred
                  cts = terminate },
                job)

    type private SynchronizedTable() =
        let lockObj = obj ()
        // Ticket 30 exception: table mutators are synchronous (not mailbox-based)
        let mutable model =
            { next = 1
              defs = Map.empty
              jobs = Map.empty
              locked = Set.empty }

        member _.Register(ActorName name, actor) =
            lock lockObj (fun () ->
                model <- { model with defs = Map.add name actor model.defs })

        member _.TryPlanLaunch(graph, request) =
            lock lockObj (fun () ->
                match planLaunch model graph request with
                | Error err -> Error err
                | Ok plan ->
                    model <- applyPlan model plan
                    Ok plan)

        member _.TryStart(name, focus, graph, revision) =
            lock lockObj (fun () ->
                match planStart model name focus graph revision with
                | Error err -> Error err
                | Ok (plan, job) ->
                    model <-
                        { next = model.next + 1
                          defs = model.defs
                          jobs = Map.add plan.number job model.jobs
                          locked = model.locked }
                    Ok plan)

        member _.SetBody(number, task) =
            lock lockObj (fun () ->
                match Map.tryFind number model.jobs with
                | None -> ()
                | Some job ->
                    let jobs =
                        Map.add number { job with body = Some task } model.jobs
                    model <- { model with jobs = jobs })

        member _.Admit(authority, secret) =
            lock lockObj (fun () ->
                match findByAuthority model.jobs authority with
                | Some (_, job) when job.credential = secret -> Ok ()
                | _ -> Error CoreAuth.refuse)

        member _.IsLive(authority) =
            lock lockObj (fun () ->
                findByAuthority model.jobs authority |> Option.isSome)

        member _.TryDrop(authority) : Job option =
            lock lockObj (fun () ->
                match findByAuthority model.jobs authority with
                | None -> None
                | Some (number, job) ->
                    model <-
                        { model with
                            jobs = Map.remove number model.jobs
                            locked = Set.difference model.locked job.spanIds }
                    Some job)

        member _.Query(number) =
            lock lockObj (fun () ->
                Map.tryFind number model.jobs |> Option.map tracked)

        member _.GetLockedIds() =
            lock lockObj (fun () -> model.locked)

    let private overlayLocks
        (lockedIds: unit -> Set<NodeId>)
        (handle: CoreChanges)
        : CoreChanges =
        { handle with
            getState =
                fun () -> async {
                    let! state = handle.getState ()
                    match state with
                    | Error err -> return Error err
                    | Ok s ->
                        let ids = lockedIds ()
                        let graph = GraphSpan.withLockPresent ids s.graph
                        return Ok { s with graph = graph }
                } }

    let private runLaunch
        (table: SynchronizedTable)
        (credentials: CoreCredentials)
        (handle: CoreChanges)
        (request: LaunchRequest)
        : Async<Result<PublicNumber, string>> =
        async {
            let! state = handle.getState ()
            match state with
            | Error err -> return Error err
            | Ok s ->
                let planned = table.TryPlanLaunch(s.graph, request)
                match planned with
                | Error err -> return Error err
                | Ok plan ->
                    do! credentials.add plan.credential
                    let bound =
                        CoreAuth.bindHandle
                            credentials
                            plan.credential
                            handle
                    Async.Start(
                        plan.actor
                            plan.job.focus
                            plan.subgraph
                            plan.credential
                            bound)
                    return Ok(PublicNumber plan.number)
        }

    let private runQuery
        (table: SynchronizedTable)
        (PublicNumber number)
        : Result<LaunchRequest, string> =
        let found = table.Query(number)
        match found with
        | Some job -> Ok job
        | None -> Error unknownJob

    let private runStart
        (table: SynchronizedTable)
        name
        focus
        graph
        revision
        bind
        (events: EventLog)
        : Result<ActorStarted, string> =
        match table.TryStart(name, focus, graph, revision) with
        | Error err -> Error err
        | Ok plan ->
            let started =
                events.appendStarted plan.publicId focus
            let handle = bind plan.publicId plan.secret
            let work =
                plan.actorFn focus graph plan.secret handle
            let task =
                Async.StartAsTask(
                    work,
                    cancellationToken = plan.cts.Token)
            table.SetBody(plan.number, task)
            Ok started

    let private runAdmit
        (table: SynchronizedTable)
        authority
        secret
        =
        table.Admit(authority, secret)

    let private runDrop (table: SynchronizedTable) authority =
        match table.TryDrop(authority) with
        | None -> ()
        | Some (job: Job) ->
            match job.body with
            | Some t when not t.IsCompleted ->
                job.terminate.Cancel()
            | _ -> ()

    let create (credentials: CoreCredentials) : CoreActorPool =
        let table = SynchronizedTable()
        let lockedIds () = table.GetLockedIds()
        { register = fun name actor -> table.Register(name, actor)
          lockedIds = lockedIds
          withLocks = overlayLocks lockedIds
          launch = runLaunch table credentials
          query = runQuery table
          startActor = runStart table
          admit = runAdmit table
          drop = runDrop table
          isLive = fun auth -> table.IsLive auth }
