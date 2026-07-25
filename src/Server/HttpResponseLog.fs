namespace Gambol.Server

open System
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

    let logPath (contentRoot: string) =
        Path.Combine(contentRoot, "logs", "http-responses.log")

    /// Delete/recreate empty log so each server run starts fresh.
    let prepareFresh (logFile: string) =
        let dir = Path.GetDirectoryName(logFile)
        if not (String.IsNullOrEmpty dir) then
            Directory.CreateDirectory(dir) |> ignore
        File.WriteAllText(logFile, "")

    let private isMutating (method: string) =
        not (HttpMethods.IsGet(method) || HttpMethods.IsHead(method))

    let private isUploadRelated (path: string) =
        path.IndexOf("/dav/", StringComparison.OrdinalIgnoreCase) >= 0
        || path.IndexOf("/git/", StringComparison.OrdinalIgnoreCase) >= 0
        || path.IndexOf("/changes", StringComparison.OrdinalIgnoreCase) >= 0

    let private shouldLog (ctx: HttpContext) =
        let status = ctx.Response.StatusCode
        let path =
            match ctx.Request.Path.Value with
            | null -> ""
            | p -> p
        status < 200
        || status >= 300
        || isMutating ctx.Request.Method
        || isUploadRelated path

    let private requestPath (ctx: HttpContext) =
        match ctx.Request.Path.Value with
        | null -> ""
        | p -> p

    let private oneLine (text: string) =
        if isNull text then ""
        else
            text.Replace("\r\n", "\\n").Replace("\n", "\\n")

    let private clip (text: string) =
        let t = if isNull text then "" else text
        if t.Length <= maxBodyBytes then t
        else t.Substring(0, maxBodyBytes)

    let noteTargetRelative (ctx: HttpContext) (relative: string) =
        if not (String.IsNullOrEmpty relative) then
            ctx.Items[RelativeItemKey] <- relative

    let private tryRelative (ctx: HttpContext) =
        match ctx.Items.TryGetValue(RelativeItemKey) with
        | true, (:? string as r) when not (String.IsNullOrEmpty r) -> Some r
        | _ -> None

    let formatEntry
        (utc: DateTime)
        (method: string)
        (path: string)
        (status: int)
        (body: string)
        (truncated: bool)
        (relative: string option)
        =
        let note = if truncated then " truncated" else ""
        let rel =
            match relative with
            | Some r when not (String.IsNullOrEmpty r) ->
                " relative=" + oneLine (clip r)
            | _ -> ""
        sprintf
            "%s %s %s%s -> %d%s body=%s"
            (utc.ToString("o"))
            method
            path
            rel
            status
            note
            (oneLine (clip body))

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
            File.AppendAllText(logFile, line + Environment.NewLine))

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
            try appendLine logFile line with _ -> ()

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

    let private tryLog
        (logFile: string)
        (ctx: HttpContext)
        (capture: CaptureStream)
        =
        if shouldLog ctx then
            let body, truncated = capture.CapturedText()
            let line =
                formatEntry
                    DateTime.UtcNow
                    ctx.Request.Method
                    (requestPath ctx)
                    ctx.Response.StatusCode
                    body
                    truncated
                    (tryRelative ctx)
            try appendLine logFile line with _ -> ()

    let useMiddleware (logFile: string) (app: WebApplication) =
        app.Use(fun (ctx: HttpContext) (next: RequestDelegate) ->
            task {
                let original = ctx.Response.Body
                use capture = new CaptureStream(original, maxBodyBytes)
                ctx.Response.Body <- capture
                try
                    do! next.Invoke(ctx)
                    tryLog logFile ctx capture
                finally
                    ctx.Response.Body <- original
            }
            :> Task)
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
    let register (contentRoot: string) (app: WebApplication) =
        let path = logPath contentRoot
        prepareFresh path
        eprintfn "Gambol: HTTP response log (fresh) at %s" path
        useMiddleware path app
        path
