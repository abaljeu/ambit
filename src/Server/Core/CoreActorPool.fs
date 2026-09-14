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

type ActorResult = | ActorSucceeded

type ActorFn = Graph -> Credential -> CoreChanges -> Async<unit>

type CoreActorPool =
    { register: ActorName -> ActorFn -> unit
      startActor: StartActorRequest -> Result<unit, string>
      isLive: Credential -> bool
      admit: Credential -> Result<unit, string>
      drop: Credential -> unit
      finish: Credential -> ActorResult -> Result<unit, string>
      liveFocusIds: unit -> Set<NodeId> }

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

    let private runStartActor
        (putLive: Credential -> NodeId -> unit)
        (credentials: CoreCredentials)
        (request: StartActorRequest)
        =
        let secret = Credential(Guid.NewGuid().ToString("N"))
        credentials.add secret |> Async.RunSynchronously
        putLive secret request.focusId
        Ok ()

    let create (credentials: CoreCredentials) : CoreActorPool =
        let mutable model =
            { defs = Map.empty
              live = Map.empty }
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
        { register =
            fun (ActorName name) actor ->
                model <- { model with defs = Map.add name actor model.defs }
          startActor = runStartActor putLive credentials
          isLive = isLive
          admit = runAdmit isLive
          drop = runDrop takeLive credentials
          finish = runFinish isLive takeLive credentials
          liveFocusIds = fun () -> liveFocusIds model }
