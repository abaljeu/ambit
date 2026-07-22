module Gambol.Server.Tests.GitSaveEndpointTests

open System.IO
open System.Net
open System.Net.Http
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private gitOnPath () = DesktopGit.isAvailable()

let private initRepo (dir: string) =
    GitSave.runGit dir "-c user.email=t@test -c user.name=test init"
    |> function
        | Ok _ -> ()
        | Error err -> failwith err

[<SkippableFact>]
let ``GET capabilities reports gitSave when data dir is a repo`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let tempDir = newTempDir ()
    initRepo tempDir
    use client = createClientForDir tempDir
    let! resp = client.GetAsync("/ambit/capabilities")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    let! body = resp.Content.ReadAsStringAsync()
    Assert.Contains(""""gitSave":true""", body.Replace(" ", ""))
}

[<SkippableFact>]
let ``POST save commits file changes in data dir`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let tempDir = newTempDir ()
    initRepo tempDir
    File.WriteAllText(Path.Combine(tempDir, "gambol.meta"), "0")
    use client = createClientForDir tempDir
    let! resp = client.PostAsync("/ambit/save", null)
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    match GitSave.runGit tempDir "log --oneline" with
    | Ok text -> Assert.False(System.String.IsNullOrWhiteSpace text)
    | Error err -> Assert.Fail(err)
}

[<SkippableFact>]
let ``POST save commit message includes X-Gambol-Client`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let tempDir = newTempDir ()
    initRepo tempDir
    File.WriteAllText(Path.Combine(tempDir, "gambol.meta"), "0")
    use client = createClientForDir tempDir
    use req = new HttpRequestMessage(HttpMethod.Post, "/ambit/save")
    req.Headers.TryAddWithoutValidation(
        Gambol.Shared.ClientIdentity.HeaderName,
        "Win32; Mozilla/5.0")
    |> ignore
    let! resp = client.SendAsync(req)
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    match GitSave.runGit tempDir "log -1 --pretty=%s" with
    | Ok subject ->
        Assert.Contains("client: Win32; Mozilla/5.0", subject)
        Assert.Contains("rev ", subject)
    | Error err -> Assert.Fail(err)
}
