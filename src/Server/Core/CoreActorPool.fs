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
      lockedIds: unit -> Set<NodeId>
      withLocks: CoreChanges -> CoreChanges
      startActor: StartActorRequest -> Async<Result<unit, string>>
      isLive: Credential -> bool
      admit: Credential -> Result<unit, string>
      drop: Credential -> unit
      finish: Credential -> ActorResult -> Result<unit, string> }

[<RequireQualifiedAccess>]
module CoreActorPool =

    type private LiveRow =
        { focusId: NodeId
          cancel: System.Threading.CancellationTokenSource }

    type private Model =
        { defs: Map<string, ActorFn>
          locked: Set<NodeId>
          live: Map<Credential, LiveRow> }

    type private SynchronizedTable() =
        let lockObj = obj ()
        // Ticket 30 exception: table mutators are synchronous (not mailbox-based)
        let mutable model =
            { defs = Map.empty
              locked = Set.empty
              live = Map.empty }

        member _.Register(ActorName name, actor) =
            lock lockObj (fun () ->
                model <- { model with defs = Map.add name actor model.defs })

        member _.GetLockedIds() =
            lock lockObj (fun () -> model.locked)

        member _.PutLive(secret, focusId) =
            lock lockObj (fun () ->
                let row =
                    { focusId = focusId
                      cancel = new System.Threading.CancellationTokenSource() }
                model <- { model with live = Map.add secret row model.live })

        member _.IsLive(secret) =
            lock lockObj (fun () -> Map.containsKey secret model.live)

        member _.TakeLive(secret) =
            lock lockObj (fun () ->
                match Map.tryFind secret model.live with
                | None -> None
                | Some row ->
                    model <-
                        { model with live = Map.remove secret model.live }
                    Some row)

    let private overlayLocks
        (lockedIds: unit -> Set<NodeId>)
        (handle: CoreChanges)
        : CoreChanges =
        let rec wrap h : CoreChanges =
            { h with
                getState =
                    fun () -> async {
                        let! state = h.getState ()
                        match state with
                        | Error err -> return Error err
                        | Ok s ->
                            let ids = lockedIds ()
                            let graph =
                                GraphSpan.withLockPresent ids s.graph
                            return Ok { s with graph = graph }
                    }
                asCaller = fun caller -> wrap (h.asCaller caller) }
        wrap handle

    let private runStartActor
        (table: SynchronizedTable)
        (credentials: CoreCredentials)
        (request: StartActorRequest)
        : Async<Result<unit, string>> =
        async {
            let secret = Credential(Guid.NewGuid().ToString("N"))
            do! credentials.add secret
            table.PutLive(secret, request.focusId)
            return Ok ()
        }

    let private runAdmit (table: SynchronizedTable) (secret: Credential) =
        match CoreAuth.admit (table.IsLive secret) with
        | Error err -> Error(CoreAdmissionError.text err)
        | Ok () -> Ok ()

    let private runDrop
        (table: SynchronizedTable)
        (credentials: CoreCredentials)
        (secret: Credential)
        : unit =
        match table.TakeLive secret with
        | None -> ()
        | Some row ->
            row.cancel.Cancel()
            credentials.remove secret |> Async.RunSynchronously

    let private runFinish
        (table: SynchronizedTable)
        (credentials: CoreCredentials)
        (secret: Credential)
        (result: ActorResult)
        : Result<unit, string> =
        match result with
        | ActorSucceeded ->
            match runAdmit table secret with
            | Error err -> Error err
            | Ok () ->
                runDrop table credentials secret
                Ok ()

    let create (credentials: CoreCredentials) : CoreActorPool =
        let table = SynchronizedTable()
        let lockedIds () = table.GetLockedIds()
        { register = fun name actor -> table.Register(name, actor)
          lockedIds = lockedIds
          withLocks = overlayLocks lockedIds
          startActor = runStartActor table credentials
          isLive = table.IsLive
          admit = runAdmit table
          drop = runDrop table credentials
          finish = runFinish table credentials }
