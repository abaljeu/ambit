namespace Gambol.Server

open System.IO

[<RequireQualifiedAccess>]
module WorkspaceGit =

    let isRepo (workspaceRoot: string) : bool =
        Directory.Exists(Path.Combine(workspaceRoot, ".git"))

    /// Ensure `workspaceRoot` is a git repo with denyNonFastForwards.
    /// Skips init when `.git` already exists. Creates the directory if needed.
    let ensureInit (workspaceRoot: string) : Result<unit, string> =
        let created =
            try
                Directory.CreateDirectory(workspaceRoot) |> ignore
                Ok ()
            with ex ->
                Error ex.Message

        match created with
        | Error err -> Error err
        | Ok () ->
            if isRepo workspaceRoot then
                Ok ()
            else
                match GitSave.runGit workspaceRoot "init" with
                | Error err -> Error err
                | Ok _ ->
                    match GitSave.runGit
                        workspaceRoot
                        "config receive.denyNonFastForwards true" with
                    | Error err -> Error err
                    | Ok _ -> Ok ()
