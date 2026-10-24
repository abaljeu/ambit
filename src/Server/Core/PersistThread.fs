namespace Gambol.Server

open System
open System.Threading
open Gambol.Shared

/// Dependencies for the long-lived persist thread. The thread does not
/// edit graph axes. It reads live state once per taken request.
type internal PersistThreadDeps = {
    currentState: unit -> State
    take: unit -> PersistRequest option
    persistNode: Graph -> NodeId -> Result<DateTime, string>
    postNotice: Notice -> unit
}

/// One long-lived persist thread. It takes a node id from the persist
/// request stack and writes that node from the live graph. It is not an
/// Actor and has no MailboxProcessor.
[<RequireQualifiedAccess>]
module PersistThread =

    /// True when that node is Unparsed.
    let internal unparsedNode (graph: Graph) (nodeId: NodeId) : bool =
        match Map.tryFind nodeId graph.nodes with
        | Some node when
            node.documentState.parseState = ParseState.Unparsed ->
            true
        | _ -> false

    let private runRequest (deps: PersistThreadDeps) (request: PersistRequest) =
        let state = deps.currentState ()
        let stillPersisted =
            CoreEventDispatch.persistedNode state.graph request.nodeId
        if not stillPersisted then
            let written =
                try
                    deps.persistNode state.graph request.nodeId
                with ex ->
                    Error ex.Message
            match written with
            | Ok updateTime ->
                deps.postNotice (
                    Notice.NodePersisted(
                        request.nodeId, state.eventId, updateTime))
            | Error message ->
                deps.postNotice (
                    Notice.PersistFailed(request.nodeId, message))

    let private loop (deps: PersistThreadDeps) =
        let rec run () =
            match deps.take () with
            | None -> ()
            | Some request ->
                try
                    runRequest deps request
                finally
                    ServerRunRegistry.notePersistFinished ()
                run ()
        run ()

    /// Hosts the consumer. The returned join waits until the thread exits.
    let internal start (deps: PersistThreadDeps) : unit -> unit =
        let thread = Thread(ThreadStart(fun () -> loop deps))
        thread.IsBackground <- true
        thread.Name <- "gambol-persist"
        thread.Start()
        let gate = obj ()
        let joined = ref false
        let join () =
            lock gate (fun () ->
                if not joined.Value then
                    joined.Value <- true
                    thread.Join())
        ServerRunRegistry.registerPersistJoin join
        join

    /// The slot is filled with CoreMailboxApi.postNotice.
    let internal postWhenBound
        (slot: (Notice -> unit) option ref)
        (notice: Notice)
        =
        match slot.Value with
        | None -> ()
        | Some post -> post notice
