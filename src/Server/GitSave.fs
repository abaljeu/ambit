namespace Gambol.Server

open System
open System.Diagnostics
open System.IO

[<RequireQualifiedAccess>]
module GitSave =

    let isRepo (dataDir: string) : bool =
        Directory.Exists(Path.Combine(dataDir, ".git"))

    let runGit (dataDir: string) (arguments: string) : Result<string, string> =
        try
            let psi =
                ProcessStartInfo(
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = dataDir,
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
