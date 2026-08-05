namespace Gambol.Shared

[<RequireQualifiedAccess>]
module SyncBatch =
    /// Rewrite change ids into a contiguous delta chain from baseRevision.
    let toDeltaChain (baseRevision: int) (changes: Change list) : Change list =
        changes
        |> List.mapi (fun idx change ->
            { change with id = baseRevision + idx })

    let toActionDeltaChain
        (baseRevision: int)
        (actions: ChangeRequest list)
        : ChangeRequest list =
        actions
        |> List.mapi (fun index action ->
            ChangeRequest.withBaseRevision (baseRevision + index) action)
