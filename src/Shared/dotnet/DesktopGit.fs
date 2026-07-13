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

    /// Set or update remote `ambit` to `remoteUrl` in `localPath`.
    let setAmbitRemote
        (localPath: string)
        (remoteUrl: string)
        : Result<unit, string> =
        if not (isRepo localPath) then
            Error "No git repository at local path."
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

    let private currentBranch (localPath: string) : Result<string, string> =
        runGit localPath "rev-parse --abbrev-ref HEAD"

    let pull
        (localPath: string)
        (auth: (string * string) option)
        : Result<string, string> =
        if not (isRepo localPath) then
            Error "No git repository at local path."
        else
            match currentBranch localPath with
            | Error err -> Error err
            | Ok branch ->
                let args =
                    sprintf "pull %s %s" WorkspaceGitRemote.RemoteName branch
                runGitCore localPath args auth

    let push
        (localPath: string)
        (auth: (string * string) option)
        : Result<string, string> =
        if not (isRepo localPath) then
            Error "No git repository at local path."
        else
            match currentBranch localPath with
            | Error err -> Error err
            | Ok branch ->
                let args =
                    sprintf "push %s %s" WorkspaceGitRemote.RemoteName branch
                runGitCore localPath args auth

    let status (localPath: string) : Result<WorkspaceGitStatus, string> =
        if not (isRepo localPath) then
            Error "No git repository at local path."
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
