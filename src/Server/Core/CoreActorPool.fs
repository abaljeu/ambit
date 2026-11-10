namespace Gambol.Server

open System
open Gambol.Shared

/// Actor input: Graph plus named ids and Actor secret.
type ActorInput =
    { graph: Graph
      zoomId: NodeId
      focusId: NodeId
      runDefinitionId: NodeId
      sessionId: string
      secret: Credential }

type ActorFn = ActorInput -> CoreChangesDoor -> Async<unit>

type ActorFunction = FunctionRun -> CoreChangesDoor -> Async<unit>

/// Lifecycle keys for the pool; Graph arrives only via getGraph.
type FunctionStart =
    { actor: ActorFunction
      getGraph: GraphCarrier
      actorNodeId: NodeId }

[<RequireQualifiedAccess>]
type StartError =
    | UnknownActor of name: string
    | Rejected of message: string

[<RequireQualifiedAccess>]
module StartError =
    let text error =
        match error with
        | StartError.UnknownActor name ->
            $"actor '{name}' not registered"
        | StartError.Rejected message -> message

type CoreActorPool =
    { register: ActorName -> ActorFn -> unit
      startActor:
        Gambol.Shared.ActorStart ->
            (unit -> Graph) ->
            Result<Credential, StartError>
      startFunction: FunctionStart -> Result<Credential, string>
      schedule: Credential -> CoreChangesDoor -> unit
      isLive: Credential -> bool
      admit: Credential -> Result<unit, string>
      drop: Credential -> unit
      finish: Credential -> ActorResult -> Result<unit, string>
      liveActorNodeIds: unit -> Set<NodeId>
      getActorNodeId: Credential -> NodeId option
      trySecretForActorNode: NodeId -> Credential option
      deliver: string * string -> Result<unit, string>
      takeInbox: string -> Result<string list, string> }

/// Pool, graph reader, and peer name for one Load or Save.
type LoadSaveContext =
    { pool: CoreActorPool
      getGraph: unit -> Graph
      peerName: PeerActorName }

