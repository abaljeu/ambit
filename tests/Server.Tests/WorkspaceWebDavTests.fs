module Gambol.Server.Tests.WorkspaceWebDavTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private gitOnPath () = DesktopGit.isAvailable()

let private propfind (client: HttpClient) (url: string) (depth: string) =
    task {
        use req = new HttpRequestMessage(HttpMethod("PROPFIND"), url)
        req.Headers.TryAddWithoutValidation("Depth", depth) |> ignore
        return! client.SendAsync(req)
    }

let private mkcol (client: HttpClient) (url: string) =
    task {
        use req = new HttpRequestMessage(HttpMethod("MKCOL"), url)
        return! client.SendAsync(req)
    }

let private putBytes (client: HttpClient) (url: string) (bytes: byte[]) =
    task {
        use content = new ByteArrayContent(bytes)
        return! client.PutAsync(url, content)
    }

[<Fact>]
let ``resolve rejects path escape`` () =
    let dataDir = newTempDir ()
    let r = WorkspaceWebDav.tryValidatePath dataDir "home" "../other"
    Assert.Equal(Error "invalid_path", r)

[<Fact>]
let ``resolve rejects .git segment`` () =
    let dataDir = newTempDir ()
    let r = WorkspaceWebDav.tryValidatePath dataDir "home" "src/.git/config"
    Assert.Equal(Error "invalid_path", r)

[<SkippableFact>]
let ``PROPFIND returns href collection and getlastmodified`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    Directory.CreateDirectory(Path.Combine(home, "docs")) |> ignore
    File.WriteAllText(Path.Combine(home, "docs", "a.txt"), "hi")
    File.WriteAllText(Path.Combine(home, "docs", ".amb"), "directory body")
    use client = createClientForDir dataDir
    let! resp = propfind client "/ambit/dav/home/docs" "1"
    Assert.Equal(HttpStatusCode.MultiStatus, resp.StatusCode)
    let! body = resp.Content.ReadAsStringAsync()
    Assert.Contains("/ambit/dav/home/docs", body)
    Assert.Contains("<D:collection/>", body)
    Assert.Contains("a.txt", body)
    Assert.Contains(".amb", body)
    Assert.Contains("<D:getlastmodified>", body)
    Assert.DoesNotContain(".git", body)
}

[<SkippableFact>]
let ``PROPFIND omits gitignored paths`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    Directory.CreateDirectory home |> ignore
    File.WriteAllText(Path.Combine(home, ".gitignore"), "secret.txt\n")
    File.WriteAllText(Path.Combine(home, "secret.txt"), "nope")
    File.WriteAllText(Path.Combine(home, "ok.txt"), "yes")
    use client = createClientForDir dataDir
    let! resp = propfind client "/ambit/dav/home" "1"
    Assert.Equal(HttpStatusCode.MultiStatus, resp.StatusCode)
    let! body = resp.Content.ReadAsStringAsync()
    Assert.Contains("ok.txt", body)
    Assert.Contains(".gitignore", body)
    Assert.DoesNotContain("secret.txt", body)
}

[<SkippableFact>]
let ``PROPFIND infinity omits ignored directory tree`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    Directory.CreateDirectory(Path.Combine(home, "keep")) |> ignore
    Directory.CreateDirectory(Path.Combine(home, "noise", "deep")) |> ignore
    File.WriteAllText(Path.Combine(home, ".gitignore"), "noise/\n")
    File.WriteAllText(Path.Combine(home, "keep", "a.txt"), "yes")
    File.WriteAllText(Path.Combine(home, "noise", "deep", "x.txt"), "no")
    use client = createClientForDir dataDir
    let! resp = propfind client "/ambit/dav/home" "infinity"
    Assert.Equal(HttpStatusCode.MultiStatus, resp.StatusCode)
    let! body = resp.Content.ReadAsStringAsync()
    Assert.Contains("keep", body)
    Assert.Contains("a.txt", body)
    Assert.DoesNotContain("noise", body)
    Assert.DoesNotContain("x.txt", body)
}

[<SkippableFact>]
let ``PUT ignored path is rejected`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    Directory.CreateDirectory home |> ignore
    File.WriteAllText(Path.Combine(home, ".gitignore"), "*.cache\n")
    use client = createClientForDir dataDir
    let! resp =
        putBytes client "/ambit/dav/home/noise.cache" (Encoding.UTF8.GetBytes "x")
    Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode)
    Assert.False(File.Exists(Path.Combine(home, "noise.cache")))
}

