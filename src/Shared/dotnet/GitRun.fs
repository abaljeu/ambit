namespace Gambol.Shared

open System
open System.Diagnostics
open System.IO

/// Sole entry point for invoking the `git` process from .NET hosts/tests.
[<RequireQualifiedAccess>]
module GitRun =

    let private combineOutput (stdout: string) (stderr: string) : string =
        let out = if isNull stdout then "" else stdout.Trim()
        let err = if isNull stderr then "" else stderr.Trim()
        if err = "" then out
        elif out = "" then err
        elif err.IndexOf(out, StringComparison.OrdinalIgnoreCase) >= 0 then
            err
        elif out.IndexOf(err, StringComparison.OrdinalIgnoreCase) >= 0 then
            out
        else
            out + Environment.NewLine + err

    /// Common case: run git in workDir. Ok trimmed stdout when exit 0.
    let gitExec (workDir: string) (arguments: string) : Result<string, string> =
        match
            ProcessExec.runCapture
                "git"
                arguments
                (ProcessExec.withWorkingDirectory workDir)
                None
        with
        | Ok(0, stdout, _) -> Ok(stdout.Trim())
        | Ok(_, stdout, stderr) -> Error(combineOutput stdout stderr)
        | Error e -> Error e

    /// Capture exit code + trimmed streams; configure owns cwd/args/env/stdin.
    let gitCapture
        (configure: ProcessStartInfo -> unit)
        (writeStdin: (StreamWriter -> unit) option)
        : Result<int * string * string, string> =
        match ProcessExec.runCapture "git" "" configure writeStdin with
        | Ok(code, stdout, stderr) ->
            Ok(code, stdout.Trim(), stderr.Trim())
        | Error e -> Error e

    /// Binary pack exchange: stdin bytes in, stdout bytes out on exit 0.
    let gitExchange
        (workDir: string)
        (arguments: string)
        (input: byte[])
        : Result<byte[], string> =
        try
            let configure (psi: ProcessStartInfo) =
                ProcessExec.withWorkingDirectory workDir psi
                ProcessExec.withStdinRedirect psi
                ProcessExec.withNoWindow psi
            use proc = ProcessExec.start "git" arguments configure
            if isNull proc then
                Error "failed to start git"
            else
                if input.Length > 0 then
                    proc.StandardInput.BaseStream.Write(
                        input, 0, input.Length)
                proc.StandardInput.Close()
                use ms = new MemoryStream()
                proc.StandardOutput.BaseStream.CopyTo(ms)
                let stderr = proc.StandardError.ReadToEnd()
                proc.WaitForExit()
                if proc.ExitCode = 0 then
                    Ok(ms.ToArray())
                else
                    let detail =
                        if String.IsNullOrWhiteSpace stderr then
                            "git failed"
                        else
                            stderr.Trim()
                    Error detail
        with ex ->
            Error ex.Message

    let isAvailable () : bool =
        match gitExec (Path.GetTempPath()) "--version" with
        | Ok _ -> true
        | Error _ -> false
