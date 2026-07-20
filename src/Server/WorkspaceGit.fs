namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

[<RequireQualifiedAccess>]
module WorkspaceGit =

    [<Literal>]
    let private reservedSystemFileExclude = "[gG][aA][mM][bB][oO][lL].*"

    let isRepo (workspaceRoot: string) : bool =
        Directory.Exists(Path.Combine(workspaceRoot, ".git"))

    [<Literal>]
    let private canonicalBranch = "master"

    /// Attached branch from `.git/HEAD` (e.g. `master`, not hard-coded `main`).
    let currentBranch (workspaceRoot: string) : Result<string, string> =
        if not (isRepo workspaceRoot) then
            Error "No git repository in workspace."
        else
            let headPath = Path.Combine(workspaceRoot, ".git", "HEAD")
            try
                if File.Exists headPath then
                    WorkspaceGitRemote.parseHeadRef (File.ReadAllText headPath)
                else
                    GitSave.runGit workspaceRoot "symbolic-ref --short HEAD"
            with ex ->
                Error ex.Message

    let private masterBranchExists (workspaceRoot: string) : bool =
        match
            GitSave.runGit
                workspaceRoot
                $"show-ref --verify --quiet refs/heads/{canonicalBranch}"
        with
        | Ok _ -> true
        | Error _ -> false

    /// Lone `main` from stock `git init` → `master`; skip when `master` exists.
    let private ensureCanonicalBranch (workspaceRoot: string) : Result<unit, string> =
        if not (isRepo workspaceRoot) then
            Ok ()
        else
            match currentBranch workspaceRoot with
            | Error _ -> Ok ()
            | Ok "main" ->
                if masterBranchExists workspaceRoot then
                    Ok ()
                else
                    GitSave.runGit workspaceRoot "branch -m main master"
                    |> Result.map ignore
            | Ok _ -> Ok ()

    let private ensureManagedExcludes (workspaceRoot: string) : Result<unit, string> =
        try
            let excludePath = Path.Combine(workspaceRoot, ".git", "info", "exclude")
            let text = if File.Exists excludePath then File.ReadAllText excludePath else ""
            let lines =
                text.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)

            if lines |> Array.contains reservedSystemFileExclude then
                Ok ()
            else
                Directory.CreateDirectory(Path.GetDirectoryName excludePath) |> ignore
                let separator =
                    if String.IsNullOrEmpty text
                       || text.EndsWith("\n", StringComparison.Ordinal) then ""
                    else Environment.NewLine
                File.AppendAllText(
                    excludePath,
                    separator + reservedSystemFileExclude + Environment.NewLine)
                Ok ()
        with ex ->
            Error ex.Message

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
                    match
                        GitSave.runGit
                            workspaceRoot
                            $"init -b {canonicalBranch}"
                    with
                    | Error err -> Error err
                    | Ok _ -> Ok ()
            match initResult with
            | Error err -> Error err
            | Ok () ->
                ensureCanonicalBranch workspaceRoot
                |> Result.bind (fun () -> ensureManagedExcludes workspaceRoot)
                |> Result.bind (fun () -> ensurePushConfig workspaceRoot)

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

    let tryHead (workspaceRoot: string) : Result<string option, string> =
        match GitSave.runGit workspaceRoot "rev-parse --verify HEAD" with
        | Ok oid -> Ok(Some oid)
        | Error headError ->
            match
                GitSave.runGit
                    workspaceRoot
                    "for-each-ref --format=%(objectname) refs/heads"
            with
            | Ok text ->
                match
                    text.Split(
                        [| '\r'; '\n' |],
                        StringSplitOptions.RemoveEmptyEntries)
                    |> Array.toList
                with
                | [] -> Ok None
                | [ oid ] -> Ok(Some oid)
                | _ -> Error headError
            | Error err -> Error err

    /// After first receive into an unborn repo, point symbolic HEAD at the
    /// sole refs/heads/* branch (client may push main while init used master).
    let alignHeadAfterUnbornReceive
        (workspaceRoot: string)
        : Result<unit, string> =
        match
            GitSave.runGit
                workspaceRoot
                "for-each-ref --format=%(refname:short) refs/heads"
        with
        | Error err -> Error err
        | Ok text ->
            match
                text.Split(
                    [| '\r'; '\n' |],
                    StringSplitOptions.RemoveEmptyEntries)
                |> Array.toList
            with
            | [ branch ] ->
                GitSave.runGit
                    workspaceRoot
                    $"symbolic-ref HEAD refs/heads/{branch}"
                |> Result.map ignore
            | _ -> Ok ()

    let private splitNulPaths (text: string) : string list =
        text.Split('\000', StringSplitOptions.RemoveEmptyEntries)
        |> Array.toList

    let parseChangedPaths
        (text: string)
        : Result<LazyLoadReconciliation.ChangedPath list, string> =
        let rec parse tokens changes =
            match tokens with
            | [] -> Ok(List.rev changes)
            | status :: path :: rest when status = "A" ->
                parse rest (LazyLoadReconciliation.Added path :: changes)
            | status :: path :: rest when status = "D" ->
                parse rest (LazyLoadReconciliation.Deleted path :: changes)
            | status :: path :: rest when status = "M" ->
                parse rest (LazyLoadReconciliation.Modified path :: changes)
            | status :: oldPath :: newPath :: rest when status.StartsWith("R") ->
                parse
                    rest
                    (LazyLoadReconciliation.Renamed(oldPath, newPath) :: changes)
            | status :: _ ->
                Error $"invalid or unsupported git name-status row '{status}'"

        text |> splitNulPaths |> fun tokens -> parse tokens []

    let changedPathsBetween
        (workspaceRoot: string)
        (oldHead: string option)
        (newHead: string)
        : Result<LazyLoadReconciliation.ChangedPath list, string> =
        let arguments =
            match oldHead with
            | None -> $"ls-tree -r --name-only -z {newHead}"
            | Some old when old = newHead -> ""
            | Some old -> $"diff --name-status -z -M --diff-filter=ADRM {old} {newHead}"
        if arguments = "" then
            Ok []
        else
            GitSave.runGit workspaceRoot arguments
            |> Result.bind (fun output ->
                match oldHead with
                | None ->
                    output
                    |> splitNulPaths
                    |> List.map LazyLoadReconciliation.Added
                    |> Ok
                | Some _ -> parseChangedPaths output)

    let addedPathsBetween
        (workspaceRoot: string)
        (oldHead: string option)
        (newHead: string)
        : Result<string list, string> =
        changedPathsBetween workspaceRoot oldHead newHead
        |> Result.map (List.choose (function
            | LazyLoadReconciliation.Added path -> Some path
            | _ -> None))
