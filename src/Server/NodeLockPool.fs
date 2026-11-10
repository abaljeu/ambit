namespace Gambol.Server

open System
open System.Threading
open Gambol.Shared

[<RequireQualifiedAccess>]
type NodeLockRejection =
    | HolderHoldsLock
    | NodeAbsent

[<RequireQualifiedAccess>]
module NodeLockPool =

    type private Phase =
        | Pending
        | Held

    type private Entry = {
        seq: int
        phase: Phase
        graph: Graph
        nodeId: NodeId
        threadId: int
    }

    type private Table = {
        nextSeq: int
        entries: Entry list
    }

    let private gate = obj ()

    let private table = ref { nextSeq = 0; entries = [] }

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

                match Map.tryFind current graph.nodes with
                | None -> List.rev acc
                | Some _ ->
                    match Map.tryFind current graph.ownerParentByChild with
                    | None -> List.rev acc
                    | Some parentId -> walk parentId visited acc

        walk startId Set.empty []

    let private kindOf (entry: Entry) : NodeKind =
        match Map.tryFind entry.nodeId entry.graph.nodes with
        | Some node -> node.kind
        | None -> Normal

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

    let private conflicts (request: Entry) (taken: Entry) =
        if request.seq = taken.seq then
            false
        else
            let kind = kindOf request
            let sameId = request.nodeId = taken.nodeId

            let ancestor =
                (not sameId)
                && waitsOnAncestor kind
                && chainReaches request.graph request.nodeId taken.nodeId

            let descendant =
                (not sameId)
                && NodeKind.container kind
                && chainReaches taken.graph taken.nodeId request.nodeId

            takenBlocks request taken && (sameId || ancestor || descendant)

    let private waiting (request: Entry) (entries: Entry list) =
        List.exists (conflicts request) entries

    let private waitForTurn (seq: int) =
        let rec loop () =
            let request =
                table.Value.entries
                |> List.tryFind (fun entry -> entry.seq = seq)

            match request with
            | None -> ()
            | Some request when waiting request table.Value.entries ->
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

    let private enqueue (graph: Graph) (nodeId: NodeId) (threadId: int) =
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
                graph = graph
                nodeId = nodeId
                threadId = threadId
            }

            table.Value <- {
                nextSeq = seq + 1
                entries = entry :: table.Value.entries
            }

            waitForTurn seq
            update seq Held
            Ok seq

    let private release (seq: int) =
        lock gate (fun () -> drop seq)

    let withLock
        (graph: Graph)
        (nodeId: NodeId)
        (action: unit -> 'a)
        : Result<'a, NodeLockRejection> =
        match Map.tryFind nodeId graph.nodes with
        | None -> Error NodeLockRejection.NodeAbsent
        | Some _ ->
            let threadId = Environment.CurrentManagedThreadId
            let queued = lock gate (fun () -> enqueue graph nodeId threadId)

            match queued with
            | Error rejection -> Error rejection
            | Ok seq ->
                try
                    Ok (action ())
                finally
                    release seq
