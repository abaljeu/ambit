namespace Gambol.Server

open System.IO

[<RequireQualifiedAccess>]
module DataDir =

    let normalize (path: string) : string =
        let full = Path.GetFullPath path
        let sep = Path.DirectorySeparatorChar |> string
        if full.EndsWith sep then full
        else full + sep

    let resolvePath
        (contentRoot: string)
        (configured: string option)
        (onAzure: bool)
        (home: string)
        : string =
        let defaultRelative = if onAzure then "AppData" else "../../data"
        let relative = configured |> Option.defaultValue defaultRelative
        if Path.IsPathRooted(relative) then Path.GetFullPath relative
        elif onAzure then Path.Combine(home, relative) |> Path.GetFullPath
        else Path.Combine(contentRoot, relative) |> Path.GetFullPath

    let resolve
        (contentRoot: string)
        (configured: string option)
        (onAzure: bool)
        (home: string)
        : string =
        resolvePath contentRoot configured onAzure home
        |> fun path ->
            Directory.CreateDirectory(path) |> ignore
            normalize path
