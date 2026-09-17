namespace Gambol.Shared

/// Copy for the ServerRejected blocking overlay. Honest: server string or short fallback.
module SyncRiskAlert =
    let usableErrorMessage last =
        match last with
        | Some (CmdLastResult.Error (_, msg)) ->
            match msg.Trim() with
            | "" -> None
            | trimmed -> Some trimmed
        | _ -> None

    let serverRejectedAlertText last =
        let cause =
            usableErrorMessage last
            |> Option.defaultValue "The server rejected the change."
        cause
        + " Reload the page to resync. Your unsaved changes will be lost."
