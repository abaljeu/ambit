namespace Gambol.Shared

/// Split graph-only ops so each DbAgent applyBatch stays under 8s.
[<RequireQualifiedAccess>]
module GraphOnlyChangeChunks =

    /// Max ops per postGraphOnlyChange. DbAgent wraps apply in 8000ms.
    [<Literal>]
    let maxOps = 80

    let split (ops: Op list) : Op list list =
        match ops with
        | [] -> []
        | _ -> List.chunkBySize maxOps ops