let private putBytesWithMtime (client: HttpClient) (url: string) (bytes: byte[]) (mtime: DateTime) =
    task {
        use content = new ByteArrayContent(bytes)
        use req = new HttpRequestMessage(HttpMethod.Put, url)
        req.Content <- content
        req.Headers.TryAddWithoutValidation(
            WorkspaceDavClient.SourceMtimeHeaderName,
            mtime.ToString("O"))
        |> ignore
        return! client.SendAsync(req)
    }

let private utf8Sample (text: string) =
    FileText.sampleFromBytes (Encoding.UTF8.GetBytes text) false

let private nulSample =
    FileText.sampleFromBytes [| 0uy; 65uy |] false

let private assertContentType
    (fileName: string)
    (sample: FileText.Sample)
    (expected: string)
    =
    Assert.Equal(expected, WorkspaceFileMedia.contentTypeFor fileName sample)
    let dot = fileName.LastIndexOf('.')
    if dot >= 0 then
        let upper = fileName.Substring(0, dot) + fileName.Substring(dot).ToUpperInvariant()
        Assert.Equal(expected, WorkspaceFileMedia.contentTypeFor upper sample)

let private plain = "text/plain; charset=utf-8"

[<Fact>]
let ``content type pdf is application/pdf`` () =
    assertContentType "doc.pdf" (utf8Sample "%PDF") "application/pdf"
    assertContentType "doc.pdf" nulSample "application/pdf"

[<Fact>]
let ``content type png is image/png`` () =
    assertContentType "pic.png" (utf8Sample "not-a-png") "image/png"
    assertContentType "pic.png" nulSample "image/png"

[<Fact>]
let ``content type jpg and jpeg are image/jpeg`` () =
    assertContentType "pic.jpg" nulSample "image/jpeg"
    assertContentType "pic.jpeg" (utf8Sample "text") "image/jpeg"

[<Fact>]
let ``content type gif is image/gif`` () =
    assertContentType "pic.gif" nulSample "image/gif"

[<Fact>]
let ``content type webp is image/webp`` () =
    assertContentType "pic.webp" nulSample "image/webp"

[<Fact>]
let ``content type svg is image/svg+xml`` () =
    assertContentType "pic.svg" (utf8Sample "<svg/>") "image/svg+xml"
    assertContentType "pic.svg" nulSample "image/svg+xml"

[<Fact>]
let ``content type heic is image/heic`` () =
    assertContentType "pic.heic" nulSample "image/heic"

[<Fact>]
let ``content type html and htm stay plain text`` () =
    assertContentType "page.html" (utf8Sample "<p>hi</p>") plain
    assertContentType "page.html" nulSample plain
    assertContentType "page.htm" nulSample plain

[<Fact>]
let ``content type text sample is plain text`` () =
    let text = utf8Sample "let x = 1\n"
    assertContentType "note.fs" text plain
    assertContentType "note.ts" text plain
    assertContentType "note.py" text plain
    assertContentType "note.yaml" text plain
    assertContentType "note.toml" text plain
    assertContentType "note.xml" text plain
    assertContentType "note.sh" text plain
    assertContentType "dir.amb" text plain
    assertContentType ".gitignore" text plain
    assertContentType "note.txt" text plain
    assertContentType "note.md" text plain
    assertContentType "note.json" text plain
    Assert.Equal(plain, WorkspaceFileMedia.contentTypeFor "README" text)
    Assert.Equal(plain, WorkspaceFileMedia.contentTypeFor "docs/README" text)

[<Fact>]
let ``content type binary sample is octet-stream`` () =
    assertContentType "note.fs" nulSample "application/octet-stream"
    assertContentType "archive.bin" nulSample "application/octet-stream"
    assertContentType "note.txt" nulSample "application/octet-stream"
    assertContentType "note.md" nulSample "application/octet-stream"
    assertContentType "note.json" nulSample "application/octet-stream"
    assertContentType ".gitignore" nulSample "application/octet-stream"
    Assert.Equal(
        "application/octet-stream",
        WorkspaceFileMedia.contentTypeFor "README" nulSample)

