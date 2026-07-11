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
/// `git-receive-pack`; custom policy (JIT pull, reject-dirty push)
/// is middleware before stock pack subprocesses.
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
            if not (WorkspaceGit.isRepo root) then
                Error "workspace git repository not found"
            else
                match WorkspaceGit.ensurePushConfig root with
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
        string -> LazyLoadReconciliation.ChangedPath list -> Async<Result<unit, string>>

    let private logReconcileError label err =
        eprintfn
            "[GitGateway] Post-receive reconciliation failed for '%s': %s"
            label
            err

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
                let diffResult =
                    match oldHead, WorkspaceGit.tryHead workspaceRoot with
                    | Error err, _ -> Error err
                    | _, Error err -> Error err
                    | Ok oldOid, Ok None ->
                        Error "successful receive left repository without HEAD"
                    | Ok oldOid, Ok(Some newOid) ->
                        WorkspaceGit.changedPathsBetween workspaceRoot oldOid newOid
                match diffResult with
                | Error err -> logReconcileError workspaceLabel err
                | Ok [] -> ()
                | Ok changedPaths ->
                    match! reconcile workspaceLabel changedPaths with
                    | Ok () -> ()
                    | Error err -> logReconcileError workspaceLabel err
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

    let private handleInfoRefs
        (isAuthenticated: HttpRequest -> bool)
        (dataDir: string)
        (flush: FlushFn)
        (ctx: HttpContext)
        (repoName: string)
        : Task =
        task {
            if not (isAuthenticated ctx.Request) then
                do! rejectUnauthorized ctx.Response
            else
                match resolveWorkspaceRoot dataDir repoName with
                | Error err ->
                    do! writeTextError ctx.Response 404 err
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
                    | Some WorkspacePush ->
                        match WorkspaceGit.assertCleanForWorkspacePush root with
                        | Error err ->
                            do! writeTextError ctx.Response 403 err
                        | Ok () ->
                            match advertiseRefs root WorkspacePush with
                            | Error err ->
                                do! writeTextError ctx.Response 500 err
                            | Ok body ->
                                do!
                                    writeBytes
                                        ctx.Response
                                        200
                                        (contentTypeAdvertise WorkspacePush)
                                        body
                    | Some WorkspacePull ->
                        let hint = clientHintOf ctx.Request
                        let! prep =
                            prepareWorkspacePull flush root hint
                        match prep with
                        | Error err ->
                            do! writeTextError ctx.Response 500 err
                        | Ok () ->
                            match advertiseRefs root WorkspacePull with
                            | Error err ->
                                do! writeTextError ctx.Response 500 err
                            | Ok body ->
                                do!
                                    writeBytes
                                        ctx.Response
                                        200
                                        (contentTypeAdvertise WorkspacePull)
                                        body
        }

    let private handlePackPost
        (isAuthenticated: HttpRequest -> bool)
        (dataDir: string)
        (flush: FlushFn)
        (reconcile: ReconcileFn)
        (service: Service)
        (ctx: HttpContext)
        (repoName: string)
        : Task =
        task {
            if not (isAuthenticated ctx.Request) then
                do! rejectUnauthorized ctx.Response
            else
                match resolveWorkspaceRoot dataDir repoName with
                | Error err ->
                    do! writeTextError ctx.Response 404 err
                | Ok(label, root) ->
                    match service with
                    | WorkspacePush ->
                        match WorkspaceGit.assertCleanForWorkspacePush root with
                        | Error err ->
                            do! writeTextError ctx.Response 403 err
                        | Ok () ->
                            let! body = readBody ctx.Request
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
                                do! writeTextError ctx.Response 500 err
                            | Ok result ->
                                do!
                                    writeBytes
                                        ctx.Response
                                        200
                                        (contentTypeResult WorkspacePush)
                                        result
                    | WorkspacePull ->
                        let hint = clientHintOf ctx.Request
                        let! prep =
                            prepareWorkspacePull flush root hint
                        match prep with
                        | Error err ->
                            do! writeTextError ctx.Response 500 err
                        | Ok () ->
                            let! body = readBody ctx.Request
                            match
                                statelessRpc root WorkspacePull body with
                            | Error err ->
                                do! writeTextError ctx.Response 500 err
                            | Ok result ->
                                do!
                                    writeBytes
                                        ctx.Response
                                        200
                                        (contentTypeResult WorkspacePull)
                                        result
        }

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
