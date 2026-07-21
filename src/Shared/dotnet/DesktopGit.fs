namespace Gambol.Shared

open System
open System.Diagnostics
open System.IO
open System.Text

/// Desktop-side stock `git` against the ambit gateway remote.
/// Shared by Desktop host and .NET tests (not Fable / Client).
/// Auth: Ambit git PAT via http.extraHeader; credential.helper cleared
/// so Git Credential Manager is not the user-facing auth path.
[<RequireQualifiedAccess>]
module DesktopGit =

    /// Drop GCM's expected localhost-HTTP noise; keep fatal/auth lines.
    let filterGitErrorDetail (detail: string) : string =
        if String.IsNullOrWhiteSpace detail then detail
        else
            detail.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.filter (fun line ->
                line.IndexOf(
                    "use of unencrypted HTTP remote URLs is not recommended",
                    StringComparison.OrdinalIgnoreCase) < 0)
            |> String.concat "\n"
            |> fun s -> s.Trim()

    let private errorDetail (stdout: string) (stderr: string) : string =
        let raw =
            if String.IsNullOrWhiteSpace stderr then stdout.Trim()
            else stderr.Trim()
        filterGitErrorDetail raw

    /// HTTP Basic value (`Basic <b64>`) for Ambit git PAT.
    let basicAuthHeaderValue (username: string) (password: string) =
        let raw = sprintf "%s:%s" username password
        let b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))
        "Basic " + b64

    /// GIT_CONFIG_* pairs: clear helper; optional Authorization header.
    let gitAuthConfigPairs
        (auth: (string * string) option)
        : (string * string) array =
        let clearHelper = "credential.helper", ""
        match auth with
        | None -> [| clearHelper |]
        | Some(user, token) ->
            let header =
                "Authorization: " + basicAuthHeaderValue user token
            [| clearHelper; "http.extraHeader", header |]

    let private applyAuthEnv
        (psi: ProcessStartInfo)
        (auth: (string * string) option)
        =
        let pairs = gitAuthConfigPairs auth
        psi.Environment["GIT_CONFIG_COUNT"] <- string pairs.Length
        pairs
        |> Array.iteri (fun i (key, value) ->
            psi.Environment[$"GIT_CONFIG_KEY_{i}"] <- key
            psi.Environment[$"GIT_CONFIG_VALUE_{i}"] <- value)

    let private runGitCore
        (workingDir: string)
        (arguments: string)
        (auth: (string * string) option)
        : Result<string, string> =
        try
            let psi =
                ProcessStartInfo(
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false)
            applyAuthEnv psi auth
            use proc = Process.Start(psi)
            let stdout = proc.StandardOutput.ReadToEnd()
            let stderr = proc.StandardError.ReadToEnd()
            proc.WaitForExit()
            if proc.ExitCode = 0 then Ok(stdout.Trim())
            else Error(errorDetail stdout stderr)
        with ex ->
            Error ex.Message

    let runGit (workingDir: string) (arguments: string) : Result<string, string> =
        runGitCore workingDir arguments None

    let isAvailable () : bool =
        match runGit (Path.GetTempPath()) "--version" with
        | Ok _ -> true
        | Error _ -> false

    let isRepo (localPath: string) : bool =
        Directory.Exists(Path.Combine(localPath, ".git"))

    let noRepoError localPath =
        Error ("No git repository at " + localPath)

    /// Set or update remote `ambit` to `remoteUrl` in `localPath`.
    let setAmbitRemote
        (localPath: string)
        (remoteUrl: string)
        : Result<unit, string> =
        if not (isRepo localPath) then
            noRepoError localPath
        else
            let name = WorkspaceGitRemote.RemoteName
            let addArgs = sprintf "remote add %s \"%s\"" name remoteUrl
            match runGit localPath addArgs with
            | Ok _ -> Ok ()
            | Error _ ->
                let setArgs =
                    sprintf "remote set-url %s \"%s\"" name remoteUrl
                match runGit localPath setArgs with
                | Ok _ -> Ok ()
                | Error err -> Error err

    /// `setAmbitRemote` using label + app base (`…/ambit`).
    let setAmbitRemoteForLabel
        (localPath: string)
        (label: string)
        (ambitBase: string)
        : Result<unit, string> =
        let url = WorkspaceGitRemote.remoteUrl ambitBase label
        setAmbitRemote localPath url

    let parseRemoteHeadBranch (text: string) : Result<string, string> =
        let prefix = "ref: refs/heads/"
        let branch =
            text.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.tryPick (fun line ->
                let fields = line.Split('\t')
                if fields.Length = 2
                   && fields.[1] = "HEAD"
                   && fields.[0].StartsWith(prefix, StringComparison.Ordinal) then
                    Some(fields.[0].Substring(prefix.Length))
                else
                    None)
        match branch with
        | Some value when value.Length > 0 -> Ok value
        | _ -> Error "Ambit remote HEAD does not identify a branch."

    let private requireAttachedHead
        (localPath: string)
        : Result<string, string> =
        match runGit localPath "symbolic-ref --quiet --short HEAD" with
        | Ok branch when not (String.IsNullOrWhiteSpace branch) -> Ok branch
        | _ -> Error "Cannot push or pull from detached HEAD."

    let pullArguments (branch: string) : string =
        sprintf
            "pull %s refs/heads/%s"
            WorkspaceGitRemote.RemoteName
            branch

    let pushArguments (branch: string) : string =
        sprintf
            "push %s HEAD:refs/heads/%s"
            WorkspaceGitRemote.RemoteName
            branch

    /// Well-known empty tree. `-c attr.tree=<this>` ignores worktree
    /// gitattributes for one invocation — clears false dirt when index
    /// blobs have CRLF but `* text eol=lf` marks the WT modified.
    [<Literal>]
    let emptyAttrTree = "4b825dc642cb6eb9a060e54bf8d0927e237e9347"

    let isOverwrittenByMergeError (detail: string) : bool =
        detail.IndexOf(
            "would be overwritten by merge",
            StringComparison.OrdinalIgnoreCase)
        >= 0

    let isNonFastForwardPushError (detail: string) : bool =
        detail.IndexOf("non-fast-forward", StringComparison.OrdinalIgnoreCase) >= 0
        || detail.IndexOf("fetch first", StringComparison.OrdinalIgnoreCase) >= 0
        || detail.IndexOf("failed to push some refs", StringComparison.OrdinalIgnoreCase) >= 0

    let pullArgumentsIgnoringAttrs (branch: string) : string =
        sprintf "-c attr.tree=%s %s" emptyAttrTree (pullArguments branch)

    let private remoteHeadBranch
        (localPath: string)
        (auth: (string * string) option)
        : Result<string, string> =
        let name = WorkspaceGitRemote.RemoteName
        let args = sprintf "ls-remote --symref %s HEAD" name
        match runGitCore localPath args auth with
        | Error err -> Error err
        | Ok text -> parseRemoteHeadBranch text

    let gitPull
        (localPath: string)
        (auth: (string * string) option)
        : Result<string, string> =
        if not (isRepo localPath) then
            noRepoError localPath
        else
            match requireAttachedHead localPath with
            | Error err -> Error err
            | Ok _ ->
                match remoteHeadBranch localPath auth with
                | Error err -> Error err
                | Ok branch ->
                    match runGitCore localPath (pullArguments branch) auth with
                    | Ok out -> Ok out
                    | Error err when isOverwrittenByMergeError err ->
                        // Retry without gitattributes so eol=lf false dirt
                        // (CRLF blobs vs LF attribute) does not block merge.
                        // Real content edits still abort on the retry.
                        runGitCore
                            localPath
                            (pullArgumentsIgnoringAttrs branch)
                            auth
                    | Error err -> Error err

    
    let push
        (localPath: string)
        (auth: (string * string) option)
        : Result<string, string> =
        if not (isRepo localPath) then
            noRepoError localPath
        else
            match requireAttachedHead localPath with
            | Error err -> Error err
            | Ok branch ->
                match runGitCore localPath (pushArguments branch) auth with
                | Ok out -> Ok out
                | Error pushErr when isNonFastForwardPushError pushErr ->
                    match gitPull localPath auth with
                    | Error pullErr ->
                        Error(
                            pushErr
                            + Environment.NewLine
                            + "auto-pull before retry failed: "
                            + pullErr)
                    | Ok _ -> runGitCore localPath (pushArguments branch) auth
                | Error pushErr -> Error pushErr

    let status (localPath: string) : Result<WorkspaceGitStatus, string> =
        if not (isRepo localPath) then
            noRepoError localPath
        else
            match runGit localPath "status -sb" with
            | Error err -> Error err
            | Ok text -> Ok(WorkspaceGitRemote.parseShortStatus text)

    /// Stock `git clone <remoteUrl> <localPath>` with optional Ambit auth.
    let clone
        (remoteUrl: string)
        (localPath: string)
        (auth: (string * string) option)
        : Result<string, string> =
        let parent =
            match Path.GetDirectoryName localPath with
            | null | "" -> Path.GetTempPath()
            | p -> p
        let created =
            try
                Directory.CreateDirectory(parent) |> ignore
                Ok ()
            with ex ->
                Error ex.Message
        match created with
        | Error err -> Error err
        | Ok () ->
            let args = sprintf "clone \"%s\" \"%s\"" remoteUrl localPath
            runGitCore parent args auth