[<SkippableFact>]
let ``GET pdf returns application/pdf and no attachment`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    Directory.CreateDirectory home |> ignore
    File.WriteAllBytes(Path.Combine(home, "doc.pdf"), [| 0x25uy; 0x50uy; 0x44uy; 0x46uy |])
    use client = createClientForDir dataDir
    Assert.True(client.DefaultRequestHeaders.Contains("Cookie"))
    let! resp = client.GetAsync("/ambit/dav/home/doc.pdf")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    Assert.Equal("application/pdf", resp.Content.Headers.ContentType.MediaType)
    let disposition = resp.Content.Headers.ContentDisposition
    Assert.True(
        isNull disposition
        || not (
            String.Equals(
                disposition.DispositionType,
                "attachment",
                StringComparison.OrdinalIgnoreCase)))
}

let private assertInlinePlain (resp: HttpResponseMessage) =
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    let media = resp.Content.Headers.ContentType
    Assert.False(isNull media)
    Assert.Equal("text/plain", media.MediaType)
    Assert.Equal("utf-8", media.CharSet)
    let disposition = resp.Content.Headers.ContentDisposition
    Assert.False(isNull disposition)
    Assert.Equal("inline", disposition.DispositionType)

[<SkippableFact>]
let ``GET text file is plain text inline`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    Directory.CreateDirectory home |> ignore
    let files = [|
        "note.fs", "let x = 1\n"
        "note.ts", "export const n = 1\n"
        "note.py", "print(1)\n"
        "note.yaml", "a: 1\n"
        "note.toml", "a = 1\n"
        "note.xml", "<a/>\n"
        "note.sh", "echo hi\n"
        "dir.amb", "body\n"
        ".gitignore", "*.log\n"
        "README", "hello\n"
        "note.json", "{}\n"
        "note.txt", "hello\n"
        "note.md", "# hi\n"
    |]
    for name, body in files do
        File.WriteAllText(Path.Combine(home, name), body)
    use client = createClientForDir dataDir
    for name, _ in files do
        let! resp = client.GetAsync("/ambit/dav/home/" + Uri.EscapeDataString(name))
        assertInlinePlain resp
}

[<SkippableFact>]
let ``GET binary file stays octet-stream`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    Directory.CreateDirectory home |> ignore
    File.WriteAllBytes(Path.Combine(home, "note.fs"), [| 0uy; 1uy; 2uy |])
    use client = createClientForDir dataDir
    let! resp = client.GetAsync("/ambit/dav/home/note.fs")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    Assert.Equal("application/octet-stream", resp.Content.Headers.ContentType.MediaType)
    Assert.True(isNull resp.Content.Headers.ContentDisposition)
}

[<SkippableFact>]
let ``GET html stays plain text and svg stays svg`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    Directory.CreateDirectory home |> ignore
    File.WriteAllBytes(Path.Combine(home, "page.html"), [| 0uy; 65uy |])
    File.WriteAllText(Path.Combine(home, "pic.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\"/>")
    use client = createClientForDir dataDir
    let! html = client.GetAsync("/ambit/dav/home/page.html")
    assertInlinePlain html
    let! svg = client.GetAsync("/ambit/dav/home/pic.svg")
    Assert.Equal(HttpStatusCode.OK, svg.StatusCode)
    Assert.Equal("image/svg+xml", svg.Content.Headers.ContentType.MediaType)
    let disposition = svg.Content.Headers.ContentDisposition
    Assert.True(
        isNull disposition
        || not (
            String.Equals(
                disposition.DispositionType,
                "inline",
                StringComparison.OrdinalIgnoreCase)))
}

