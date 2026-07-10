namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

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

    /// `git status --porcelain` under the workspace root.
    let statusPorcelain
        (workspaceRoot: string)
        : Result<string, string> =
        if not (isRepo workspaceRoot) then
            Error "No git repository in workspace."
        else
            GitSave.runGit workspaceRoot "status --porcelain"

    /// True when porcelain output is non-empty.
    let isDirty (workspaceRoot: string) : Result<bool, string> =
        match statusPorcelain workspaceRoot with
        | Error err -> Error err
        | Ok text -> Ok(not (String.IsNullOrWhiteSpace text))

    /// `git add -A` + commit scoped to `workspaceRoot`.
    /// `clientHint` is folded into the commit message via
    /// `ClientIdentity.formatCommitMessage`.
    let commitAll
        (workspaceRoot: string)
        (baseMsg: string)
        (clientHint: string option)
        : Result<string, string> =
        let message =
            ClientIdentity.formatCommitMessage baseMsg clientHint
        GitSave.commitAll workspaceRoot message
