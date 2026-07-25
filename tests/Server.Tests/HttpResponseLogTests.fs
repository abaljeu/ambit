module Gambol.Server.Tests.HttpResponseLogTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.Configuration
open Xunit
open Gambol.Server
open Gambol.Server.Tests.TestBackend

[<Fact>]
let ``formatEntry includes relative when present`` () =
    let line =
        HttpResponseLog.formatEntry
            (DateTime(2026, 7, 25, 16, 0, 0, DateTimeKind.Utc))
            "POST"
            "/ambit/direct-upload"
            400
            "upload_digest_mismatch"
            false
            (Some "docs/a.txt")
    Assert.Contains(
        "POST /ambit/direct-upload relative=docs/a.txt -> 400",
        line)
    Assert.Contains("body=upload_digest_mismatch", line)
    Assert.DoesNotContain("Cookie", line)
    Assert.DoesNotContain("GambolUpload", line)

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
