namespace Gambol.Server

open System
open System.Diagnostics
open System.IO
open System.Text
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open Gambol.Shared

/// Smart HTTP gateway. Wire paths use stock `git-upload-pack` /
/// `git-receive-pack`; custom policy (JIT pull, reject-dirty push when
/// born, allow dirty unborn seed) is middleware before stock pack
/// subprocesses.
[<RequireQualifiedAccess>]
module GitGateway =

    type Service =
        | WorkspacePull
        | WorkspacePush

    /// Path / ?service= name (stock git-*-pack).
    let urlServiceName =
        function
        | WorkspacePull -> WorkspaceGitRemote.WorkspacePull
        | WorkspacePush -> WorkspaceGitRemote.WorkspacePush

    /// Stock git subprocess verb.
    let private gitPackCommand =
        function
        | WorkspacePull -> "upload-pack"
        | WorkspacePush -> "receive-pack"

    let private serviceFromQuery (raw: string) : Service option =
        match raw with
        | WorkspaceGitRemote.WorkspacePull -> Some WorkspacePull
        | WorkspaceGitRemote.WorkspacePush -> Some WorkspacePush
        | _ -> None

    let resolveWorkspaceRoot
        (dataDir: string)
        (repoName: string)
        : Result<string * string, string> =
        match WorkspaceGitRemote.tryLabelFromRepoName repoName with
        | None -> Error "invalid workspace git repo name"
        | Some label ->
            let root = Path.Combine(dataDir, label)
            // Recreate after DataDir/{label} wipe so seed Upload can proceed.
            match WorkspaceGit.ensureInit root with
            | Error err -> Error err
            | Ok () -> Ok(label, root)

    let private pktLine (payload: string) : byte[] =
        let body = Encoding.UTF8.GetBytes(payload)
        let header =
            Encoding.ASCII.GetBytes(sprintf "%04x" (body.Length + 4))
        Array.append header body

    let private advertisePrefix (service: Service) : byte[] =
        let line =
            sprintf "# service=%s\n" (urlServiceName service)
        Array.append (pktLine line) (Encoding.ASCII.GetBytes("0000"))

    let private runGitExchange
        (workDir: string)
        (arguments: string)
        (input: byte[])
        : Result<byte[], string> =
        try
            let psi =
                ProcessStartInfo(
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true)
            use proc = Process.Start(psi)
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

    let advertiseRefs
        (workspaceRoot: string)
        (service: Service)
        : Result<byte[], string> =
        let args =
            sprintf
                "%s --advertise-refs --stateless-rpc ."
                (gitPackCommand service)
        match runGitExchange workspaceRoot args [||] with
        | Error err -> Error err
        | Ok refs -> Ok(Array.append (advertisePrefix service) refs)

    let statelessRpc
        (workspaceRoot: string)
        (service: Service)
        (requestBody: byte[])
        : Result<byte[], string> =
        let args =
            sprintf "%s --stateless-rpc ." (gitPackCommand service)
        runGitExchange workspaceRoot args requestBody

    let private contentTypeAdvertise (service: Service) =
        sprintf
            "application/x-%s-advertisement"
            (urlServiceName service)

    let private contentTypeResult (service: Service) =
        sprintf "application/x-%s-result" (urlServiceName service)

    type FlushFn = unit -> Async<Result<unit, string>>
    type ReconcileFn =
        string
            -> LazyLoadReconciliation.ChangedPath list
            -> Async<Result<LazyLoadReconciliationReport.Failure list, string>>

    let private logReconcileError label err =
        eprintfn
            "[GitGateway] Post-receive reconciliation failed for '%s': %s"
            label
            err

    let private storeReconcileResult
        (workspaceLabel: string)
        (result: Result<LazyLoadReconciliationReport.Failure list, string>)
        =
        match result with
        | Ok failures ->
            LazyLoadReconciliationDiagnostics.set workspaceLabel failures
        | Error err ->
            logReconcileError workspaceLabel err
            LazyLoadReconciliationDiagnostics.set
                workspaceLabel
                [ { path = ""
                    message = err } ]

    let private logAlignHeadError label err =
        eprintfn
            "[GitGateway] Align HEAD after unborn receive failed for '%s': %s"
            label
            err

    let private prepareWorkspacePush
        (flush: FlushFn)
        (workspaceRoot: string)
        (clientHint: string option)
        : Async<Result<unit, string>> =
        async {
            let! flushResult = flush ()
            match flushResult with
            | Error err -> return Error err
            | Ok () ->
                match WorkspaceGit.tryHead workspaceRoot with
                | Error err -> return Error err
                | Ok None -> return Ok ()
                | Ok(Some _) ->
                    match
                        WorkspaceGit.jitCommitBeforeWorkspacePush
                            workspaceRoot
                            clientHint
                    with
                    | Error err -> return Error err
                    | Ok _ -> return Ok ()
        }

    let completeWorkspacePush
        (workspaceRoot: string)
        (workspaceLabel: string)
        (oldHead: Result<string option, string>)
        (receiveResult: Result<byte[], string>)
        (reconcile: ReconcileFn)
        : Async<Result<byte[], string>> =
        async {
            match receiveResult with
            | Error err -> return Error err
            | Ok response ->
                match oldHead with
                | Ok None ->
                    match
                        WorkspaceGit.alignHeadAfterUnbornReceive workspaceRoot
                    with
                    | Error err -> logAlignHeadError workspaceLabel err
                    | Ok () -> ()
                | _ -> ()
                let diffResult =
                    match oldHead, WorkspaceGit.tryHead workspaceRoot with
                    | Error err, _ -> Error err
                    | _, Error err -> Error err
                    | Ok oldOid, Ok None ->
                        Error "successful receive left repository without HEAD"
                    | Ok oldOid, Ok(Some newOid) ->
                        WorkspaceGit.changedPathsBetween workspaceRoot oldOid newOid
                match diffResult with
                | Error err ->
                    logReconcileError workspaceLabel err
                    LazyLoadReconciliationDiagnostics.set
                        workspaceLabel
                        [ { path = ""
                            message = err } ]
                | Ok changedPaths ->
                    let! reconcileResult = reconcile workspaceLabel changedPaths
                    storeReconcileResult workspaceLabel reconcileResult
                return Ok response
        }

    let private prepareWorkspacePull
        (flush: FlushFn)
        (workspaceRoot: string)
        (clientHint: string option)
        : Async<Result<unit, string>> =
        async {
            let! flushResult = flush ()
            match flushResult with
            | Error err -> return Error err
            | Ok () ->
                match WorkspaceGit.jitCommitIfDirty workspaceRoot clientHint with
                | Error err -> return Error err
                | Ok _ -> return Ok ()
        }

    let private readBody (req: HttpRequest) : Task<byte[]> = task {
        use ms = new MemoryStream()
        do! req.Body.CopyToAsync(ms)
        return ms.ToArray()
    }

    let private writeBytes
        (resp: HttpResponse)
        (status: int)
        (contentType: string)
        (body: byte[])
        : Task =
        task {
            resp.StatusCode <- status
            resp.ContentType <- contentType
            resp.Headers.CacheControl <- "no-cache"
            do! resp.Body.WriteAsync(body.AsMemory())
        }

    let private writeTextError
        (resp: HttpResponse)
        (status: int)
        (message: string)
        : Task =
        let bytes = Encoding.UTF8.GetBytes(message)
        writeBytes resp status "text/plain; charset=utf-8" bytes

    let private clientHintOf (req: HttpRequest) : string option =
        match req.Headers.TryGetValue(ClientIdentity.HeaderName) with
        | true, values -> ClientIdentity.tryFromValues values
        | _ -> None

    let private rejectUnauthorized (resp: HttpResponse) : Task =
        task {
            resp.StatusCode <- 401
            resp.Headers.WWWAuthenticate <-
                StringValues(
                    sprintf "Basic realm=\"%s\"" AuthToken.gitBasicRealm)
        }

    // Release builds treat FS3511 as error (TreatWarningsAsErrors). Deeply
    // nested `task` CEs with many `do!` branches fail static reduction; keep
    // these handlers in `async` and surface Task only at the ASP.NET boundary.
    let private handleInfoRefs
        (isAuthenticated: HttpRequest -> bool)
        (dataDir: string)
        (flush: FlushFn)
        (ctx: HttpContext)
        (repoName: string)
        : Task =
        async {
            if not (isAuthenticated ctx.Request) then
                do! rejectUnauthorized ctx.Response |> Async.AwaitTask
            else
                match resolveWorkspaceRoot dataDir repoName with
                | Error err ->
                    do! writeTextError ctx.Response 404 err |> Async.AwaitTask
                | Ok(_, root) ->
                    let serviceOpt =
                        match ctx.Request.Query.TryGetValue("service") with
                        | true, values ->
                            serviceFromQuery (string values.[0])
                        | _ -> None
                    match serviceOpt with
                    | None ->
                        do!
                            writeTextError
                                ctx.Response
                                400
                                "missing or unknown service (want git-upload-pack|git-receive-pack)"
                            |> Async.AwaitTask
                    | Some WorkspacePush ->
                        match advertiseRefs root WorkspacePush with
                        | Error err ->
                            do!
                                writeTextError ctx.Response 500 err
                                |> Async.AwaitTask
                        | Ok body ->
                            do!
                                writeBytes
                                    ctx.Response
                                    200
                                    (contentTypeAdvertise WorkspacePush)
                                    body
                                |> Async.AwaitTask
                    | Some WorkspacePull ->
                        let hint = clientHintOf ctx.Request
                        let! prep = prepareWorkspacePull flush root hint
                        match prep with
                        | Error err ->
                            do!
                                writeTextError ctx.Response 500 err
                                |> Async.AwaitTask
                        | Ok () ->
                            match advertiseRefs root WorkspacePull with
                            | Error err ->
                                do!
                                    writeTextError ctx.Response 500 err
                                    |> Async.AwaitTask
                            | Ok body ->
                                do!
                                    writeBytes
                                        ctx.Response
                                        200
                                        (contentTypeAdvertise WorkspacePull)
                                        body
                                    |> Async.AwaitTask
        }
        |> Async.StartAsTask
        :> Task

    let private handlePackPost
        (isAuthenticated: HttpRequest -> bool)
        (dataDir: string)
        (flush: FlushFn)
        (reconcile: ReconcileFn)
        (service: Service)
        (ctx: HttpContext)
        (repoName: string)
        : Task =
        async {
            if not (isAuthenticated ctx.Request) then
                do! rejectUnauthorized ctx.Response |> Async.AwaitTask
            else
                match resolveWorkspaceRoot dataDir repoName with
                | Error err ->
                    do! writeTextError ctx.Response 404 err |> Async.AwaitTask
                | Ok(label, root) ->
                    match service with
                    | WorkspacePush ->
                        let hint = clientHintOf ctx.Request
                        let! prep = prepareWorkspacePush flush root hint
                        match prep with
                        | Error err ->
                            do!
                                writeTextError ctx.Response 403 err
                                |> Async.AwaitTask
                        | Ok () ->
                            let! body = readBody ctx.Request |> Async.AwaitTask
                            let oldHead = WorkspaceGit.tryHead root
                            let! completed =
                                completeWorkspacePush
                                    root
                                    label
                                    oldHead
                                    (statelessRpc root WorkspacePush body)
                                    reconcile
                            match completed with
                            | Error err ->
                                do!
                                    writeTextError ctx.Response 500 err
                                    |> Async.AwaitTask
                            | Ok result ->
                                do!
                                    writeBytes
                                        ctx.Response
                                        200
                                        (contentTypeResult WorkspacePush)
                                        result
                                    |> Async.AwaitTask
                    | WorkspacePull ->
                        let hint = clientHintOf ctx.Request
                        let! prep = prepareWorkspacePull flush root hint
                        match prep with
                        | Error err ->
                            do!
                                writeTextError ctx.Response 500 err
                                |> Async.AwaitTask
                        | Ok () ->
                            let! body = readBody ctx.Request |> Async.AwaitTask
                            match
                                statelessRpc root WorkspacePull body with
                            | Error err ->
                                do!
                                    writeTextError ctx.Response 500 err
                                    |> Async.AwaitTask
                            | Ok result ->
                                do!
                                    writeBytes
                                        ctx.Response
                                        200
                                        (contentTypeResult WorkspacePull)
                                        result
                                    |> Async.AwaitTask
        }
        |> Async.StartAsTask
        :> Task

    let registerRoutes
        (app: WebApplication)
        (isAuthenticated: HttpRequest -> bool)
        (dataDir: string)
        (flush: FlushFn)
        (reconcile: ReconcileFn)
        =
        app.MapGet(
            "/ambit/git/{repoName}/info/refs",
            Func<HttpContext, string, Task>(fun ctx repoName ->
                handleInfoRefs isAuthenticated dataDir flush ctx repoName)
        )
        |> ignore
        app.MapPost(
            "/ambit/git/{repoName}/git-upload-pack",
            Func<HttpContext, string, Task>(fun ctx repoName ->
                handlePackPost
                    isAuthenticated
                    dataDir
                    flush
                    reconcile
                    WorkspacePull
                    ctx
                    repoName)
        )
        |> ignore
        app.MapPost(
            "/ambit/git/{repoName}/git-receive-pack",
            Func<HttpContext, string, Task>(fun ctx repoName ->
                handlePackPost
                    isAuthenticated
                    dataDir
                    flush
                    reconcile
                    WorkspacePush
                    ctx
                    repoName)
        )
        |> ignore
