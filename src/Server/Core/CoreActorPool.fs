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
      query: PublicNumber -> Async<Result<LaunchRequest, string>>
      lockedIds: unit -> Async<Set<NodeId>>
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

    type private Msg =
        | Register of ActorName * ActorFn * AsyncReplyChannel<unit>
        | TryLaunch of
            Graph *
            LaunchRequest *
            AsyncReplyChannel<Result<LaunchPlan, string>>
        | Query of int * AsyncReplyChannel<LaunchRequest option>
        | GetLocked of AsyncReplyChannel<Set<NodeId>>
        | DeleteActor of int

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

    let private startMailbox () =
        MailboxProcessor.Start(fun inbox ->
            let rec loop model = async {
                let! msg = inbox.Receive()
                match msg with
                | Register(ActorName name, actor, reply) ->
                    reply.Reply()
                    return!
                        loop { model with defs = Map.add name actor model.defs }
                | GetLocked reply ->
                    reply.Reply model.locked
                    return! loop model
                | Query(number, reply) ->
                    reply.Reply(
                        Map.tryFind number model.jobs |> Option.map tracked)
                    return! loop model
                | TryLaunch(graph, request, reply) ->
                    match planLaunch model graph request with
                    | Error err ->
                        reply.Reply(Error err)
                        return! loop model
                    | Ok plan ->
                        reply.Reply(Ok plan)
                        return! loop (applyPlan model plan)
                | DeleteActor number ->
                    match Map.tryFind number model.jobs with
                    | None -> return! loop model
                    | Some job ->
                        return!
                            loop
                                { model with
                                    jobs = Map.remove number model.jobs
                                    locked =
                                        Set.difference model.locked job.spanIds }
            }
            loop
                { next = 1
                  defs = Map.empty
                  jobs = Map.empty
                  locked = Set.empty })

    let private overlayLocks
        (lockedIds: unit -> Async<Set<NodeId>>)
        (handle: CoreChanges)
        : CoreChanges =
        { handle with
            getState =
                fun () -> async {
                    let! state = handle.getState ()
                    match state with
                    | Error err -> return Error err
                    | Ok s ->
                        let! ids = lockedIds ()
                        let graph = GraphSpan.withLockPresent ids s.graph
                        return Ok { s with graph = graph }
                } }

    let private runLaunch
        (mailbox: MailboxProcessor<Msg>)
        (credentials: CoreCredentials)
        (handle: CoreChanges)
        (request: LaunchRequest)
        : Async<Result<PublicNumber, string>> =
        async {
            let! state = handle.getState ()
            match state with
            | Error err -> return Error err
            | Ok s ->
                let! planned =
                    mailbox.PostAndAsyncReply(fun reply ->
                        TryLaunch(s.graph, request, reply))
                match planned with
                | Error err -> return Error err
                | Ok plan ->
                    do! credentials.add plan.credential
                    let bound =
                        CoreAuth.bindHandle
                            credentials
                            plan.credential
                            handle
                    Async.Start(async {
                        do! plan.actor plan.subgraph plan.credential bound
                        do! credentials.remove plan.credential
                        mailbox.Post(DeleteActor plan.number)
                    })
                    return Ok(PublicNumber plan.number)
        }

    let private runQuery
        (mailbox: MailboxProcessor<Msg>)
        (PublicNumber number)
        : Async<Result<LaunchRequest, string>> =
        async {
            let! found =
                mailbox.PostAndAsyncReply(fun reply -> Query(number, reply))
            match found with
            | Some job -> return Ok job
            | None -> return Error unknownJob
        }

    let create (credentials: CoreCredentials) : CoreActorPool =
        let mailbox = startMailbox ()
        let lockedIds () = mailbox.PostAndAsyncReply GetLocked
        { register =
            fun name actor ->
                mailbox.PostAndAsyncReply(fun reply ->
                    Register(name, actor, reply))
                |> Async.RunSynchronously
          lockedIds = lockedIds
          withLocks = overlayLocks lockedIds
          launch = runLaunch mailbox credentials
          query = runQuery mailbox }
