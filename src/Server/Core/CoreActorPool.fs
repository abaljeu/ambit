namespace Gambol.Server

open System
open Gambol.Shared

type ActorName = ActorName of string

type PublicNumber = PublicNumber of int

type LaunchRequest =
    { name: ActorName
      revision: Revision
      span: NodeRange }

type ActorFn = Graph -> Credential -> CoreChanges -> Async<unit>

type CoreActorPool =
    { register: ActorName -> ActorFn -> unit
      launch:
        CoreChanges -> LaunchRequest -> Async<Result<PublicNumber, string>>
      query: PublicNumber -> Result<LaunchRequest, string>
      lockedIds: unit -> Set<NodeId>
      withLocks: CoreChanges -> CoreChanges }

[<RequireQualifiedAccess>]
module CoreActorPool =

    let unknownActor = CoreAdmissionError.text UnknownActor

    let unknownJob = CoreAdmissionError.text UnknownJob

    let overlap = CoreAdmissionError.text Overlap

    type private Job =
        { credential: Credential
          span: NodeRange
          spanIds: Set<NodeId>
          revision: Revision
          name: ActorName }

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

    let private tracked (job: Job) : LaunchRequest =
        { name = job.name
          revision = job.revision
          span = job.span }

    let private nameKey (ActorName name) = name

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
                    let cred =
                        Credential(Guid.NewGuid().ToString("N"))
                    Ok
                        { number = model.next
                          actor = actor
                          subgraph = subgraph
                          credential = cred
                          job =
                            { credential = cred
                              span = request.span
                              spanIds = ids
                              revision = request.revision
                              name = request.name } }

    let private applyPlan (model: Model) (plan: LaunchPlan) : Model =
        { next = model.next + 1
          defs = model.defs
          jobs = Map.add plan.number plan.job model.jobs
          locked = Set.union model.locked plan.job.spanIds }

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
                        plan.actor plan.subgraph plan.credential bound)
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

    let create (credentials: CoreCredentials) : CoreActorPool =
        let table = SynchronizedTable()
        let lockedIds () = table.GetLockedIds()
        { register = fun name actor -> table.Register(name, actor)
          lockedIds = lockedIds
          withLocks = overlayLocks lockedIds
          launch = runLaunch table credentials
          query = runQuery table }
