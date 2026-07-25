namespace Gambol.Server

open System
open System.Diagnostics
open System.IO
open System.Text
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http

/// File log of HTTP responses (status + body text). Fresh file each process start.
[<RequireQualifiedAccess>]
module HttpResponseLog =

    let private maxBodyBytes = 8 * 1024
    let private writeGate = obj ()

    /// HttpContext.Items key for workspace-relative upload target.
    [<Literal>]
    let RelativeItemKey = "gambol.upload.relative"

    let logPath (dataDir: string) =
        Path.Combine(dataDir, "SYSTEM", "http-responses.log")

    /// Delete/recreate empty log so each server run starts fresh.
    let prepareFresh (logFile: string) =
        let dir = Path.GetDirectoryName(logFile)
        if not (String.IsNullOrEmpty dir) then
            Directory.CreateDirectory(dir) |> ignore
        File.WriteAllText(logFile, "")

    let private isMutating (method: string) =
        not (HttpMethods.IsGet(method) || HttpMethods.IsHead(method))

    let private bodyCapturePaths =
        set [
            "/ambit/changes"
            "/ambit/file/parse"
            "/ambit/file-status"
            "/ambit/save"
            "/ambit/workspace/reconciliation/directory"
            "/ambit/workspace/reconciliation/added"
        ]

    let private requestPath (ctx: HttpContext) =
        match ctx.Request.Path.Value with
        | null -> ""
        | p -> p

    let private requestTarget (ctx: HttpContext) =
        requestPath ctx + string ctx.Request.QueryString

    let private oneLine (text: string) =
        if isNull text then ""
        else
            text.Replace("\r\n", "\\n").Replace("\n", "\\n")

    let private clip (text: string) =
        let t = if isNull text then "" else text
        if t.Length <= maxBodyBytes then t
        else t.Substring(0, maxBodyBytes)

    let private summarize (text: string) =
        let t = if isNull text then "" else text
        let suffix = if t.Length > maxBodyBytes then " [TRUNCATED]" else ""
        oneLine (clip t) + suffix

    let noteTargetRelative (ctx: HttpContext) (relative: string) =
        if not (String.IsNullOrEmpty relative) then
            ctx.Items[RelativeItemKey] <- relative

    let private tryRelative (ctx: HttpContext) =
        match ctx.Items.TryGetValue(RelativeItemKey) with
        | true, (:? string as r) when not (String.IsNullOrEmpty r) -> Some r
        | _ -> None

    let formatErrorReport
        (utc: DateTime)
        (relative: string)
        (status: int)
        (message: string)
        =
        sprintf
            "%s ERROR-REPORT relative=%s status=%d body=%s"
            (utc.ToString("o"))
            (oneLine (clip relative))
            status
            (oneLine (clip message))

    let private appendLine (logFile: string) (line: string) =
        lock writeGate (fun () ->
            use stream =
                new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.Read)
            use writer = new StreamWriter(stream, UTF8Encoding(false), 1024, true)
            writer.WriteLine(line)
            writer.Flush()
            stream.Flush(true))

    let private tryAppendLine logFile line =
        if not (String.IsNullOrEmpty logFile) then
            try appendLine logFile line with _ -> ()

    let formatException
        (utc: DateTime)
        (source: string)
        (operation: string)
        (context: string)
        (ex: exn)
        =
        sprintf
            "%s EXCEPTION source=%s operation=%s context=%s type=%s message=%s stack=%s"
            (utc.ToString("o"))
            (oneLine source)
            (oneLine operation)
            (oneLine context)
            (ex.GetType().FullName)
            (oneLine (clip ex.Message))
            (oneLine (clip ex.StackTrace))

    /// Best-effort direct exception append; never uses graph persistence and never throws.
    let appendException
        (logFile: string)
        (source: string)
        (operation: string)
        (context: string)
        (ex: exn)
        =
        if not (String.IsNullOrEmpty logFile) then
            try
                let line =
                    formatException DateTime.UtcNow source operation context ex
                tryAppendLine logFile line
            with _ ->
                ()

    /// Append a client/server upload failure line (no auth secrets).
    let appendErrorReport
        (logFile: string)
        (relative: string)
        (status: int)
        (message: string)
        =
        if
            not (String.IsNullOrEmpty logFile)
            && not (String.IsNullOrWhiteSpace relative)
        then
            let line =
                formatErrorReport
                    DateTime.UtcNow
                    relative
                    status
                    message
            tryAppendLine logFile line

    /// Forwards writes to the real response; keeps the first N bytes for the log.
    type private CaptureStream(inner: Stream, limit: int) =
        inherit Stream()
        let buf = Array.zeroCreate limit
        let mutable captured = 0
        let mutable overflowed = false

        let take (source: ReadOnlySpan<byte>) =
            let room = limit - captured
            if room > 0 then
                let n = min room source.Length
                source.Slice(0, n).CopyTo(Span<byte>(buf, captured, n))
                captured <- captured + n
                if source.Length > n then overflowed <- true
            elif source.Length > 0 then
                overflowed <- true

        override _.CanRead = false
        override _.CanSeek = false
        override _.CanWrite = true
        override _.Length = inner.Length
        override _.Position
            with get () = inner.Position
            and set v = inner.Position <- v
        override _.Flush() = inner.Flush()
        override _.FlushAsync(ct) = inner.FlushAsync(ct)
        override _.Read(_, _, _) = raise (NotSupportedException())
        override _.Seek(_, _) = raise (NotSupportedException())
        override _.SetLength(_) = raise (NotSupportedException())

        override _.Write(buffer, offset, count) =
            take (ReadOnlySpan(buffer, offset, count))
            inner.Write(buffer, offset, count)

        override _.WriteAsync
            (buffer, offset, count, ct: CancellationToken)
            =
            take (ReadOnlySpan(buffer, offset, count))
            inner.WriteAsync(buffer, offset, count, ct)

        override _.WriteAsync
            (buffer: ReadOnlyMemory<byte>, ct: CancellationToken)
            =
            take buffer.Span
            inner.WriteAsync(buffer, ct)

        member _.CapturedText() =
            let text = Encoding.UTF8.GetString(buf, 0, captured)
            text, overflowed

        override _.Dispose(disposing) =
            if disposing then ()
            base.Dispose(disposing)

    let private capturesRequestBody (ctx: HttpContext) =
        isMutating ctx.Request.Method
        && bodyCapturePaths.Contains(requestPath ctx)

    let private readRequestBody (ctx: HttpContext) = task {
        if not (capturesRequestBody ctx) then
            return ""
        else
            try
                ctx.Request.EnableBuffering()
                use reader =
                    new StreamReader(
                        ctx.Request.Body,
                        Encoding.UTF8,
                        true,
                        1024,
                        true)
                let! body = reader.ReadToEndAsync()
                ctx.Request.Body.Position <- 0L
                return body
            with _ ->
                if ctx.Request.Body.CanSeek then
                    ctx.Request.Body.Position <- 0L
                return "[BODY CAPTURE FAILED]"
    }

    let private formatBegin
        (utc: DateTime)
        (requestId: string)
        (ctx: HttpContext)
        (requestBody: string)
        =
        sprintf
            "%s BEGIN requestId=%s method=%s target=%s body=%s"
            (utc.ToString("o"))
            requestId
            ctx.Request.Method
            (oneLine (requestTarget ctx))
            (summarize requestBody)

    let private formatEnd
        (utc: DateTime)
        (requestId: string)
        (elapsedMs: int64)
        (ctx: HttpContext)
        (body: string)
        =
        let relative =
            tryRelative ctx
            |> Option.map (fun value -> " relative=" + summarize value)
            |> Option.defaultValue ""
        sprintf
            "%s END requestId=%s status=%d elapsedMs=%d%s body=%s"
            (utc.ToString("o"))
            requestId
            ctx.Response.StatusCode
            elapsedMs
            relative
            (summarize body)

    let private formatRequestException
        (utc: DateTime)
        (requestId: string)
        (elapsedMs: int64)
        (ex: exn)
        =
        sprintf
            "%s EXCEPTION requestId=%s elapsedMs=%d source=AspNet type=%s message=%s stack=%s"
            (utc.ToString("o"))
            requestId
            elapsedMs
            (ex.GetType().FullName)
            (summarize ex.Message)
            (summarize ex.StackTrace)

    let invokeLifecycle
        (logFile: string)
        (ctx: HttpContext)
        (next: RequestDelegate)
        =
        task {
            let requestId = Guid.NewGuid().ToString("N")
            let! requestBody = readRequestBody ctx
            tryAppendLine logFile (formatBegin DateTime.UtcNow requestId ctx requestBody)
            let timer = Stopwatch.StartNew()
            let original = ctx.Response.Body
            use capture = new CaptureStream(original, maxBodyBytes)
            ctx.Response.Body <- capture
            try
                try
                    do! next.Invoke(ctx)
                    let body, truncated = capture.CapturedText()
                    let summary =
                        if truncated then body + " [TRUNCATED]" else body
                    tryAppendLine
                        logFile
                        (formatEnd
                            DateTime.UtcNow
                            requestId
                            timer.ElapsedMilliseconds
                            ctx
                            summary)
                with ex ->
                    tryAppendLine
                        logFile
                        (formatRequestException
                            DateTime.UtcNow
                            requestId
                            timer.ElapsedMilliseconds
                            ex)
                    return raise ex
            finally
                ctx.Response.Body <- original
        }

    let useMiddleware (logFile: string) (app: WebApplication) =
        app.Use(fun (ctx: HttpContext) (next: RequestDelegate) ->
            invokeLifecycle logFile ctx next :> Task)
        |> ignore

    let private tryReadReport (req: HttpRequest) = task {
        try
            use! doc = JsonDocument.ParseAsync(req.Body)
            let root = doc.RootElement
            let relative =
                match root.TryGetProperty("relative") with
                | true, p when p.ValueKind = JsonValueKind.String ->
                    p.GetString()
                | _ -> ""
            let status =
                match root.TryGetProperty("status") with
                | true, p when p.ValueKind = JsonValueKind.Number ->
                    p.GetInt32()
                | _ -> 0
            let message =
                match root.TryGetProperty("message") with
                | true, p when p.ValueKind = JsonValueKind.String ->
                    p.GetString()
                | _ -> ""
            if String.IsNullOrWhiteSpace relative then
                return Error "relative_required"
            else
                return Ok(relative, status, if isNull message then "" else message)
        with _ ->
            return Error "invalid_error_report"
    }

    /// POST /ambit/upload-error-report — append ERROR-REPORT to same logfile.
    let registerErrorReportRoute
        (app: WebApplication)
        (isAuthenticated: HttpRequest -> bool)
        (logFile: string)
        =
        app.MapPost(
            "/ambit/upload-error-report",
            Func<HttpRequest, Task<IResult>>(fun req ->
                task {
                    if not (isAuthenticated req) then
                        return Results.Unauthorized()
                    else
                        match! tryReadReport req with
                        | Error e -> return Results.BadRequest(e)
                        | Ok(relative, status, message) ->
                            appendErrorReport logFile relative status message
                            return Results.NoContent()
                })
        )
        |> ignore

    /// Wipe previous log, print path, install middleware (early in pipeline).
    /// Returns the log file path for error-report registration.
    let register (dataDir: string) (app: WebApplication) =
        let path = logPath dataDir
        prepareFresh path
        eprintfn "Gambol: HTTP response log (fresh) at %s" path
        useMiddleware path app
        path
