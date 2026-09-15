namespace Gambol.Server

open System
open Gambol.Shared

type ActorName = ActorName of string

/// One Command / StartActor payload for CoreMsg, CoreMailbox, and the pool.
type StartActorRequest =
    { zoomId: NodeId
      focusId: NodeId
      commandId: NodeId
      graphIds: NodeId list
      revision: Revision }

/// Actor input: Graph plus named ids and Actor secret.
type ActorInput =
    { graph: Graph
      zoomId: NodeId
      focusId: NodeId
      commandId: NodeId
      secret: Credential }

type ActorFn = ActorInput -> CoreChanges -> Async<unit>

/// Callback to append ActorStarted to mailbox History.
type AppendActorStarted = NodeId -> string -> unit

type CoreActorPool =
    { register: ActorName -> ActorFn -> unit
      startActor: StartActorRequest -> (unit -> Graph) -> CoreChanges -> AppendActorStarted -> Result<unit, string>
      isLive: Credential -> bool
      admit: Credential -> Result<unit, string>
      drop: Credential -> unit
      finish: Credential -> ActorResult -> Result<unit, string>
      liveFocusIds: unit -> Set<NodeId>
      getFocusId: Credential -> NodeId option }

[<RequireQualifiedAccess>]
module CoreActorPool =

    type private LiveRow =
        { focusId: NodeId
          cancel: System.Threading.CancellationTokenSource }

    type private Model =
        { defs: Map<string, ActorFn>
          live: Map<Credential, LiveRow> }

    let private liveFocusIds (model: Model) =
        model.live
        |> Map.toList
        |> List.map (fun (_, row) -> row.focusId)
        |> Set.ofList

    let private runAdmit isLive secret =
        match CoreAuth.admit (isLive secret) with
        | Error err -> Error(CoreAdmissionError.text err)
        | Ok () -> Ok ()

    let private runDrop takeLive credentials secret =
        match takeLive secret with
        | None -> ()
        | Some row ->
            row.cancel.Cancel()
            credentials.remove secret |> Async.RunSynchronously

    let private runFinish isLive takeLive credentials secret result =
        match result with
        | ActorSucceeded ->
            match runAdmit isLive secret with
            | Error err -> Error err
            | Ok () ->
                runDrop takeLive credentials secret
                Ok ()
        | ActorFailed ->
            match runAdmit isLive secret with
            | Error err -> Error err
            | Ok () ->
                runDrop takeLive credentials secret
                Ok ()

    let private runStartActor
        (getModel: unit -> Model)
        (putLive: Credential -> NodeId -> unit)
        (credentials: CoreCredentials)
        (request: StartActorRequest)
        (getState: unit -> Graph)
        (coreChanges: CoreChanges)
        (appendActorStarted: AppendActorStarted)
        =
        let fullGraph = getState ()
        
        // Build Actor input Graph from client-provided graphIds. Server does NOT
        // expand from Zoom root; client walks its SiteMap (honoring Fold) and sends graphIds.
        if request.graphIds.IsEmpty then
            Error "graphIds required: client must provide Included context (SiteMap under Zoom, honoring Fold)"
        else
            let actorNodes =
                request.graphIds
                |> List.choose (fun id -> Map.tryFind id fullGraph.nodes |> Option.map (fun n -> id, n))
                |> Map.ofList
            let actorGraph = Graph.fromNodes fullGraph.root actorNodes
            
            match Map.tryFind request.commandId actorGraph.nodes with
            | None -> Error "command node not found in provided graphIds"
            | Some commandNode ->
                // Actor selection: prefer CSS class "actor-<name>" for explicit marking,
                // fall back to command node text for interim compatibility.
                // This separates actor selection from command interpretation (TestActor reads text).
                let actorName =
                    CssClass.toList commandNode.cssClasses
                    |> List.tryPick (fun cls ->
                        if cls.StartsWith("actor-") && cls.Length > 6 then
                            Some (cls.Substring(6))
                        else
                            None)
                    |> Option.defaultValue (commandNode.text.Trim().ToLowerInvariant())
                
                let model = getModel ()
                match Map.tryFind actorName model.defs with
                | None -> Error $"actor '{actorName}' not registered"
                | Some actorFn ->
                    let secret = Credential(Guid.NewGuid().ToString("N"))
                    credentials.add secret |> Async.RunSynchronously
                    putLive secret request.focusId
                    
                    appendActorStarted request.focusId "Actor"
                    
                    let input: ActorInput =
                        { graph = actorGraph
                          zoomId = request.zoomId
                          focusId = request.focusId
                          commandId = request.commandId
                          secret = secret }
                    
                    let modelWithLive = getModel ()
                    let cts = modelWithLive.live.[secret].cancel
                    Async.Start(actorFn input coreChanges, cts.Token)
                    
                    Ok ()

    let create (credentials: CoreCredentials) : CoreActorPool =
        let mutable model =
            { defs = Map.empty
              live = Map.empty }
        let getModel () = model
        let putLive secret focusId =
            let row =
                { focusId = focusId
                  cancel = new System.Threading.CancellationTokenSource() }
            model <- { model with live = Map.add secret row model.live }
        let takeLive secret =
            match Map.tryFind secret model.live with
            | None -> None
            | Some row ->
                model <-
                    { model with live = Map.remove secret model.live }
                Some row
        let isLive secret = Map.containsKey secret model.live
        let getFocusId secret =
            Map.tryFind secret model.live
            |> Option.map (fun row -> row.focusId)
        { register =
            fun (ActorName name) actor ->
                model <- { model with defs = Map.add name actor model.defs }
          startActor = runStartActor getModel putLive credentials
          isLive = isLive
          admit = runAdmit isLive
          drop = runDrop takeLive credentials
          finish = runFinish isLive takeLive credentials
          liveFocusIds = fun () -> liveFocusIds model
          getFocusId = getFocusId }
