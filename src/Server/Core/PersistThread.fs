namespace Gambol.Server

open System.Threading
open Gambol.Shared

/// Dependencies for the long-lived persist thread. The thread does not
/// edit graph axes. finish is the private SnapshotDone add.
type internal PersistThreadDeps = {
    consumer: unit -> PersistWork
    persistOps:
        string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>
    persistChange:
        string -> Graph -> Graph -> Result<PersistGraphOk, string>
    finish: NodeId -> Graph option -> unit
}

/// One long-lived persist thread. It pulls collector jobs and calls the
/// existing persist functions. It is not an Actor and has no MailboxProcessor.
[<RequireQualifiedAccess>]
module PersistThread =

    let private reply (work: PersistWork) (outcome: PersistOutcome) =
        match work.reply with
        | None -> ()
        | Some send -> send outcome

    let private notify
        (deps: PersistThreadDeps)
        (submit: PersistSubmit)
        (graph: Graph option)
        (ids: NodeId list)
        =
        if submit.notify then
            ids |> List.iter (fun id -> deps.finish id graph)

    let private notifyIds (submit: PersistSubmit) : NodeId list =
        match submit.kind with
        | PersistKind.Ops ->
            submit.nodeIds
            |> List.filter (PersistCollectors.openNode submit.postGraph)
        | PersistKind.Change ->
            submit.nodeIds
            |> List.filter (fun id ->
                not (PersistCollectors.unparsedNode submit.postGraph id))

    /// A Change job writes the whole graph. One Unparsed submitted id
    /// blocks that write. The caller does not get SnapshotDone.
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
        (deps: PersistThreadDeps)
        (submit: PersistSubmit)
        (ops: Op list)
        : Result<PersistGraphOk, string> =
        match submit.dataDir with
        | None -> Ok { graph = submit.postGraph; message = None }
        | Some dataDir ->
            match submit.kind with
            | PersistKind.Ops ->
                deps.persistOps dataDir submit.preGraph submit.postGraph ops
            | PersistKind.Change ->
                deps.persistChange dataDir submit.preGraph submit.postGraph

    let private afterWrite
        (deps: PersistThreadDeps)
        (work: PersistWork)
        (ids: NodeId list)
        (written: Result<PersistGraphOk, string>)
        =
        let submit = work.submit
        match written with
        | Error err ->
            if submit.kind = PersistKind.Change then
                notify deps submit None ids
            reply work (PersistOutcome.Failed err)
        | Ok stamped ->
            let clean = stamped.message.IsNone
            let tell =
                submit.notify
                && (submit.kind = PersistKind.Change || clean)
            if tell then
                notify deps submit (Some stamped.graph) ids
            reply work (PersistOutcome.Wrote stamped)

    let private runWrite
        (deps: PersistThreadDeps)
        (work: PersistWork)
        (ops: Op list)
        (ids: NodeId list)
        =
        try
            afterWrite deps work ids (invoke deps work.submit ops)
        with ex ->
            reply work (PersistOutcome.Raised ex)

    let private runOne (deps: PersistThreadDeps) (work: PersistWork) =
        let ops =
            PersistCollectors.opsOpenForPersist work.submit.postGraph work.submit.ops
        if blocked work.submit ops then
            reply work PersistOutcome.Blocked
        else
            runWrite deps work ops (notifyIds work.submit)

    let private loop (deps: PersistThreadDeps) =
        while true do
            runOne deps (deps.consumer ())

    /// Hosts the perpetual consumer on a background thread.
    let internal start (deps: PersistThreadDeps) =
        let thread = Thread(ThreadStart(fun () -> loop deps))
        thread.IsBackground <- true
        thread.Start()

    /// Private add when the mailbox has bound the snapshot post.
    let internal finishWhenBound
        (slot: (InMsg -> unit) option ref)
        (nodeId: NodeId)
        (graph: Graph option)
        =
        match slot.Value with
        | None -> ()
        | Some post -> post (InMsg.SnapshotDone(nodeId, graph))
