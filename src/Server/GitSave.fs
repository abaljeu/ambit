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

    let commitAll (dataDir: string) (message: string) : Result<string, string> =
        if not (isRepo dataDir) then
            Error "No git repository in data directory."
        else
            match runGit dataDir "add -A" with
            | Error err -> Error err
            | Ok _ ->
                let args =
                    sprintf "-c user.email=gambol@save -c user.name=gambol commit -m \"%s\"" message
                match runGit dataDir args with
                | Ok out -> Ok out
                | Error err when err.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase) ->
                    Ok "nothing to commit"
                | Error err -> Error err
