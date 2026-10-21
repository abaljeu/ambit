namespace Gambol.Server

open System
open System.Collections.Concurrent
open Gambol.Shared

/// The node that this push names.
type PersistRequest = {
    nodeId: NodeId
}

/// LIFO persist requests. take blocks until push.
[<RequireQualifiedAccess>]
module PersistRequestStack =

    /// Stop the stack. A blocked takeUntilStop then returns None.
    let stop (stack: BlockingCollection<PersistRequest>) =
        try
            stack.CompleteAdding()
        with :? InvalidOperationException -> ()

    /// Empty stack. The collection is this concrete request type.
    /// Stop is registered the same way the collector stack was.
    let create () : BlockingCollection<PersistRequest> =
        let store = ConcurrentStack<PersistRequest>()
        let stack = new BlockingCollection<PersistRequest>(store)
        ServerRunRegistry.registerPersistUnblock (fun () -> stop stack)
        stack

    /// Push this request. The newest request is the next take.
    let push
        (stack: BlockingCollection<PersistRequest>)
        (request: PersistRequest)
        =
        stack.Add request

    /// Block until a request is present, then return the newest.
    let take (stack: BlockingCollection<PersistRequest>) : PersistRequest =
        stack.Take ()

    /// Block until a request is present. None means the stack is stopped.
    let takeUntilStop
        (stack: BlockingCollection<PersistRequest>)
        : PersistRequest option =
        try
            Some(stack.Take())
        with :? InvalidOperationException ->
            None
