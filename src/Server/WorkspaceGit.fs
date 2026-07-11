namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

[<RequireQualifiedAccess>]
module WorkspaceGit =

    let isRepo (workspaceRoot: string) : bool =
        Directory.Exists(Path.Combine(workspaceRoot, ".git"))

    /// FF-only + allow push into non-bare checked-out branch.
    let ensurePushConfig (workspaceRoot: string) : Result<unit, string> =
        match GitSave.runGit
            workspaceRoot
            "config receive.denyNonFastForwards true" with
        | Error err -> Error err
        | Ok _ ->
            match GitSave.runGit
                workspaceRoot
                "config receive.denyCurrentBranch updateInstead" with
            | Error err -> Error err
            | Ok _ -> Ok ()

    /// Ensure `workspaceRoot` is a git repo with push policy.
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
            let initResult =
                if isRepo workspaceRoot then
                    Ok ()
                else
                    match GitSave.runGit workspaceRoot "init" with
                    | Error err -> Error err
                    | Ok _ -> Ok ()
            match initResult with
            | Error err -> Error err
            | Ok () -> ensurePushConfig workspaceRoot

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

    /// JIT commit when dirty (workspace-pull path only).
    let jitCommitIfDirty
        (workspaceRoot: string)
        (clientHint: string option)
        : Result<string, string> =
        match isDirty workspaceRoot with
        | Error err -> Error err
        | Ok false -> Ok "clean"
        | Ok true ->
            commitAll
                workspaceRoot
                "gambol: autosave before workspace-pull"
                clientHint

    /// Reject workspace-push when the server work tree is dirty.
    let assertCleanForWorkspacePush
        (workspaceRoot: string)
        : Result<unit, string> =
        match isDirty workspaceRoot with
        | Error err -> Error err
        | Ok true ->
            Error
                "server working tree dirty; workspace-pull or wait for autosave flush"
        | Ok false -> Ok ()
