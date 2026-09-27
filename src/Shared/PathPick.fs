namespace Gambol.Shared

[<RequireQualifiedAccess>]
type LoadSavePath =
    | Git
    | Desk

[<RequireQualifiedAccess>]
module PathPick =

    let choose (remoteExists: bool) : LoadSavePath =
        if remoteExists then
            LoadSavePath.Git
        else
            LoadSavePath.Desk