[<SkippableFact>]
let ``GET after PUT round-trips bytes`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    Directory.CreateDirectory(Path.Combine(dataDir, "home")) |> ignore
    use client = createClientForDir dataDir
    let payload = Encoding.UTF8.GetBytes "round-trip"
    let! putResp = putBytes client "/ambit/dav/home/f.txt" payload
    Assert.True(
        putResp.StatusCode = HttpStatusCode.Created
        || putResp.StatusCode = HttpStatusCode.NoContent)
    let! getResp = client.GetAsync("/ambit/dav/home/f.txt")
    Assert.Equal(HttpStatusCode.OK, getResp.StatusCode)
    let! got = getResp.Content.ReadAsByteArrayAsync()
    Assert.Equal<byte>(payload, got)
}

[<SkippableFact>]
let ``direct capability uploads exact WAF-sensitive path and body idempotently`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    Directory.CreateDirectory(Path.Combine(dataDir, "home")) |> ignore
    use client = createClientForDir dataDir
    let relative = "employment/research/targets/priorities.md"
    let payload =
        Array.concat
            [ Encoding.UTF8.GetBytes(
                "<script>alert('ModSecurity')</script> ../ café %00")
              [| 0uy; 255uy; 13uy; 10uy |] ]
    let resource = WorkspaceDavClient.encodeResourceToken "home" relative
    let digest = SHA256.HashData payload |> Convert.ToHexString
    let grantJson =
        JsonSerializer.Serialize(
            {| resource = resource
               size = payload.LongLength
               sha256 = digest
               sourceMtimeTicks = 0L |})
    use grantContent =
        new StringContent(grantJson, Encoding.UTF8, "application/json")
    let! grantResponse =
        client.PostAsync("/ambit/upload-capability", grantContent)
    Assert.Equal(HttpStatusCode.OK, grantResponse.StatusCode)
    let! grantBody = grantResponse.Content.ReadAsStringAsync()
    use grant = JsonDocument.Parse grantBody
    let capability =
        grant.RootElement.GetProperty("capability").GetString()
    let uploadAddress =
        grant.RootElement.GetProperty("uploadUrl").GetString()
    Assert.Equal("http://localhost/ambit/direct-upload", uploadAddress)
    let uploadUrl = Uri(uploadAddress).PathAndQuery
    Assert.Equal("/ambit/direct-upload", uploadUrl)
    let upload bytes = task {
        use content = new ByteArrayContent(bytes)
        use req = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
        req.Content <- content
        req.Headers.TryAddWithoutValidation(
            "Authorization",
            "GambolUpload " + capability)
        |> ignore
        return! client.SendAsync req
    }
    use! first = upload payload
    use! replay = upload payload
    let changed = payload |> Array.map (fun value -> value ^^^ 1uy)
    use! changedBody = upload changed
    Assert.Equal(HttpStatusCode.Created, first.StatusCode)
    Assert.Equal(HttpStatusCode.NoContent, replay.StatusCode)
    Assert.Equal(HttpStatusCode.BadRequest, changedBody.StatusCode)
    let full =
        relative.Split('/')
        |> Array.fold
            (fun path segment -> Path.Combine(path, segment))
            (Path.Combine(dataDir, "home"))
    Assert.Equal<byte>(payload, File.ReadAllBytes full)
}

[<Fact>]
let ``direct upload rejects a tampered capability`` () = task {
    let dataDir = newTempDir ()
    Directory.CreateDirectory(Path.Combine(dataDir, "home")) |> ignore
    use client = createClientForDir dataDir
    use content = new ByteArrayContent([| 1uy |])
    use req = new HttpRequestMessage(HttpMethod.Post, "/ambit/direct-upload")
    req.Content <- content
    req.Headers.TryAddWithoutValidation(
        "Authorization",
        "GambolUpload tampered")
    |> ignore
    let! response = client.SendAsync req
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    let! body = response.Content.ReadAsStringAsync()
    Assert.Contains("invalid_upload_capability", body)
}