[<RequireQualifiedAccess>]
module CoreActorPool =

    type private PendingBody =
        | BagPending of actorFn: ActorFn * input: ActorInput
        | FunctionPending of
            actor: ActorFunction *
            getGraph: GraphCarrier *
            secret: Credential

    type private LiveRow =
        { actorNodeId: NodeId
          runDefinitionId: NodeId
          sessionId: string
          inbox: string list
          cancel: System.Threading.CancellationTokenSource
          pending: PendingBody option }

    type private Model =
        { defs: Map<string, ActorFn>
          live: Map<Credential, LiveRow>
          bySession: Map<string, Credential> }

    let private liveActorNodeIds (model: Model) =
        model.live
        |> Map.toList
        |> List.map (fun (_, row) -> row.actorNodeId)
        |> Set.ofList

    let private trySecretForActorNode (model: Model) actorNodeId =
        model.live
        |> Map.tryPick (fun secret row ->
            if row.actorNodeId = actorNodeId then Some secret
            else None)

    let private runAdmit isLive secret =
        match CoreAuth.admit (isLive secret) with
        | Error err -> Error(CoreAdmissionError.text err)
        | Ok () -> Ok ()

    let private runDrop takeLive secret =
        match takeLive secret with
        | None -> ()
        | Some row -> row.cancel.Cancel()

    let private runFinish takeLive secret result =
        match result with
        | ActorSucceeded
        | ActorFailed _
        | ActorCancelled
        | ActorQuery _ ->
            runDrop takeLive secret
            Ok ()

    let private actorNameFrom (commandNode: Node) =
        match CommandRequest.actorNameFromText commandNode.text with
        | Some name -> name
        | None ->
            CssClass.toList commandNode.cssClasses
            |> List.tryPick (fun cls ->
                if cls.StartsWith("actor-") && cls.Length > 6 then
                    Some (cls.Substring(6))
                else
                    None)
            |> Option.defaultValue (
                commandNode.text.Trim().ToLowerInvariant())

    let private actorGraphFrom (fullGraph: Graph) (request: ActorStart) =
        let actorNodes =
            request.graphIds
            |> List.choose (fun id ->
                Map.tryFind id fullGraph.nodes
                |> Option.map (fun n -> id, n))
            |> Map.ofList
        let childMap =
            request.graphIds
            |> List.choose (fun id ->
                Graph.tryGetChildren fullGraph id
                |> Option.map (fun kids -> id, kids))
            |> Map.ofList
        Graph.fromExtracted request.zoomId actorNodes childMap
        |> Graph.withFocus (Some request.focusId)

    let private commandIsLive (model: Model) runDefinitionId =
        model.live
        |> Map.exists (fun _ row -> row.runDefinitionId = runDefinitionId)

    let private admitStart request (model: Model) =
        if request.graphIds.IsEmpty then
            Error
                "graphIds required: client must provide Included context (SiteMap under Zoom, honoring Fold)"
        elif Set.contains request.focusId (liveActorNodeIds model) then
            Error "focus already has a live Actor"
        elif commandIsLive model request.runDefinitionId then
            Error "command already has a live Actor"
        else
            Ok()

    let private putActorStart
        (putLive:
            Credential -> NodeId -> NodeId -> string -> PendingBody -> unit)
        (request: Gambol.Shared.ActorStart)
        (fullGraph: Graph)
        (actorFn: ActorFn)
        =
        let actorGraph = actorGraphFrom fullGraph request
        let secret =
            Credential(Guid.NewGuid().ToString("N"))
        let sessionId = Guid.NewGuid().ToString()
        let input: ActorInput =
            { graph = actorGraph
              zoomId = request.zoomId
              focusId = request.focusId
              runDefinitionId = request.runDefinitionId
              sessionId = sessionId
              secret = secret }
        putLive
            secret
            request.focusId
            request.runDefinitionId
            sessionId
            (BagPending(actorFn, input))
        Ok secret

    let private runStartActor putLive getModel request getState =
        let fullGraph = getState ()
        match admitStart request (getModel ()) with
        | Error err -> Error (StartError.Rejected err)
        | Ok() ->
            let actorGraph = actorGraphFrom fullGraph request
            match Map.tryFind request.runDefinitionId actorGraph.nodes with
            | None ->
                Error (
                    StartError.Rejected
                        "command node not found in provided graphIds")
            | Some commandNode ->
                let actorName = actorNameFrom commandNode
                match Map.tryFind actorName (getModel ()).defs with
                | None -> Error (StartError.UnknownActor actorName)
                | Some actorFn ->
                    putActorStart
                        putLive request fullGraph actorFn
                    |> Result.mapError StartError.Rejected

    let private admitFunctionStart
        (request: FunctionStart)
        (model: Model)
        =
        if Set.contains request.actorNodeId (liveActorNodeIds model) then
            Error "actor node already has a live Actor"
        else
            Ok()

    let private putFunctionStart
        (putLive:
            Credential -> NodeId -> NodeId -> string -> PendingBody -> unit)
        (request: FunctionStart)
        =
        let secret =
            Credential(Guid.NewGuid().ToString("N"))
        let sessionId = Guid.NewGuid().ToString()
        putLive
            secret
            request.actorNodeId
            request.actorNodeId
            sessionId
            (FunctionPending(request.actor, request.getGraph, secret))
        Ok secret

    let private runStartFunction
        putLive
        getModel
        (request: FunctionStart)
        =
        match admitFunctionStart request (getModel ()) with
        | Error err -> Error err
        | Ok() -> putFunctionStart putLive request

    let startLoadSaveCommand
        (_run: LoadSaveContext)
        (path: LoadSavePath)
        (_choice: LoadPathChoice)
        (_request: LoadSaveCommandRequest)
        : Result<Credential option, string>
        =
        match path with
        | LoadSavePath.Desk
        | LoadSavePath.Git -> Ok None

    let private takePending (model: Model) secret =
        match Map.tryFind secret model.live with
        | None -> model, None
        | Some row ->
            match row.pending with
            | None -> model, None
            | Some pending ->
                let live =
                    Map.add secret { row with pending = None } model.live
                { model with live = live }, Some (pending, row)

    module private LiveSession =

        let private gate = obj ()

        let private readers =
            ref Map.empty<Guid, Credential -> string option>

        let register (read: Credential -> string option) =
            let id = Guid.NewGuid()
            lock gate (fun () ->
                readers.Value <- Map.add id read readers.Value)
            id

        let unregister id =
            lock gate (fun () ->
                readers.Value <- Map.remove id readers.Value)

        let sessionIdFor (secret: Credential) =
            lock gate (fun () ->
                readers.Value
                |> Map.toList
                |> List.tryPick (fun (_, read) -> read secret))

    /// Live-row session id for an Actor credential. `putFunctionStart` stores it.
    let sessionIdFor = LiveSession.sessionIdFor

    let private runSchedule
        (takePendingBody: Credential -> (PendingBody * LiveRow) option)
        (secret: Credential)
        (coreChanges: CoreChangesDoor)
        =
        match takePendingBody secret with
        | None -> ()
        | Some (pending, row) ->
            let work =
                match pending with
                | BagPending(actorFn, input) ->
                    actorFn input coreChanges
                | FunctionPending(actor, getGraph, secret) ->
                    actor
                        { getGraph = getGraph
                          secret = secret }
                        coreChanges
            Async.Start(work, row.cancel.Token)

    let dropAndReply
        (pool: CoreActorPool)
        (secret: Credential)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        (err: string)
        =
        pool.drop secret
        reply.Reply(Error err)

    let create () : CoreActorPool =
        let mutable model =
            { defs = Map.empty
              live = Map.empty
              bySession = Map.empty }
        let getModel () = model
        let putLive secret actorNodeId runDefinitionId sessionId pending =
            let row =
                { actorNodeId = actorNodeId
                  runDefinitionId = runDefinitionId
                  sessionId = sessionId
                  inbox = []
                  cancel = new System.Threading.CancellationTokenSource()
                  pending = Some pending }
            model <-
                { model with
                    live = Map.add secret row model.live
                    bySession = Map.add sessionId secret model.bySession }
        let takeLive secret =
            match Map.tryFind secret model.live with
            | None -> None
            | Some row ->
                model <-
                    { model with
                        live = Map.remove secret model.live
                        bySession =
                            Map.remove row.sessionId model.bySession }
                Some row
        let takePendingBody secret =
            let next, pending = takePending model secret
            model <- next
            pending
        let liveRowFor sessionId =
            Map.tryFind sessionId model.bySession
            |> Option.bind (fun secret ->
                Map.tryFind secret model.live
                |> Option.map (fun row -> secret, row))
        let deliver (sessionId, text) =
            match liveRowFor sessionId with
            | None -> Error "not live"
            | Some(secret, row) ->
                let next =
                    { row with inbox = text :: row.inbox }
                model <-
                    { model with
                        live = Map.add secret next model.live }
                Ok()
        let takeInbox sessionId =
            match liveRowFor sessionId with
            | None -> Error "not live"
            | Some(secret, row) ->
                let texts = List.rev row.inbox
                model <-
                    { model with
                        live =
                            Map.add
                                secret
                                { row with inbox = [] }
                                model.live }
                Ok texts
        let readSession secret =
            Map.tryFind secret model.live
            |> Option.map (fun row -> row.sessionId)
        let readerId = LiveSession.register readSession
        let clearLive () =
            LiveSession.unregister readerId
            let live = model.live
            model <-
                { model with
                    live = Map.empty
                    bySession = Map.empty }
            live |> Map.iter (fun _ row -> row.cancel.Cancel())
        ServerRunRegistry.registerPool clearLive
        { register =
            fun (ActorName name) actor ->
                model <- { model with defs = Map.add name actor model.defs }
          startActor = runStartActor putLive getModel
          startFunction = runStartFunction putLive getModel
          schedule = runSchedule takePendingBody
          isLive = fun secret -> Map.containsKey secret model.live
          admit = runAdmit (fun secret -> Map.containsKey secret model.live)
          drop = runDrop takeLive
          finish = runFinish takeLive
          liveActorNodeIds = fun () -> liveActorNodeIds model
          getActorNodeId =
            fun secret ->
                Map.tryFind secret model.live
                |> Option.map (fun row -> row.actorNodeId)
          trySecretForActorNode =
            fun actorNodeId -> trySecretForActorNode model actorNodeId
          deliver = deliver
          takeInbox = takeInbox }
