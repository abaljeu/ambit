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

    /// `Database.loadPersistedState` falling back to an empty Graph, with the
    /// `tryLoadGraphFromProjection` / `GraphProjection.graphFromPersistence` reason.
    let appendLoadFallback (dataDir: string option) (reason: string) =
        appendForDataDir dataDir "LOAD-FALLBACK" (sprintf "reason=%s" reason)

    /// `DbAgent.loadReconciled` dropping a row whose `EventLogFile.decodeEvent` failed.
    let appendDecodeDrop (dataDir: string option) (eventId: int) (reason: string) =
        appendForDataDir
            dataDir "DECODE-DROP" (sprintf "eventId=%d reason=%s" eventId reason)

    /// `EventLog.recover` advancing past an event via `ApplyResult.Invalid`.
    let appendRecoverInvalid (dataDir: string option) (eventId: int) (reason: string) =
        appendForDataDir
            dataDir "RECOVER-INVALID" (sprintf "eventId=%d reason=%s" eventId reason)

    /// `DatabaseSetup.resolveDbConnection` falling back to the file store.
    let appendDbConnectionFallback (dataDir: string option) (message: string) =
        appendForDataDir dataDir "DB-CONNECTION-FALLBACK" message

    /// Any other cold-start fallback not covered by a more specific entry above.
    let appendStartupFallback
        (dataDir: string option) (source: string) (detail: string) =
        appendForDataDir
            dataDir "STARTUP-FALLBACK" (sprintf "source=%s detail=%s" source detail)

    /// An unhandled exception raised while starting the server.
    let appendStartupException
        (dataDir: string option) (source: string) (ex: exn) =
        appendForDataDir
            dataDir
            "STARTUP-EXCEPTION"
            (sprintf
                "source=%s type=%s message=%s"
                source
                (ex.GetType().FullName)
                ex.Message)
