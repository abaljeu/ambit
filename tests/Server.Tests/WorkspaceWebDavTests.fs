module Gambol.Server.Tests.WorkspaceWebDavTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Threading.Tasks
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
let ``PUT ignored path is rejected`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    Directory.CreateDirectory home |> ignore
    File.WriteAllText(Path.Combine(home, ".gitignore"), "*.log\n")
    use client = createClientForDir dataDir
    let! resp =
        putBytes client "/ambit/dav/home/noise.log" (Encoding.UTF8.GetBytes "x")
    Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode)
    Assert.False(File.Exists(Path.Combine(home, "noise.log")))
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
