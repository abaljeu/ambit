namespace Gambol.Server

open System.Collections.Concurrent
open Gambol.Shared

/// Graph at the time of the push, and the node that push names.
type PersistRequest = {
    graph: Graph
    nodeId: NodeId
}

/// LIFO persist requests. take blocks until push.
[<RequireQualifiedAccess>]
module PersistRequestStack =

    /// Empty stack. The collection is this concrete request type.
    let create () : BlockingCollection<PersistRequest> =
        let store = ConcurrentStack<PersistRequest>()
        new BlockingCollection<PersistRequest>(store)

    /// Push this request. The newest request is the next take.
    let push
        (stack: BlockingCollection<PersistRequest>)
        (request: PersistRequest)
        =
        stack.Add request

    /// Block until a request is present, then return the newest.
    let take (stack: BlockingCollection<PersistRequest>) : PersistRequest =
        stack.Take ()
