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

    /// Drain stdout/stderr concurrently, then wait. Avoids pipe-buffer deadlock.
    let waitCaptureText (proc: Process) : int * string * string =
        let stdoutTask = proc.StandardOutput.ReadToEndAsync()
        let stderrTask = proc.StandardError.ReadToEndAsync()
        proc.WaitForExit()
        let stdout = stdoutTask.GetAwaiter().GetResult()
        let stderr = stderrTask.GetAwaiter().GetResult()
        proc.ExitCode, stdout, stderr

    /// Start, optionally write stdin text, capture stdout/stderr, wait.
    /// Stdout/stderr reads start before stdin write so large --stdin payloads
    /// cannot fill the OS pipe and deadlock (seen with git check-ignore).
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
                let stdoutTask = proc.StandardOutput.ReadToEndAsync()
                let stderrTask = proc.StandardError.ReadToEndAsync()

                match writeStdin with
                | Some write ->
                    write proc.StandardInput
                    proc.StandardInput.Close()
                | None -> ()

                proc.WaitForExit()
                let stdout = stdoutTask.GetAwaiter().GetResult()
                let stderr = stderrTask.GetAwaiter().GetResult()
                Ok(proc.ExitCode, stdout, stderr)
        with ex ->
            Error ex.Message
