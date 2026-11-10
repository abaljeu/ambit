namespace Gambol.Server

open System
open System.Threading
open System.Threading.Tasks
open Gambol.Shared

[<RequireQualifiedAccess>]
type NodeLockRejection =
    | HolderHoldsLock

[<RequireQualifiedAccess>]
module NodeLockPool =

    type private Phase =
        | Pending
        | Held

    type private Entry = {
        seq: int
        phase: Phase
        nodeId: NodeId
        threadId: int
    }

    type private Table = {
        nextSeq: int
        entries: Entry list
    }

    type private QueueWait = {
        count: int
        ready: TaskCompletionSource<unit>
    }

    let private gate = obj ()

    let private table = ref { nextSeq = 0; entries = [] }

    let private queueWaits = ref ([]: QueueWait list)

    let private entryTotal () = List.length table.Value.entries

    let private signalQueued () =
        let count = entryTotal ()

        let ready, waiting =
            queueWaits.Value
            |> List.partition (fun wait -> count >= wait.count)

        queueWaits.Value <- waiting
        ready |> List.iter (fun wait -> wait.ready.TrySetResult() |> ignore)

    let private countPending () =
        table.Value.entries
        |> List.filter (fun entry -> entry.phase = Pending)
        |> List.length

    let internal entryCount () =
        lock gate (fun () -> entryTotal ())

    /// True while a request for this node is Held. Does not take a lock.
    let internal nodeIsHeld (nodeId: NodeId) =
        lock gate (fun () ->
            table.Value.entries
            |> List.exists (fun entry ->
                entry.nodeId = nodeId && entry.phase = Held))

    let internal pendingCount () =
        lock gate (fun () -> countPending ())

    let internal waitUntilQueued (count: int) =
        lock gate (fun () ->
            if entryTotal () >= count then
                Task.CompletedTask
            else
                let ready =
                    TaskCompletionSource<unit>(
                        TaskCreationOptions.RunContinuationsAsynchronously)

                queueWaits.Value <-
                    { count = count; ready = ready } :: queueWaits.Value

                ready.Task)

    let private ownerChain (graph: Graph) (startId: NodeId) : NodeId list =
        let rec walk
            (current: NodeId)
            (visited: Set<NodeId>)
            (acc: NodeId list)
            =
            if Set.contains current visited then
                List.rev acc
            else
                let visited = Set.add current visited
                let acc = current :: acc

                match Map.tryFind current graph.ownerParentByChild with
                | None -> List.rev acc
                | Some parentId -> walk parentId visited acc

        walk startId Set.empty []

    let private waitsOnAncestor (kind: NodeKind) =
        match kind with
        | Special Directory
        | Special File -> true
        | Special Workspaces
        | Special Workspace
        | Normal -> false

    let private chainReaches
        (graph: Graph)
        (startId: NodeId)
        (targetId: NodeId)
        =
        ownerChain graph startId
        |> List.exists (fun candidate -> candidate = targetId)

    let private takenBlocks (request: Entry) (taken: Entry) =
        match taken.phase with
        | Held -> true
        | Pending -> taken.seq < request.seq

    let private conflicts
        (graph: Graph)
        (kind: NodeKind)
        (request: Entry)
        (taken: Entry)
        =
        if request.seq = taken.seq then
            false
        elif not (takenBlocks request taken) then
            false
        elif request.nodeId = taken.nodeId then
            true
        else
            let ancestor =
                waitsOnAncestor kind
                && chainReaches graph request.nodeId taken.nodeId

            let descendant =
                NodeKind.container kind
                && chainReaches graph taken.nodeId request.nodeId

            ancestor || descendant

    let private waiting
        (graph: Graph)
        (kind: NodeKind)
        (request: Entry)
        (entries: Entry list)
        =
        List.exists (conflicts graph kind request) entries

    let private waitForTurn (graph: Graph) (kind: NodeKind) (seq: int) =
        let rec loop () =
            let request =
                table.Value.entries
                |> List.tryFind (fun entry -> entry.seq = seq)

            match request with
            | None -> ()
            | Some request when waiting graph kind request table.Value.entries ->
                Monitor.Wait gate |> ignore
                loop ()
            | Some _ -> ()

        loop ()

    let private update (seq: int) (phase: Phase) =
        let entries =
            table.Value.entries
            |> List.map (fun entry ->
                if entry.seq = seq then
                    { entry with phase = phase }
                else
                    entry)

        table.Value <- { table.Value with entries = entries }

    let private drop (seq: int) =
        let entries =
            table.Value.entries
            |> List.filter (fun entry -> entry.seq <> seq)

        table.Value <- { table.Value with entries = entries }
        Monitor.PulseAll gate

    let private enqueue
        (graph: Graph)
        (kind: NodeKind)
        (nodeId: NodeId)
        (threadId: int)
        =
        let threadHolds =
            table.Value.entries
            |> List.exists (fun entry -> entry.threadId = threadId)

        if threadHolds then
            Error NodeLockRejection.HolderHoldsLock
        else
            let seq = table.Value.nextSeq

            let entry = {
                seq = seq
                phase = Pending
                nodeId = nodeId
                threadId = threadId
            }

            table.Value <- {
                nextSeq = seq + 1
                entries = entry :: table.Value.entries
            }

            try
                signalQueued ()
                waitForTurn graph kind seq
                update seq Held
                Ok seq
            with _ ->
                drop seq
                reraise ()

    let private release (seq: int) =
        lock gate (fun () -> drop seq)

    let withLock
        (graph: Graph)
        (nodeId: NodeId)
        (action: unit -> 'a)
        : Result<'a, NodeLockRejection> =
        match Map.tryFind nodeId graph.nodes with
        | None -> Ok (action ())
        | Some node ->
            let threadId = Environment.CurrentManagedThreadId

            let queued =
                lock gate (fun () ->
                    enqueue graph node.kind nodeId threadId)

            match queued with
            | Error rejection -> Error rejection
            | Ok seq ->
                try
                    Ok (action ())
                finally
                    release seq

    let internal withLockResult
        (graph: Graph)
        (nodeId: NodeId)
        (action: unit -> Result<'value, string>)
        : Result<'value, string> =
        match withLock graph nodeId action with
        | Error NodeLockRejection.HolderHoldsLock ->
            Error "holder already holds a lock"
        | Ok result -> result
