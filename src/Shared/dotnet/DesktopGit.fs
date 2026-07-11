namespace Gambol.Shared

open System
open System.Diagnostics
open System.IO

/// Desktop-side stock `git` against the ambit gateway remote.
/// Shared by Desktop host and .NET tests (not Fable / Client).
[<RequireQualifiedAccess>]
module DesktopGit =

    let runGit (workingDir: string) (arguments: string) : Result<string, string> =
        try
            let psi =
                ProcessStartInfo(
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false)
            use proc = Process.Start(psi)
            let stdout = proc.StandardOutput.ReadToEnd()
            let stderr = proc.StandardError.ReadToEnd()
            proc.WaitForExit()
            if proc.ExitCode = 0 then Ok(stdout.Trim())
            else
                let detail =
                    if String.IsNullOrWhiteSpace stderr then stdout.Trim()
                    else stderr.Trim()
                Error detail
        with ex ->
            Error ex.Message

    let private runGitWithStdin
        (workingDir: string)
        (arguments: string)
        (stdinText: string)
        : Result<string, string> =
        try
            let psi =
                ProcessStartInfo(
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false)
            use proc = Process.Start(psi)
            proc.StandardInput.Write(stdinText)
            proc.StandardInput.Close()
            let stdout = proc.StandardOutput.ReadToEnd()
            let stderr = proc.StandardError.ReadToEnd()
            proc.WaitForExit()
            if proc.ExitCode = 0 then Ok(stdout.Trim())
            else
                let detail =
                    if String.IsNullOrWhiteSpace stderr then stdout.Trim()
                    else stderr.Trim()
                Error detail
        with ex ->
            Error ex.Message

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

    let pull (localPath: string) : Result<string, string> =
        if not (isRepo localPath) then
            Error "No git repository at local path."
        else
            match currentBranch localPath with
            | Error err -> Error err
            | Ok branch ->
                let args =
                    sprintf "pull %s %s" WorkspaceGitRemote.RemoteName branch
                runGit localPath args

    let push (localPath: string) : Result<string, string> =
        if not (isRepo localPath) then
            Error "No git repository at local path."
        else
            match currentBranch localPath with
            | Error err -> Error err
            | Ok branch ->
                let args =
                    sprintf "push %s %s" WorkspaceGitRemote.RemoteName branch
                runGit localPath args

    let status (localPath: string) : Result<WorkspaceGitStatus, string> =
        if not (isRepo localPath) then
            Error "No git repository at local path."
        else
            match runGit localPath "status -sb" with
            | Error err -> Error err
            | Ok text -> Ok(WorkspaceGitRemote.parseShortStatus text)

    /// Stock `git clone <remoteUrl> <localPath>`.
    let clone (remoteUrl: string) (localPath: string) : Result<string, string> =
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
            runGit parent args

    /// Store HTTPS PAT via `git credential approve` for the gateway host.
    let storeCredential
        (protocol: string)
        (host: string)
        (username: string)
        (password: string)
        : Result<unit, string> =
        let payload =
            sprintf
                "protocol=%s\nhost=%s\nusername=%s\npassword=%s\n\n"
                protocol
                host
                username
                password
        match runGitWithStdin (Path.GetTempPath()) "credential approve" payload with
        | Ok _ -> Ok ()
        | Error err -> Error err

    /// Host part of an ambit app base URL for credential storage.
    let hostFromAmbitBase (ambitBase: string) : Result<string, string> =
        try
            let uri = Uri(ambitBase)
            if String.IsNullOrEmpty uri.Host then
                Error "invalid ambit base URL"
            else
                Ok uri.Host
        with
        | :? UriFormatException -> Error "invalid ambit base URL"
        | :? ArgumentNullException -> Error "invalid ambit base URL"
