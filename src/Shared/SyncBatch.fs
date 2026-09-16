namespace Gambol.Shared

[<RequireQualifiedAccess>]
module SyncBatch =
    /// Rewrite event ids into a contiguous delta chain from baseRevision.
    let toDeltaChain (baseRevision: int) (events: Gambol.Shared.Events.Event list) : Gambol.Shared.Events.Event list =
        events
        |> List.mapi (fun idx event ->
            { event with id = Gambol.Shared.Events.EventId (baseRevision + idx) })

    let toPendingDeltaChain
        (baseRevision: int)
        (items: PendingChange list)
        : PendingChange list =
        items
        |> List.mapi (fun index item ->
            { item with
                event = { item.event with 
                            id = Gambol.Shared.Events.EventId 
                                        (baseRevision + index) 
                        } } )

    let toWireBatch
        (baseRevision: int)
        (items: PendingChange list)
        : Gambol.Shared.Events.Event list =
        toPendingDeltaChain baseRevision items
        |> List.map (fun item -> item.event)
