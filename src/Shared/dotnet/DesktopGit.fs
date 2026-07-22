namespace Gambol.Shared

open System
open System.Diagnostics
open System.IO

/// Desktop `git` binary helpers (ignore filtering / capability probe).
/// Shared by Desktop host and .NET tests (not Fable / Client).
[<RequireQualifiedAccess>]
module DesktopGit =

    let private combineGitOutput (stdout: string) (stderr: string) : string =
        let out = if isNull stdout then "" else stdout.Trim()
        let err = if isNull stderr then "" else stderr.Trim()
        if err = "" then out
        elif out = "" then err
        elif err.IndexOf(out, StringComparison.OrdinalIgnoreCase) >= 0 then err
        elif out.IndexOf(err, StringComparison.OrdinalIgnoreCase) >= 0 then out
        else out + Environment.NewLine + err

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
            else Error(combineGitOutput stdout stderr)
        with ex ->
            Error ex.Message

    let isAvailable () : bool =
        match runGit (Path.GetTempPath()) "--version" with
        | Ok _ -> true
        | Error _ -> false
