namespace Gambol.Server

open System
open Gambol.Shared

type ActorName = ActorName of string

type PeerActorName = PeerActorName of string

/// Actor input: Graph plus named ids and Actor secret.
type ActorInput =
    { graph: Graph
      zoomId: NodeId
      focusId: NodeId
      commandId: NodeId
      sessionId: string
      secret: Credential }

type ActorFn = ActorInput -> CoreChanges -> Async<unit>

/// Graph carrier the Actor calls. The pool does not invoke this.
type GraphCarrier = unit -> Graph

/// What a function-shaped Actor receives. No Graph or ids bag.
type FunctionRun =
    { getGraph: GraphCarrier
      secret: Credential }

type FunctionActor = FunctionRun -> CoreChanges -> Async<unit>

/// Lifecycle keys for the pool; Graph arrives only via getGraph.
type FunctionStart =
    { actor: FunctionActor
      getGraph: GraphCarrier
      focusId: NodeId
      commandId: NodeId }

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
      registerPeer: PeerActorName -> ActorFn -> unit
      startActor:
        Gambol.Shared.ActorStart ->
            (unit -> Graph) ->
            Result<Credential, StartError>
      startFunction: FunctionStart -> Result<Credential, string>
      startPeerActor:
        PeerActorName ->
            Gambol.Shared.ActorStart ->
            (unit -> Graph) ->
            Result<Credential, string>
      schedule: Credential -> CoreChanges -> unit
      isLive: Credential -> bool
      admit: Credential -> Result<unit, string>
      drop: Credential -> unit
      finish: Credential -> ActorResult -> Result<unit, string>
      liveFocusIds: unit -> Set<NodeId>
      getFocusId: Credential -> NodeId option
      trySecretForFocus: NodeId -> Credential option
      deliver: string * string -> Result<unit, string>
      takeInbox: string -> Result<string list, string> }

/// Pool, graph reader, and peer name for one Load or Save.
type LoadSaveContext =
    { pool: CoreActorPool
      getState: unit -> Graph
      peerName: PeerActorName }

[<RequireQualifiedAccess>]
module CoreActorPool =

    type private PendingBody =
        | BagPending of actorFn: ActorFn * input: ActorInput
        | FunctionPending of
            actor: FunctionActor *
            getGraph: GraphCarrier *
            secret: Credential

    type private LiveRow =
        { focusId: NodeId
          commandId: NodeId
          sessionId: string
          inbox: string list
          cancel: System.Threading.CancellationTokenSource
          pending: PendingBody option }

    type private Model =
        { defs: Map<string, ActorFn>
          peerDefs: Map<string, ActorFn>
          live: Map<Credential, LiveRow>
          bySession: Map<string, Credential> }

    let private liveFocusIds (model: Model) =
        model.live
        |> Map.toList
        |> List.map (fun (_, row) -> row.focusId)
        |> Set.ofList

    let private trySecretForFocus (model: Model) focusId =
        model.live
        |> Map.tryPick (fun secret row ->
            if row.focusId = focusId then Some secret
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

    let private commandIsLive (model: Model) commandId =
        model.live
        |> Map.exists (fun _ row -> row.commandId = commandId)

    let private admitStart request (model: Model) =
        if request.graphIds.IsEmpty then
            Error
                "graphIds required: client must provide Included context (SiteMap under Zoom, honoring Fold)"
        elif Set.contains request.focusId (liveFocusIds model) then
            Error "focus already has a live Actor"
        elif commandIsLive model request.commandId then
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
              commandId = request.commandId
              sessionId = sessionId
              secret = secret }
        putLive
            secret
            request.focusId
            request.commandId
            sessionId
            (BagPending(actorFn, input))
        Ok secret

    let private runStartActor putLive getModel request getState =
        let fullGraph = getState ()
        match admitStart request (getModel ()) with
        | Error err -> Error (StartError.Rejected err)
        | Ok() ->
            let actorGraph = actorGraphFrom fullGraph request
            match Map.tryFind request.commandId actorGraph.nodes with
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
        if Set.contains request.focusId (liveFocusIds model) then
            Error "focus already has a live Actor"
        elif commandIsLive model request.commandId then
            Error "command already has a live Actor"
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
            request.focusId
            request.commandId
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

    let private runStartPeerActor putLive getModel peerName request getState =
        let fullGraph = getState ()
        match admitStart request (getModel ()) with
        | Error err -> Error err
        | Ok () ->
            let (PeerActorName name) = peerName
            match Map.tryFind name (getModel ()).peerDefs with
            | None -> Error $"Peer Actor '{name}' not registered"
            | Some actorFn ->
                putActorStart putLive request fullGraph actorFn

    let startLoadSaveCommand
        (run: LoadSaveContext)
        (path: LoadSavePath)
        (choice: LoadPathChoice)
        (request: LoadSaveCommandRequest)
        : Result<Credential option, string>
        =
        match path with
        | LoadSavePath.Desk -> Ok None
        | LoadSavePath.Git ->
            run.pool.startPeerActor
                run.peerName
                request.start
                run.getState
            |> Result.map Some

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

    let private runSchedule
        (takePendingBody: Credential -> (PendingBody * LiveRow) option)
        (secret: Credential)
        (coreChanges: CoreChanges)
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
              peerDefs = Map.empty
              live = Map.empty
              bySession = Map.empty }
        let getModel () = model
        let putLive secret focusId commandId sessionId pending =
            let row =
                { focusId = focusId
                  commandId = commandId
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
        { register =
            fun (ActorName name) actor ->
                model <- { model with defs = Map.add name actor model.defs }
          registerPeer =
            fun (PeerActorName name) actor ->
                model <-
                    { model with
                        peerDefs = Map.add name actor model.peerDefs }
          startActor = runStartActor putLive getModel
          startFunction = runStartFunction putLive getModel
          startPeerActor = runStartPeerActor putLive getModel
          schedule = runSchedule takePendingBody
          isLive = fun secret -> Map.containsKey secret model.live
          admit = runAdmit (fun secret -> Map.containsKey secret model.live)
          drop = runDrop takeLive
          finish = runFinish takeLive
          liveFocusIds = fun () -> liveFocusIds model
          getFocusId =
            fun secret ->
                Map.tryFind secret model.live
                |> Option.map (fun row -> row.focusId)
          trySecretForFocus =
            fun focusId -> trySecretForFocus model focusId
          deliver = deliver
          takeInbox = takeInbox }
