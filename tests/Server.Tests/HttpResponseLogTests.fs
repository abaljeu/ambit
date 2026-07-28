module Gambol.Server.Tests.HttpResponseLogTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.Configuration
open Xunit
open Gambol.Server
open Gambol.Server.Tests.TestBackend

let private contextForPost (path: string) (body: string) =
    let ctx = DefaultHttpContext()
    ctx.Request.Method <- "POST"
    ctx.Request.Path <- PathString(path)
    ctx.Request.QueryString <- QueryString("?rev=7")
    ctx.Request.Body <- new MemoryStream(Encoding.UTF8.GetBytes(body))
    ctx.Response.Body <- new MemoryStream()
    ctx

let private lifecycleLines (logPath: string) =
    File.ReadAllLines(logPath)
    |> Array.filter (fun line ->
        line.Contains(" BEGIN ") || line.Contains(" END ") || line.Contains(" EXCEPTION "))

let private requestId (line: string) =
    line.Split(' ')
    |> Array.find (fun part -> part.StartsWith("requestId="))

let private runLifecycle
    (logPath: string)
    (ctx: HttpContext)
    (next: HttpContext -> Task)
    =
    HttpResponseLog.invokeLifecycle logPath ctx (RequestDelegate next)

[<Fact>]
let ``formatErrorReport is one-line identity plus status`` () =
    let line =
        HttpResponseLog.formatErrorReport
            (DateTime(2026, 7, 25, 16, 0, 0, DateTimeKind.Utc))
            "docs/blocked.txt"
            403
            "blocked\nby WAF"
    Assert.StartsWith("2026-07-25T16:00:00.0000000Z ERROR-REPORT ", line)
    Assert.Contains("relative=docs/blocked.txt", line)
    Assert.Contains("status=403", line)
    Assert.Contains("body=blocked\\nby WAF", line)

[<Fact>]
let ``successful post logs correlated begin and end with payload`` () = task {
    let dataDir = newTempDir ()
    let logPath = HttpResponseLog.logPath dataDir
    HttpResponseLog.prepareFresh logPath
    let body = """{"changes":[{"id":7}]}"""
    let ctx = contextForPost "/ambit/changes" body
    do!
        runLifecycle logPath ctx (fun endpointCtx -> task {
            endpointCtx.Response.StatusCode <- 200
            do! endpointCtx.Response.WriteAsync("""{"revision":8}""")
        })
    let lines = lifecycleLines logPath
    Assert.Equal(2, lines.Length)
    Assert.Contains("method=POST target=/ambit/changes?rev=7", lines.[0])
    Assert.Contains("body={\"changes\":[{\"id\":7}]}", lines.[0])
    Assert.Contains("status=200", lines.[1])
    Assert.Contains("elapsedMs=", lines.[1])
    Assert.Contains("body={\"revision\":8}", lines.[1])
    Assert.Equal(requestId lines.[0], requestId lines.[1])
}

[<Fact>]
let ``controlled bad request logs begin and end with request body`` () = task {
    let dataDir = newTempDir ()
    let logPath = HttpResponseLog.logPath dataDir
    HttpResponseLog.prepareFresh logPath
    let ctx = contextForPost "/ambit/changes" "not valid change json"
    do!
        runLifecycle logPath ctx (fun endpointCtx -> task {
            endpointCtx.Response.StatusCode <- 400
            do! endpointCtx.Response.WriteAsync("invalid_change_body")
        })
    let lines = lifecycleLines logPath
    Assert.Equal(2, lines.Length)
    Assert.Contains("body=not valid change json", lines.[0])
    Assert.Contains("status=400", lines.[1])
    Assert.Equal(requestId lines.[0], requestId lines.[1])
}

[<Fact>]
let ``response body remains on original stream after capture lifecycle`` () = task {
    let dataDir = newTempDir ()
    let logPath = HttpResponseLog.logPath dataDir
    HttpResponseLog.prepareFresh logPath
    let original = new MemoryStream()
    let ctx = DefaultHttpContext()
    ctx.Request.Method <- "GET"
    ctx.Request.Path <- PathString("/ambit/state")
    ctx.Response.Body <- original
    let expected =
        "Internal server error in FileAgent GetState (dataDir=C:\\data)."
    do!
        runLifecycle logPath ctx (fun endpointCtx -> task {
            endpointCtx.Response.StatusCode <- 500
            endpointCtx.Response.ContentType <- "application/problem+json"
            do! endpointCtx.Response.WriteAsync(expected)
        })
    Assert.True(obj.ReferenceEquals(original, ctx.Response.Body))
    original.Position <- 0L
    use reader =
        new StreamReader(
            original,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks = false,
            leaveOpen = true)
    let body = reader.ReadToEnd()
    Assert.Equal(expected, body)
    Assert.True(body.Length > 0)
}

