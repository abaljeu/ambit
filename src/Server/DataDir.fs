namespace Gambol.Server

open System.IO

[<RequireQualifiedAccess>]
module DataDir =

    let normalize (path: string) : string =
        let full = Path.GetFullPath path
        let sep = Path.DirectorySeparatorChar |> string
        if full.EndsWith sep then full
        else full + sep
