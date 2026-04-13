namespace Gambol.Server

open System

[<RequireQualifiedAccess>]
module HeadPresence =

    let hasHeadFromUserInteractive (isUserInteractive: bool) : bool =
        isUserInteractive

    let detectHasHead () : bool =
        Environment.UserInteractive |> hasHeadFromUserInteractive
