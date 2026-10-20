namespace Gambol.Server

open System.Collections.Concurrent
open Gambol.Shared

/// Graph snapshot and node id at the moment of push.
type PersistRequest = {
    graph: Graph
    nodeId: NodeId
}

/// LIFO persist stack. Take blocks. Add does not.
[<RequireQualifiedAccess>]
module PersistStack =

    let create () : BlockingCollection<PersistRequest> =
        let stack = ConcurrentStack<PersistRequest>()
        new BlockingCollection<PersistRequest>(stack)

    let push
        (requests: BlockingCollection<PersistRequest>)
        (request: PersistRequest)
        =
        requests.Add request

    /// Parsed and Unpersisted.
    let openForPersist (graph: Graph) (nodeId: NodeId) =
        match Map.tryFind nodeId graph.nodes with
        | Some node when
            node.documentState.persistState = PersistState.Unpersisted
            && node.documentState.parseState = ParseState.Parsed ->
            true
        | _ -> false
