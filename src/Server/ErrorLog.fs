namespace Gambol.Server

open System
open System.IO
open System.Text

/// Append-only, timestamped error log at `<dataDir>/SYSTEM/errors.log`. Unlike
/// `HttpResponseLog`'s `http-responses.log`, this file is never wiped on process
/// start: nothing here calls anything like `HttpResponseLog.prepareFresh`.
[<RequireQualifiedAccess>]
module ErrorLog =

    let private writeGate = obj ()

    let logPath (dataDir: string) =
        Path.Combine(Bookkeeping.systemDir dataDir, "errors.log")

    let private oneLine (text: string) =
        if isNull text then ""
        else text.Replace("\r\n", "\\n").Replace("\n", "\\n")

    let private appendLine (logFile: string) (line: string) =
        lock writeGate (fun () ->
            let dir = Path.GetDirectoryName(logFile)
            if not (String.IsNullOrEmpty dir) then
                Directory.CreateDirectory(dir) |> ignore
            use stream =
                new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.Read)
            use writer = new StreamWriter(stream, UTF8Encoding(false), 1024, true)
            writer.WriteLine(line)
            writer.Flush()
            stream.Flush(true))

    let private tryAppendLine (logFile: string) (line: string) =
        if not (String.IsNullOrEmpty logFile) then
            try appendLine logFile line with _ -> ()

    let private formatLine (utc: DateTime) (kind: string) (detail: string) =
        sprintf "%s %s %s" (utc.ToString("o")) kind (oneLine detail)

    /// Append one timestamped entry. Best-effort: never throws, never blocks startup.
    let append (dataDir: string) (kind: string) (detail: string) : unit =
        tryAppendLine (logPath dataDir) (formatLine DateTime.UtcNow kind detail)

    /// Same as `append`; no-op when no data directory is known yet.
    let private appendForDataDir
        (dataDir: string option) (kind: string) (detail: string) : unit =
        dataDir |> Option.iter (fun dir -> append dir kind detail)

    /// `DbAgent.liveStartupSweep`'s projection repair, with the same counts and
    /// affected ids it already prints to stderr.
    let appendProjectionRepair (dataDir: string option) (message: string) =
        appendForDataDir dataDir "PROJECTION-REPAIR" message

    /// `DatabaseSetup.resolveDbConnection` falling back to the file store.
    let appendDbConnectionFallback (dataDir: string option) (message: string) =
        appendForDataDir dataDir "DB-CONNECTION-FALLBACK" message
