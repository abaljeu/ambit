namespace Gambol.Shared

[<RequireQualifiedAccess>]
type LoadSavePath =
    | Git
    | Desk

[<RequireQualifiedAccess>]
type LoadSavePrePick =
    | Plain
    | Git
    | Desk

[<RequireQualifiedAccess>]
module PathPick =

    let choose (remoteExists: bool) : LoadSavePath =
        if remoteExists then
            LoadSavePath.Git
        else
            LoadSavePath.Desk

    let resolve
        (prePick: LoadSavePrePick)
        (remoteExists: unit -> Result<bool, string>)
        : Result<LoadSavePath, string> =
        match prePick with
        | LoadSavePrePick.Plain ->
            remoteExists () |> Result.map choose
        | LoadSavePrePick.Git -> Ok LoadSavePath.Git
        | LoadSavePrePick.Desk -> Ok LoadSavePath.Desk
