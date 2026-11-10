namespace Gambol.Server

open System.Threading
open Gambol.Shared

[<RequireQualifiedAccess>]
module NodeEnclosureLock =

    type private Phase =
        | Pending
        | Held

    type private Grant = {
        id: int
        nodes: Set<NodeId>
    }

    type private Table = {
        phase: Map<NodeId, Phase>
        grants: Grant list
    }

    let private gate = obj ()

    let private table = ref { phase = Map.empty; grants = [] }

    let private nextGrantId = ref 0

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

    let private occupied (phase: Map<NodeId, Phase>) (nodeId: NodeId) =
        match Map.tryFind nodeId phase with
        | Some Pending
        | Some Held -> true
        | None -> false

    let private chainBlocked (nodes: NodeId list) (phase: Map<NodeId, Phase>) =
        List.exists (occupied phase) nodes

    let private grantTouches (nodeId: NodeId) (grants: Grant list) =
        List.exists (fun grant -> Set.contains nodeId grant.nodes) grants

    let private waitUntil (clear: Table -> bool) =
        let rec loop () =
            if clear table.Value then
                ()
            else
                Monitor.Wait gate |> ignore
                loop ()

        loop ()

    let private markPhase (nodeId: NodeId) (phase: Phase) =
        table.Value <-
            { table.Value with
                phase = Map.add nodeId phase table.Value.phase }

    let private clearPhase (nodeId: NodeId) =
        table.Value <-
            { table.Value with
                phase = Map.remove nodeId table.Value.phase }

    let private takeGrant (nodes: NodeId list) : int =
        lock gate (fun () ->
            waitUntil (fun state -> not (chainBlocked nodes state.phase))
            let id = nextGrantId.Value
            nextGrantId.Value <- id + 1
            let grant = { id = id; nodes = Set.ofList nodes }
            table.Value <-
                { table.Value with grants = grant :: table.Value.grants }
            id)

    let private dropGrant (grantId: int) =
        lock gate (fun () ->
            table.Value <-
                { table.Value with
                    grants =
                        table.Value.grants
                        |> List.filter (fun grant -> grant.id <> grantId) }
            Monitor.PulseAll gate)

    let private runGrant (nodes: NodeId list) (action: unit -> 'a) : 'a =
        let grantId = takeGrant nodes
        try
            action ()
        finally
            dropGrant grantId

    let withPersist
        (graph: Graph)
        (nodeId: NodeId)
        (action: NodeId list -> 'a)
        : 'a =
        let nodes = ownerChain graph nodeId
        runGrant nodes (fun () -> action nodes)

    let withParseRead
        (graph: Graph)
        (nodeId: NodeId)
        (action: unit -> 'a)
        : 'a =
        runGrant (ownerChain graph nodeId) action

    let withNodeLock (nodeId: NodeId) (action: unit -> 'a) : 'a =
        let marked = ref false
        try
            lock gate (fun () ->
                waitUntil (fun state -> not (occupied state.phase nodeId))
                markPhase nodeId Pending
                marked := true
                waitUntil (fun state ->
                    not (grantTouches nodeId state.grants))
                markPhase nodeId Held)
            action ()
        finally
            if marked.Value then
                lock gate (fun () ->
                    clearPhase nodeId
                    Monitor.PulseAll gate)
