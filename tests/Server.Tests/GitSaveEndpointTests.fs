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

