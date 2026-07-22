namespace Gambol.Shared

open System.Diagnostics
open System.IO

/// Shared ProcessStartInfo + Process.Start helpers for .NET hosts/tests.
[<RequireQualifiedAccess>]
module ProcessExec =

    let withWorkingDirectory (dir: string) (psi: ProcessStartInfo) =
        psi.WorkingDirectory <- dir

    let withStdinRedirect (psi: ProcessStartInfo) =
        psi.RedirectStandardInput <- true

    let withNoWindow (psi: ProcessStartInfo) =
        psi.CreateNoWindow <- true

    let withEnv (key: string) (value: string) (psi: ProcessStartInfo) =
        psi.Environment[key] <- value

    /// Builds a no-shell redirected ProcessStartInfo and calls Process.Start.
    let start
        (fileName: string)
        (arguments: string)
        (configure: ProcessStartInfo -> unit)
        : Process =
        let psi =
            ProcessStartInfo(
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false)
        configure psi
        Process.Start(psi)

    let waitCaptureText (proc: Process) : int * string * string =
        let stdout = proc.StandardOutput.ReadToEnd()
        let stderr = proc.StandardError.ReadToEnd()
        proc.WaitForExit()
        proc.ExitCode, stdout, stderr

    /// Start, optionally write stdin text, capture stdout/stderr, wait.
    let runCapture
        (fileName: string)
        (arguments: string)
        (configure: ProcessStartInfo -> unit)
        (writeStdin: (StreamWriter -> unit) option)
        : Result<int * string * string, string> =
        try
            use proc = start fileName arguments configure
            if isNull proc then
                Error "failed to start process"
            else
                match writeStdin with
                | Some write ->
                    write proc.StandardInput
                    proc.StandardInput.Close()
                | None -> ()
                Ok(waitCaptureText proc)
        with ex ->
            Error ex.Message