[<SkippableFact>]
let ``PUT honors X-Gambol-Source-Mtime on disk`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    Directory.CreateDirectory home |> ignore
    use client = createClientForDir dataDir
    let source = DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc)
    let! putResp =
        putBytesWithMtime
            client
            "/ambit/dav/home/m.txt"
            (Encoding.UTF8.GetBytes "mtime")
            source
    Assert.True(
        putResp.StatusCode = HttpStatusCode.Created
        || putResp.StatusCode = HttpStatusCode.NoContent)
    let full = Path.Combine(home, "m.txt")
    let onDisk = File.GetLastWriteTimeUtc full
    Assert.Equal(source, onDisk)
}

[<SkippableFact>]
let ``MKCOL creates collection visible to PROPFIND`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    Directory.CreateDirectory(Path.Combine(dataDir, "home")) |> ignore
    use client = createClientForDir dataDir
    let! mk = mkcol client "/ambit/dav/home/sub"
    Assert.Equal(HttpStatusCode.Created, mk.StatusCode)
    let! resp = propfind client "/ambit/dav/home" "1"
    let! body = resp.Content.ReadAsStringAsync()
    Assert.Contains("/ambit/dav/home/sub", body)
    Assert.Contains("<D:collection/>", body)
}

[<SkippableFact>]
let ``path escape via request does not succeed`` () = task {
    let dataDir = newTempDir ()
    Directory.CreateDirectory(Path.Combine(dataDir, "home")) |> ignore
    use client = createClientForDir dataDir
    let! resp = propfind client "/ambit/dav/home/%2e%2e/other" "0"
    // ASP.NET may 404-normalize; our resolve also returns invalid_path.
    Assert.True(
        resp.StatusCode = HttpStatusCode.BadRequest
        || resp.StatusCode = HttpStatusCode.NotFound)
    Assert.NotEqual(HttpStatusCode.MultiStatus, resp.StatusCode)
}

[<SkippableFact>]
let ``concurrent direct uploads keep siblings when one path is ignored`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    Directory.CreateDirectory home |> ignore
    File.WriteAllText(Path.Combine(home, ".gitignore"), "bad.cache\n")
    use client = createClientForDir dataDir
    let uploadOne (relative: string) (text: string) = task {
        let payload = Encoding.UTF8.GetBytes text
        let resource = WorkspaceDavClient.encodeResourceToken "home" relative
        let digest = SHA256.HashData payload |> Convert.ToHexString
        let grantJson =
            JsonSerializer.Serialize(
                {| resource = resource
                   size = payload.LongLength
                   sha256 = digest
                   sourceMtimeTicks = 0L |})
        use grantContent =
            new StringContent(grantJson, Encoding.UTF8, "application/json")
        let! grantResponse =
            client.PostAsync("/ambit/upload-capability", grantContent)
        Assert.Equal(HttpStatusCode.OK, grantResponse.StatusCode)
        let! grantBody = grantResponse.Content.ReadAsStringAsync()
        use grant = JsonDocument.Parse grantBody
        let capability =
            grant.RootElement.GetProperty("capability").GetString()
        use content = new ByteArrayContent(payload)
        use req =
            new HttpRequestMessage(HttpMethod.Post, "/ambit/direct-upload")
        req.Content <- content
        req.Headers.TryAddWithoutValidation(
            "Authorization",
            "GambolUpload " + capability)
        |> ignore
        return! client.SendAsync req
    }
    let! results =
        Task.WhenAll(
            [| uploadOne "ok1.txt" "one"
               uploadOne "bad.cache" "nope"
               uploadOne "ok2.txt" "two"
               uploadOne "nested/ok3.txt" "three" |])
    Assert.Equal(HttpStatusCode.Created, results.[0].StatusCode)
    Assert.Equal(HttpStatusCode.Forbidden, results.[1].StatusCode)
    Assert.Equal(HttpStatusCode.Created, results.[2].StatusCode)
    Assert.Equal(HttpStatusCode.Created, results.[3].StatusCode)
    Assert.True(File.Exists(Path.Combine(home, "ok1.txt")))
    Assert.False(File.Exists(Path.Combine(home, "bad.cache")))
    Assert.True(File.Exists(Path.Combine(home, "ok2.txt")))
    Assert.True(File.Exists(Path.Combine(home, "nested", "ok3.txt")))
}

[<SkippableFact>]
let ``finish-commit advances HEAD after PUT`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    Directory.CreateDirectory home |> ignore
    use client = createClientForDir dataDir
    let! putResp =
        putBytes
            client
            "/ambit/dav/home/note.txt"
            (Encoding.UTF8.GetBytes "committed")
    Assert.Equal(HttpStatusCode.Created, putResp.StatusCode)
    let! finish =
        client.PostAsync("/ambit/dav/home/_finish-commit", null)
    Assert.Equal(HttpStatusCode.OK, finish.StatusCode)
    let! body = finish.Content.ReadAsStringAsync()
    Assert.Contains("head", body)
    match WorkspaceGit.tryHead home with
    | Ok(Some oid) -> Assert.False(String.IsNullOrWhiteSpace oid)
    | Ok None -> Assert.Fail("expected HEAD after finish-commit")
    | Error err -> Assert.Fail(err)
}


