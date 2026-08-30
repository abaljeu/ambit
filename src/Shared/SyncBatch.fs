namespace Gambol.Shared

[<RequireQualifiedAccess>]
module SyncBatch =
    /// Rewrite change ids into a contiguous delta chain from baseRevision.
    let toDeltaChain (baseRevision: int) (changes: Change list) : Change list =
        changes
        |> List.mapi (fun idx change ->
            { change with id = baseRevision + idx })

    let toPendingDeltaChain
        (baseRevision: int)
        (items: PendingChange list)
        : PendingChange list =
        items
        |> List.mapi (fun index item ->
            { item with change = { item.change with id = baseRevision + index } })

    let toWireBatch
        (baseRevision: int)
        (items: PendingChange list)
        : Change list =
        toPendingDeltaChain baseRevision items
        |> List.map (fun item -> item.change)
