namespace Gambol.Server

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Gambol.Shared

/// WebDAV Class 1: `/ambit/dav/{label}/…` → `DataDir/{label}/…`.
[<RequireQualifiedAccess>]
module WorkspaceWebDav =

    type private Depth =
        | Depth0
        | Depth1
        | DepthInfinity

    type private Resolved =
        { label: string
          relative: string
          workspaceRoot: string
          fullPath: string }

    type private Entry =
        { relative: string
          fullPath: string
          isCollection: bool
          mtimeUtc: DateTime
          length: int64 }

    let private xmlEscape (s: string) =
        s.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")

    let private touchesGit (relative: string) =
        relative.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.exists (fun s ->
            String.Equals(s, ".git", StringComparison.OrdinalIgnoreCase))

    let private isUnderRoot (root: string) (candidate: string) =
        let r =
            Path.GetFullPath(root)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)
        let c = Path.GetFullPath(candidate)
        c = r
        || c.StartsWith(
            r + string Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase)

    let private parseLabel (label: string) : Result<string, string> =
        match Filename.create label with
        | Filename.Ok name -> Ok name
        | _ -> Error "invalid_label"

    let private resolve
        (dataDir: string)
        (labelRaw: string)
        (pathTail: string)
        : Result<Resolved, string> =
        match parseLabel labelRaw with
        | Error e -> Error e
        | Ok label ->
            match WorkspaceSyncScope.normalizeRelative pathTail with
            | Error e -> Error e
            | Ok relative when touchesGit relative -> Error "invalid_path"
            | Ok relative ->
                try
                    let root =
                        Path.GetFullPath(Path.Combine(dataDir, label))
                    let full =
                        if relative = "" then root
                        else
                            let parts =
                                relative.Split(
                                    [| '/' |],
                                    StringSplitOptions.RemoveEmptyEntries)
                            Path.GetFullPath(
                                Path.Combine(Array.append [| root |] parts))
                    if not (isUnderRoot root full) then Error "invalid_path"
                    else
                        Ok
                            { label = label
                              relative = relative
                              workspaceRoot = root
                              fullPath = full }
                with ex ->
                    Error ex.Message

    let tryValidatePath
        (dataDir: string)
        (labelRaw: string)
        (pathTail: string)
        : Result<unit, string> =
        resolve dataDir labelRaw pathTail |> Result.map ignore

    let private parseDepth (req: HttpRequest) =
        match req.Headers.TryGetValue("Depth") with
        | true, values ->
            match string values.[0] with
            | "0" -> Depth0
            | s when
                s.Equals("infinity", StringComparison.OrdinalIgnoreCase)
                ->
                DepthInfinity
            | _ -> Depth1
        | _ -> Depth1

    let private hrefOf (label: string) (relative: string) (isColl: bool) =
        let baseHref = "/ambit/dav/" + Uri.EscapeDataString(label)
        let path =
            if relative = "" then baseHref + "/"
            else
                relative.Split('/')
                |> Array.map Uri.EscapeDataString
                |> fun parts -> baseHref + "/" + String.Join("/", parts)
        if isColl && not (path.EndsWith("/", StringComparison.Ordinal)) then
            path + "/"
        else
            path

    let private httpDate (utc: DateTime) =
        utc.ToUniversalTime().ToString("R")

    let private isOmitted
        (workspaceRoot: string)
        (relative: string)
        : Result<bool, string> =
        if relative = "" then Ok false
        elif touchesGit relative then Ok true
        else GitCheckIgnore.isEffectivelyIgnored workspaceRoot relative

    let private tryEntryInfo (fullPath: string) =
        try
            if Directory.Exists fullPath then
                let di = DirectoryInfo(fullPath)
                Some(true, di.LastWriteTimeUtc, 0L)
            elif File.Exists fullPath then
                let fi = FileInfo(fullPath)
                Some(false, fi.LastWriteTimeUtc, fi.Length)
            else
                None
        with _ ->
            None

    let private childRelative (parentRel: string) (name: string) =
        if parentRel = "" then name else parentRel + "/" + name

    let private listChildren
        (workspaceRoot: string)
        (parentRel: string)
        (parentFull: string)
        : Result<(string * string * bool) list, string> =
        try
            let dirs =
                Directory.GetDirectories parentFull
                |> Array.toList
                |> List.choose (fun p ->
                    let name = Path.GetFileName p
                    if
                        String.Equals(
                            name,
                            ".git",
                            StringComparison.OrdinalIgnoreCase)
                    then
                        None
                    else
                        Some(childRelative parentRel name, p, true))
            let files =
                Directory.GetFiles parentFull
                |> Array.toList
                |> List.map (fun p ->
                    childRelative parentRel (Path.GetFileName p), p, false)
            (dirs @ files)
            |> List.fold
                (fun acc (rel, full, isColl) ->
                    match acc with
                    | Error e -> Error e
                    | Ok kept ->
                        match isOmitted workspaceRoot rel with
                        | Error e -> Error e
                        | Ok true -> Ok kept
                        | Ok false -> Ok((rel, full, isColl) :: kept))
                (Ok [])
            |> Result.map List.rev
        with ex ->
            Error ex.Message

    let private toEntry (rel, full, isColl, mtime, len) : Entry =
        { relative = rel
          fullPath = full
          isCollection = isColl
          mtimeUtc = mtime
          length = len }

    let private collectEntries
        (resolved: Resolved)
        (depth: Depth)
        : Result<Entry list, string> =
        match tryEntryInfo resolved.fullPath with
        | None -> Error "not_found"
        | Some(isColl, mtime, len) ->
            let self =
                toEntry (
                    resolved.relative,
                    resolved.fullPath,
                    isColl,
                    mtime,
                    len)

            let rec walk d rel full coll : Result<Entry list, string> =
                match d, coll with
                | Depth0, _
                | _, false -> Ok []
                | Depth1, true
                | DepthInfinity, true ->
                    match listChildren resolved.workspaceRoot rel full with
                    | Error e -> Error e
                    | Ok children ->
                        children
                        |> List.fold
                            (fun acc (cRel, cFull, cColl) ->
                                match acc with
                                | Error e -> Error e
                                | Ok soFar ->
                                    match tryEntryInfo cFull with
                                    | None -> Ok soFar
                                    | Some(_, cm, cl) ->
                                        let here =
                                            toEntry (cRel, cFull, cColl, cm, cl)
                                        match d with
                                        | DepthInfinity when cColl ->
                                            match
                                                walk
                                                    DepthInfinity
                                                    cRel
                                                    cFull
                                                    true
                                            with
                                            | Error e -> Error e
                                            | Ok deeper ->
                                                Ok(soFar @ (here :: deeper))
                                        | _ -> Ok(soFar @ [ here ]))
                            (Ok [])

            match walk depth resolved.relative resolved.fullPath isColl with
            | Error e -> Error e
            | Ok kids -> Ok(self :: kids)

    let private responseXml (label: string) (e: Entry) =
        let href = xmlEscape (hrefOf label e.relative e.isCollection)
        let rt =
            if e.isCollection then
                "<D:resourcetype><D:collection/></D:resourcetype>"
            else
                "<D:resourcetype/>"
        String.Concat(
            "<D:response><D:href>",
            href,
            "</D:href><D:propstat><D:prop>",
            rt,
            "<D:getlastmodified>",
            httpDate e.mtimeUtc,
            "</D:getlastmodified><D:getcontentlength>",
            string e.length,
            "</D:getcontentlength></D:prop>",
            "<D:status>HTTP/1.1 200 OK</D:status>",
            "</D:propstat></D:response>")

    let private multistatusXml (label: string) (entries: Entry list) =
        String.Concat(
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<D:multistatus xmlns:D=\"DAV:\">",
            String.Concat(entries |> List.map (responseXml label)),
            "</D:multistatus>")

    let private ensureParents (fullPath: string) =
        try
            let parent = Path.GetDirectoryName fullPath
            if not (String.IsNullOrEmpty parent) then
                Directory.CreateDirectory parent |> ignore
            Ok ()
        with ex ->
            Error ex.Message

    let private rejectIfIgnored (resolved: Resolved) =
        match isOmitted resolved.workspaceRoot resolved.relative with
        | Error e -> Error e
        | Ok true -> Error "ignored"
        | Ok false -> Ok ()

    let private handlePropfind (resolved: Resolved) (req: HttpRequest) =
        match collectEntries resolved (parseDepth req) with
        | Error "not_found" -> Results.NotFound()
        | Error e -> Results.Problem(detail = e, statusCode = 500)
        | Ok entries ->
            Results.Content(
                multistatusXml resolved.label entries,
                "application/xml; charset=utf-8",
                statusCode = 207)

    let private handleGet (resolved: Resolved) =
        match rejectIfIgnored resolved with
        | Error "ignored" -> Results.NotFound()
        | Error e -> Results.Problem(detail = e, statusCode = 500)
        | Ok () ->
            match tryEntryInfo resolved.fullPath with
            | None -> Results.NotFound()
            | Some(true, _, _) -> Results.BadRequest("is a collection")
            | Some(false, _, _) ->
                Results.File(resolved.fullPath, "application/octet-stream")

    let private trySourceMtime (req: HttpRequest) =
        match req.Headers.TryGetValue(WorkspaceDavClient.SourceMtimeHeaderName) with
        | true, values ->
            values
            |> Seq.tryHead
            |> Option.bind (fun text ->
                match DateTimeOffset.TryParse text with
                | true, dto -> Some dto.UtcDateTime
                | _ -> None)
        | _ -> None

    let private handlePut (resolved: Resolved) (req: HttpRequest) =
        task {
            match rejectIfIgnored resolved with
            | Error "ignored" ->
                return
                    Results.Json(
                        {| error = "ignored by .gitignore" |},
                        statusCode = 403)
            | Error e ->
                return Results.Problem(detail = e, statusCode = 500)
            | Ok () when Directory.Exists resolved.fullPath ->
                return Results.Conflict("is a collection")
            | Ok () ->
                match ensureParents resolved.fullPath with
                | Error e ->
                    return Results.Problem(detail = e, statusCode = 500)
                | Ok () ->
                    let existed = File.Exists resolved.fullPath
                    let! writeResult =
                        task {
                            try
                                use fs =
                                    new FileStream(
                                        resolved.fullPath,
                                        FileMode.Create,
                                        FileAccess.Write)
                                do! req.Body.CopyToAsync(fs)
                                return Ok()
                            with ex ->
                                return Error ex.Message
                        }
                    match writeResult with
                    | Error msg ->
                        return Results.Problem(detail = msg, statusCode = 500)
                    | Ok () ->
                        match trySourceMtime req with
                        | Some utc ->
                            File.SetLastWriteTimeUtc(resolved.fullPath, utc)
                        | None -> ()
                        let href =
                            hrefOf resolved.label resolved.relative false
                        return
                            if existed then Results.NoContent()
                            else Results.Created(href, null)
        }

    let private handleMkcol (resolved: Resolved) =
        match rejectIfIgnored resolved with
        | Error "ignored" ->
            Results.Json(
                {| error = "ignored by .gitignore" |},
                statusCode = 403)
        | Error e -> Results.Problem(detail = e, statusCode = 500)
        | Ok () when
            File.Exists resolved.fullPath
            || Directory.Exists resolved.fullPath
            ->
            Results.StatusCode(405)
        | Ok () ->
            try
                Directory.CreateDirectory resolved.workspaceRoot |> ignore
                let parent = Path.GetDirectoryName resolved.fullPath
                if
                    not (String.IsNullOrEmpty parent)
                    && not (Directory.Exists parent)
                then
                    Results.Conflict("parent missing")
                else
                    Directory.CreateDirectory resolved.fullPath |> ignore
                    Results.Created(
                        hrefOf resolved.label resolved.relative true,
                        null)
            with ex ->
                Results.Problem(detail = ex.Message, statusCode = 500)

    let private handlePrepare
        (dataDir: string)
        (labelRaw: string)
        (clientHint: string option)
        =
        match parseLabel labelRaw with
        | Error e -> Results.BadRequest(e)
        | Ok label ->
            let root = Path.GetFullPath(Path.Combine(dataDir, label))
            match WorkspaceGit.ensureInit root with
            | Error e -> Results.Problem(detail = e, statusCode = 500)
            | Ok () ->
                match
                    WorkspaceGit.jitCommitBeforeWorkspacePush
                        root
                        clientHint
                with
                | Error e -> Results.Problem(detail = e, statusCode = 500)
                | Ok () -> Results.Json {| result = "ok" |}

    let private handleFinish
        (dataDir: string)
        (labelRaw: string)
        (clientHint: string option)
        =
        match parseLabel labelRaw with
        | Error e -> Results.BadRequest(e)
        | Ok label ->
            let root = Path.GetFullPath(Path.Combine(dataDir, label))
            match WorkspaceGit.ensureInit root with
            | Error e -> Results.Problem(detail = e, statusCode = 500)
            | Ok () ->
                match
                    WorkspaceGit.commitAll
                        root
                        "gambol: webdav push"
                        clientHint
                with
                | Error e -> Results.Problem(detail = e, statusCode = 500)
                | Ok msg ->
                    let head =
                        match WorkspaceGit.tryHead root with
                        | Ok h -> h
                        | Error _ -> None
                    Results.Json {| result = msg; head = head |}

    let private dispatch
        (isAuthenticated: HttpRequest -> bool)
        (dataDir: string)
        (ctx: HttpContext)
        (label: string)
        (pathTail: string)
        : Task<IResult> =
        task {
            if not (isAuthenticated ctx.Request) then
                return Results.Unauthorized()
            else
                match resolve dataDir label pathTail with
                | Error e -> return Results.BadRequest(e)
                | Ok resolved ->
                    match ctx.Request.Method.ToUpperInvariant() with
                    | "PROPFIND" ->
                        return handlePropfind resolved ctx.Request
                    | "GET" -> return handleGet resolved
                    | "PUT" -> return! handlePut resolved ctx.Request
                    | "MKCOL" -> return handleMkcol resolved
                    | _ -> return Results.StatusCode(405)
        }

    let registerRoutes
        (app: WebApplication)
        (isAuthenticated: HttpRequest -> bool)
        (dataDir: string)
        =
        let methods = [| "PROPFIND"; "GET"; "PUT"; "MKCOL" |]
        let clientHint (req: HttpRequest) =
            match req.Headers.TryGetValue(ClientIdentity.HeaderName) with
            | true, values -> ClientIdentity.tryFromValues values
            | _ -> None

        app.MapPost(
            "/ambit/dav/{label}/_prepare-push",
            Func<HttpRequest, string, Task<IResult>>(fun req label ->
                task {
                    if not (isAuthenticated req) then
                        return Results.Unauthorized()
                    else
                        return handlePrepare dataDir label (clientHint req)
                })
        )
        |> ignore

        app.MapPost(
            "/ambit/dav/{label}/_finish-commit",
            Func<HttpRequest, string, Task<IResult>>(fun req label ->
                task {
                    if not (isAuthenticated req) then
                        return Results.Unauthorized()
                    else
                        return handleFinish dataDir label (clientHint req)
                })
        )
        |> ignore

        app.MapMethods(
            "/ambit/dav/{label}/{*path}",
            methods,
            Func<HttpContext, string, string, Task<IResult>>(
                fun ctx label path ->
                    dispatch isAuthenticated dataDir ctx label path)
        )
        |> ignore

        app.MapMethods(
            "/ambit/dav/{label}",
            methods,
            Func<HttpContext, string, Task<IResult>>(fun ctx label ->
                dispatch isAuthenticated dataDir ctx label "")
        )
        |> ignore
