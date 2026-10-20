namespace Gambol.Server

open System.Threading
open Gambol.Shared

/// Write body and finish hook for one persist thread.
type internal PersistWrite = {
    persistOps:
        string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>
    persistChange:
        string -> Graph -> Graph -> Result<PersistGraphOk, string>
    finish: NodeId -> unit
    changeFailed: NodeId list -> string -> unit
}

/// Dependencies for the long-lived persist thread. The thread does not
/// edit graph axes. finish reports one node whose file write succeeded.
type internal PersistThreadDeps = {
    consumer: unit -> PersistWork
    write: PersistWrite
}

/// One node pulled from the persist stack, plus the write body.
type internal PersistNodePull = {
    take: unit -> NodeId
    dataDir: string option
    graph: unit -> Graph
    write: PersistWrite
}

/// One long-lived persist thread. It pulls collector jobs and calls the
/// existing persist functions. It is not an Actor and has no MailboxProcessor.
[<RequireQualifiedAccess>]
module PersistThread =

    let private reply (work: PersistWork) (outcome: PersistOutcome) =
        match work.reply with
        | None -> ()
        | Some send -> send outcome

    let private notify (write: PersistWrite) (ids: NodeId list) =
        ids |> List.iter write.finish

    let private notifyIds (submit: PersistSubmit) : NodeId list =
        match submit.kind with
        | PersistKind.Ops ->
            submit.nodeIds
            |> List.filter (PersistCollectors.openNode submit.postGraph)
        | PersistKind.Change ->
            submit.nodeIds
            |> List.filter (fun id ->
                not (PersistCollectors.unparsedNode submit.postGraph id))

    /// A Change job writes differing document files. One Unparsed
    /// submitted id blocks that write. The caller does not get NodePersisted.
    let private changeBlocked (submit: PersistSubmit) =
        submit.kind = PersistKind.Change
        && submit.nodeIds
           |> List.exists (
               PersistCollectors.unparsedNode submit.postGraph)

    let private blocked (submit: PersistSubmit) (ops: Op list) : bool =
        let droppedOps =
            submit.kind = PersistKind.Ops
            && not (List.isEmpty submit.ops)
            && List.isEmpty ops
        droppedOps || changeBlocked submit

    let private invoke
        (write: PersistWrite)
        (submit: PersistSubmit)
        (ops: Op list)
        : Result<PersistGraphOk, string> =
        match submit.dataDir with
        | None -> Ok { graph = submit.postGraph; message = None }
        | Some dataDir ->
            match submit.kind with
            | PersistKind.Ops ->
                write.persistOps dataDir submit.preGraph submit.postGraph ops
            | PersistKind.Change ->
                write.persistChange dataDir submit.preGraph submit.postGraph

    let private afterWrite
        (write: PersistWrite)
        (work: PersistWork)
        (ids: NodeId list)
        (written: Result<PersistGraphOk, string>)
        =
        let submit = work.submit
        match written with
        | Error err ->
            if submit.kind = PersistKind.Change then
                write.changeFailed submit.nodeIds err
            reply work (PersistOutcome.Failed err)
        | Ok stamped ->
            let clean = stamped.message.IsNone
            let tell =
                submit.notify
                && (submit.kind = PersistKind.Change || clean)
            if tell then
                notify write ids
            reply work (PersistOutcome.Wrote stamped)

    let private runWrite
        (write: PersistWrite)
        (work: PersistWork)
        (ops: Op list)
        (ids: NodeId list)
        =
        let written =
            try
                invoke write work.submit ops
            with ex ->
                Error ex.Message
        afterWrite write work ids written

    let private runOne (write: PersistWrite) (work: PersistWork) =
        let ops =
            PersistCollectors.opsOpenForPersist work.submit.postGraph work.submit.ops
        if blocked work.submit ops then
            reply work PersistOutcome.Blocked
        else
            runWrite write work ops (notifyIds work.submit)

    let private loop (deps: PersistThreadDeps) =
        while true do
            runOne deps.write (deps.consumer ())

    /// Hosts the perpetual consumer on a background thread.
    let internal start (deps: PersistThreadDeps) =
        let thread = Thread(ThreadStart(fun () -> loop deps))
        thread.IsBackground <- true
        thread.Start()

    /// Pulls one node and calls the Change write. Does not edit the graph.
    let internal pullOne (pull: PersistNodePull) =
        let nodeId = pull.take ()
        let graph = pull.graph ()
        runOne pull.write {
            submit = {
                nodeIds = [ nodeId ]
                dataDir = pull.dataDir
                preGraph = graph
                postGraph = graph
                ops = []
                kind = PersistKind.Change
                notify = true
                wait = false
            }
            reply = None
        }

    let private loopNodes (pull: PersistNodePull) =
        while true do
            pullOne pull

    /// Hosts the node-stack consumer on a background thread.
    let internal startNodeStack (pull: PersistNodePull) =
        let thread = Thread(ThreadStart(fun () -> loopNodes pull))
        thread.IsBackground <- true
        thread.Start()

    /// Private add after a successful finish. A failed write does not call this.
    let internal finishWhenBound
        (slot: (Notice -> unit) option ref)
        (nodeId: NodeId)
        =
        match slot.Value with
        | None -> ()
        | Some post -> post (Notice.NodePersisted nodeId)
