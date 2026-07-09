namespace Gambol.Desktop

open System
open System.Diagnostics
open System.IO
open Gambol.Shared

[<RequireQualifiedAccess>]
module GitShell =

    let runGit (repoDir: string) (arguments: string) : Result<string, string> =
        try
            let psi =
                ProcessStartInfo(
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = repoDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false)

            use proc = Process.Start(psi)
            let stdout = proc.StandardOutput.ReadToEnd()
            let stderr = proc.StandardError.ReadToEnd()
            proc.WaitForExit()

            if proc.ExitCode = 0 then
                Ok(stdout.Trim())
            else
                let detail =
                    if String.IsNullOrWhiteSpace stderr then stdout.Trim()
                    else stderr.Trim()

                Error detail
        with ex ->
            Error ex.Message

    let private remoteExists (repoDir: string) (remoteName: string) =
        match runGit repoDir (sprintf "remote get-url %s" remoteName) with
        | Ok _ -> true
        | Error _ -> false

    let setupRemote (repoDir: string) (url: string) : Result<string, string> =
        let remote = WorkspaceGitRemote.remoteName
        let escapedUrl = url.Replace("\"", "\\\"")

        let args =
            if remoteExists repoDir remote then
                sprintf "remote set-url %s \"%s\"" remote escapedUrl
            else
                sprintf "remote add %s \"%s\"" remote escapedUrl

        runGit repoDir args

    let pull (repoDir: string) : Result<string list * string, string> =
        match runGit repoDir (sprintf "pull %s" WorkspaceGitRemote.remoteName) with
        | Error err -> Error err
        | Ok out ->
            let changed =
                match runGit repoDir "diff --name-only ORIG_HEAD HEAD" with
                | Ok paths when not (String.IsNullOrWhiteSpace paths) ->
                    paths.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
                    |> Array.toList
                | _ -> []

            Ok(changed, out)

    let push (repoDir: string) : Result<string, string> =
        runGit repoDir (sprintf "push %s" WorkspaceGitRemote.remoteName)
