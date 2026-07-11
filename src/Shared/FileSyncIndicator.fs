namespace Gambol.Shared

open System

/// Compare node `updateTime` to filesystem mtime for file-indicator labels.
[<RequireQualifiedAccess>]
module FileSyncIndicator =
    let labelForExistingFile (nodeUpdateTime: DateTime) (sourceModifiedUtc: DateTime) : string =
        let node = NodeUpdateTime.toDbPrecision nodeUpdateTime
        let source = NodeUpdateTime.toDbPrecision sourceModifiedUtc

        if node = source then
            "current"
        elif node < source then
            "old"
        else
            "edited"

    let indicatorTextForStatus
        (nodeUpdateTime: DateTime)
        (status: DesktopFileStatus)
        (sourceModifiedUtc: DateTime option)
        : string =
        match status with
        | InvalidPath -> "invalid"
        | CreateFile -> "create"
        | MissingArtifact -> "missing"
        | ExistingFolder -> NodeStatus.label status
        | ExistingFile ->
            match sourceModifiedUtc with
            | Some source -> labelForExistingFile nodeUpdateTime source
            | None -> ""
        | EvalError -> "error"
        | EvalOk -> "OK"
