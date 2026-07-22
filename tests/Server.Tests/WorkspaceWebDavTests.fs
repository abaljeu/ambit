module Gambol.Server.Tests.WorkspaceWebDavTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text
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
    use client = createClientForDir dataDir
    let! resp = propfind client "/ambit/dav/home/docs" "1"
    Assert.Equal(HttpStatusCode.MultiStatus, resp.StatusCode)
    let! body = resp.Content.ReadAsStringAsync()
    Assert.Contains("/ambit/dav/home/docs", body)
    Assert.Contains("<D:collection/>", body)
    Assert.Contains("a.txt", body)
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