[<SkippableFact>]
let ``prepare-push JIT commits dirty DataDir`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    Directory.CreateDirectory home |> ignore
    match WorkspaceGit.ensureInit home with
    | Error err -> Assert.Fail(err)
    | Ok () ->
        File.WriteAllText(Path.Combine(home, "dirty.txt"), "before")
        use client = createClientForDir dataDir
        let! prep =
            client.PostAsync("/ambit/dav/home/_prepare-push", null)
        Assert.Equal(HttpStatusCode.OK, prep.StatusCode)
        match WorkspaceGit.tryHead home with
        | Ok(Some oid) -> Assert.False(String.IsNullOrWhiteSpace oid)
        | Ok None -> Assert.Fail("expected HEAD after prepare-push")
        | Error err -> Assert.Fail(err)
        match WorkspaceGit.isDirty home with
        | Ok false -> ()
        | Ok true -> Assert.Fail("expected clean after JIT")
        | Error err -> Assert.Fail(err)
}

type private ForceAsyncSendHandler(inner: HttpMessageHandler) =
    inherit DelegatingHandler(inner)
    override this.Send(request, cancellationToken) =
        this.SendAsync(request, cancellationToken).GetAwaiter().GetResult()

/// Phase-1 upload loop: local edit → WorkspaceFileSync.post → DataDir bytes.
[<SkippableFact>]
let ``WorkspaceFileSync.post uploads edited local file into DataDir`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let label =
        "up"
        + Guid.NewGuid().ToString("N").Substring(0, 10)
    let ledgerPath = WorkspaceSyncLedger.ledgerPathFor label
    if File.Exists ledgerPath then File.Delete ledgerPath
    let dataDir = newTempDir ()
    let serverRoot = Path.Combine(dataDir, label)
    Directory.CreateDirectory serverRoot |> ignore
    File.WriteAllText(Path.Combine(serverRoot, "note.txt"), "OLD")
    match WorkspaceGit.ensureInit serverRoot with
    | Error err -> Assert.Fail(err)
    | Ok () -> ()
    let mapped = newTempDir ()
    File.WriteAllText(Path.Combine(mapped, "note.txt"), "NEW-EDIT")
    File.SetLastWriteTimeUtc(
        Path.Combine(mapped, "note.txt"),
        DateTime.UtcNow.AddMinutes(1.0))
    let priorDb = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", null)
    try
        suppressDailyGitSave dataDir
        use factory =
            (new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>())
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
                    |> ignore)
        use handler = new ForceAsyncSendHandler(factory.Server.CreateHandler())
        use client = new HttpClient(handler, disposeHandler = true)
        client.BaseAddress <- factory.Server.BaseAddress
        withDevelopmentCookie client |> ignore
        let ambitBase =
            client.BaseAddress.ToString().TrimEnd('/') + "/ambit"
        let scope =
            { label = label
              relative = "note.txt"
              kind = SyncScopeKind.File }
        match
            WorkspaceFileSync.post
                client
                ambitBase
                mapped
                scope
                None
                None
        with
        | Error e -> Assert.Fail(e)
        | Ok result ->
            Assert.True(
                result.uploaded >= 1,
                "expected at least one uploaded path, got detail="
                + result.detail)
            Assert.Equal(
                "NEW-EDIT",
                File.ReadAllText(Path.Combine(serverRoot, "note.txt")))
    finally
        if isNull priorDb then
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", null)
        else
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", priorDb)
        if File.Exists ledgerPath then File.Delete ledgerPath
}
