namespace Gambol.Shared

open System
open System.Collections.Concurrent
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

    let private workTreeGates =
        ConcurrentDictionary<string, obj>(
            StringComparer.OrdinalIgnoreCase)

    let private workTreeGate (workDir: string) =
        let key = Path.GetFullPath workDir
        workTreeGates.GetOrAdd(key, fun _ -> obj ())

    let private withWorkTreeGate (workDir: string) (action: unit -> 'a) : 'a =
        lock (workTreeGate workDir) action

    /// Common case: run git in workDir. Ok trimmed stdout when exit 0.
    let gitExec (workDir: string) (arguments: string) : Result<string, string> =
        withWorkTreeGate workDir (fun () ->
            match
                ProcessExec.runCapture
                    "git"
                    arguments
                    (ProcessExec.withWorkingDirectory workDir)
                    None
            with
            | Ok(0, stdout, _) -> Ok(stdout.Trim())
            | Ok(_, stdout, stderr) -> Error(combineOutput stdout stderr)
            | Error e -> Error e)

    /// Capture exit code + trimmed streams; configure owns cwd/args/env/stdin.
    let gitCapture
        (configure: ProcessStartInfo -> unit)
        (writeStdin: (StreamWriter -> unit) option)
        : Result<int * string * string, string> =
        match ProcessExec.runCapture "git" "" configure writeStdin with
        | Ok(code, stdout, stderr) ->
            Ok(code, stdout.Trim(), stderr.Trim())
        | Error e -> Error e

    let isAvailable () : bool =
        match gitExec (Path.GetTempPath()) "--version" with
        | Ok _ -> true
        | Error _ -> false