[<Fact>]
let ``thrown exception logs correlated begin and exception`` () = task {
    let dataDir = newTempDir ()
    let logPath = HttpResponseLog.logPath dataDir
    HttpResponseLog.prepareFresh logPath
    let ctx = contextForPost "/ambit/changes" """{"changes":[]}"""
    let! wasRethrown =
        task {
            try
                do!
                    runLifecycle logPath ctx (fun _ ->
                        Task.FromException(InvalidOperationException("route exploded")))
                return false
            with :? InvalidOperationException ->
                return true
        }
    Assert.True(wasRethrown)
    let lines = lifecycleLines logPath
    Assert.Equal(2, lines.Length)
    Assert.Contains("BEGIN", lines.[0])
    Assert.Contains("EXCEPTION", lines.[1])
    Assert.Contains("source=AspNet", lines.[1])
    Assert.Contains("type=System.InvalidOperationException", lines.[1])
    Assert.Contains("message=route exploded", lines.[1])
    Assert.Contains("elapsedMs=", lines.[1])
    Assert.Equal(requestId lines.[0], requestId lines.[1])
}

[<Fact>]
let ``pending handler is observable as unmatched begin without leaking task`` () = task {
    let dataDir = newTempDir ()
    let logPath = HttpResponseLog.logPath dataDir
    HttpResponseLog.prepareFresh logPath
    let ctx = contextForPost "/ambit/changes" """{"changes":[{"pending":true}]}"""
    let release = TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
    let pending = runLifecycle logPath ctx (fun _ -> release.Task)
    let deadline = DateTime.UtcNow.AddSeconds(2.0)
    while lifecycleLines logPath |> Array.isEmpty do
        if DateTime.UtcNow >= deadline then
            Assert.Fail("BEGIN was not durably written before handler execution.")
        do! Task.Delay(10)
    let observed = lifecycleLines logPath
    Assert.Single(observed) |> ignore
    Assert.Contains(" BEGIN ", observed.[0])
    Assert.Contains("pending", observed.[0])
    release.SetResult()
    do! pending.WaitAsync(TimeSpan.FromSeconds(2.0))
}

[<Fact>]
let ``request body reaches endpoint unchanged after begin logging`` () = task {
    let dataDir = newTempDir ()
    let logPath = HttpResponseLog.logPath dataDir
    HttpResponseLog.prepareFresh logPath
    let expected = """{"changes":[{"text":"unchanged"}]}"""
    let ctx = contextForPost "/ambit/changes" expected
    let actual = TaskCompletionSource<string>()
    do!
        runLifecycle logPath ctx (fun endpointCtx -> task {
            use reader = new StreamReader(endpointCtx.Request.Body)
            let! body = reader.ReadToEndAsync()
            actual.SetResult(body)
        })
    Assert.Equal(expected, actual.Task.Result)
}

[<Fact>]
let ``large request body is bounded and visibly truncated`` () = task {
    let dataDir = newTempDir ()
    let logPath = HttpResponseLog.logPath dataDir
    HttpResponseLog.prepareFresh logPath
    let body = String.replicate 9000 "x" + "END-OF-PAYLOAD"
    let ctx = contextForPost "/ambit/changes" body
    do! runLifecycle logPath ctx (fun _ -> Task.CompletedTask)
    let beginLine = lifecycleLines logPath |> Array.head
    Assert.Contains("[TRUNCATED]", beginLine)
    Assert.DoesNotContain("END-OF-PAYLOAD", beginLine)
}

[<Fact>]
let ``logger write failure never breaks request`` () = task {
    let dataDir = newTempDir ()
    let impossibleLogPath = Path.Combine(dataDir, "missing", "http.log")
    let ctx = contextForPost "/ambit/changes" "{}"
    let reached = TaskCompletionSource()
    do!
        runLifecycle impossibleLogPath ctx (fun _ ->
            reached.SetResult()
            Task.CompletedTask)
    Assert.True(reached.Task.IsCompletedSuccessfully)
}

[<Fact>]
let ``upload-error-report appends ERROR-REPORT to SYSTEM http-responses.log`` () = task {
    let dataDir = newTempDir ()
    Directory.CreateDirectory(Path.Combine(dataDir, "home")) |> ignore
    use factory =
        (new WebApplicationFactory<Program>())
            .WithWebHostBuilder(fun builder ->
                builder.ConfigureAppConfiguration(fun _ config ->
                    config.AddInMemoryCollection(
                        dict [
                            "DataDir", dataDir
                            "Persistence:Mode", "file"
                            "DB_CONNECTION_STRING", ""
                            "Auth:Username", ""
                            "Auth:Password", ""
                        ]
                    )
                    |> ignore
                )
                |> ignore
            )
    use client = factory.CreateClient()
    let logPath = HttpResponseLog.logPath dataDir
    use content =
        new StringContent(
            """{"relative":"docs/x.txt","status":403,"message":"WAF block"}""",
            Encoding.UTF8,
            "application/json")
    let! resp = client.PostAsync("/ambit/upload-error-report", content)
    Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode)
    let text = File.ReadAllText logPath
    Assert.Contains("ERROR-REPORT relative=docs/x.txt status=403", text)
    Assert.Contains("body=WAF block", text)
    Assert.DoesNotContain("gambol_auth", text)
}
