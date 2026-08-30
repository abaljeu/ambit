namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

[<RequireQualifiedAccess>]
module GitSave =

    let isRepo (dataDir: string) : bool =
        Directory.Exists(Path.Combine(dataDir, ".git"))

    let runGit (dataDir: string) (arguments: string) : Result<string, string> =
        GitRun.gitExec dataDir arguments

    /// Nested repos cannot be added (unborn: "does not have a commit checked out").
    let private nestedRepoNames (root: string) : string list =
        try
            Directory.GetDirectories root
            |> Array.toList
            |> List.filter isRepo
            |> List.map Path.GetFileName
        with _ ->
            []

    let private addAllArguments (root: string) =
        match nestedRepoNames root with
        | [] -> "add -A"
        | names ->
            let excludes =
                names
                |> List.map (fun name -> sprintf "\":!%s\"" name)
                |> String.concat " "
            sprintf "add -A -- . %s" excludes

    let commitAll (dataDir: string) (message: string) : Result<string, string> =
        if not (isRepo dataDir) then
            Error "No git repository in data directory."
        else
            match runGit dataDir (addAllArguments dataDir) with
            | Error err -> Error err
            | Ok _ ->
                let args =
                    sprintf "-c user.email=gambol@save -c user.name=gambol commit -m \"%s\"" message
                match runGit dataDir args with
                | Ok out -> Ok out
                | Error err when err.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase) ->
                    Ok "nothing to commit"
                | Error err -> Error err
