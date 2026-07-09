namespace Gambol.Shared

type StatusKind =
    | StatusInfo
    | StatusWarn
    | StatusError

type StatusMessage =
    { text: string
      kind: StatusKind }

[<RequireQualifiedAccess>]
module StatusMessage =
    let info text = { text = text; kind = StatusInfo }
    let warn text = { text = text; kind = StatusWarn }
    let error text = { text = text; kind = StatusError }

    let fromDiagnostic (key: string) (operation: string) : StatusMessage option =
        if System.String.IsNullOrWhiteSpace key then
            None
        else
            Some(info ("<" + key + "> \u2192 " + operation))

    /// Higher-priority messages win; diagnostics are low priority.
    let chooseDisplay (status: StatusMessage option) (diagnostic: StatusMessage option) : StatusMessage option =
        match status with
        | Some _ -> status
        | None -> diagnostic
