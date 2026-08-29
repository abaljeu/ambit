module Gambol.Server.Tests.DailyGitSaveTests

open System
open System.IO
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared

let private gitOnPath () = DesktopGit.isAvailable()

let private newTempDir () =
    let dir = Path.Combine(Path.GetTempPath(), $"gambol-dgs-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    dir

let private utcDay = DateTime(2026, 8, 28, 15, 30, 0, DateTimeKind.Utc)

let private fakeGit (dir: string) =
    Directory.CreateDirectory(Path.Combine(dir, ".git")) |> ignore

let private initRepo (dir: string) =
    GitSave.runGit dir "-c user.email=t@test -c user.name=test init"
    |> function
        | Ok _ -> ()
        | Error err -> failwith err

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

[<Fact>]
let ``formatUtcDay is ISO date`` () =
    Assert.Equal("2026-08-28", DailyGitSave.formatUtcDay utcDay)

[<Fact>]
let ``shouldRunToday is true when stamp is missing`` () =
    Assert.True(DailyGitSave.shouldRunToday None "2026-08-28")

[<Fact>]
let ``shouldRunToday is false when stamp matches today`` () =
    Assert.False(DailyGitSave.shouldRunToday (Some "2026-08-28") "2026-08-28")

[<Fact>]
let ``shouldRunToday trims stamp text`` () =
    Assert.False(DailyGitSave.shouldRunToday (Some "2026-08-28\n") "2026-08-28")

[<Fact>]
let ``shouldRunToday is true for a prior day`` () =
    Assert.True(DailyGitSave.shouldRunToday (Some "2026-08-27") "2026-08-28")

[<Fact>]
let ``shouldRunToday is true for malformed stamp`` () =
    Assert.True(DailyGitSave.shouldRunToday (Some "nope") "2026-08-28")

[<Fact>]
let ``repoRoots puts DataDir first then children`` () =
    let roots =
        DailyGitSave.repoRoots true "data" [ "data/home"; "data/SYSTEM" ]
    Assert.Equal<string list>([ "data"; "data/home"; "data/SYSTEM" ], roots)

[<Fact>]
let ``repoRoots omits DataDir when it is not a repo`` () =
    let roots = DailyGitSave.repoRoots false "data" [ "data/home" ]
    Assert.Equal<string list>([ "data/home" ], roots)

[<Fact>]
let ``discoverRepoRoots includes SYSTEM and TRASH and skips nested`` () =
    let dataDir = newTempDir ()
    fakeGit dataDir
    let systemDir = Path.Combine(dataDir, "SYSTEM")
    let trashDir = Path.Combine(dataDir, "TRASH")
    let homeDir = Path.Combine(dataDir, "home")
    let nested = Path.Combine(homeDir, "nested")
    let plain = Path.Combine(dataDir, "plain")
    Directory.CreateDirectory systemDir |> ignore
    Directory.CreateDirectory trashDir |> ignore
    Directory.CreateDirectory homeDir |> ignore
    Directory.CreateDirectory nested |> ignore
    Directory.CreateDirectory plain |> ignore
    fakeGit systemDir
    fakeGit trashDir
    fakeGit homeDir
    fakeGit nested
    let roots = requireOk "discover" (DailyGitSave.discoverRepoRoots dataDir)
    Assert.Contains(dataDir, roots)
    Assert.Contains(systemDir, roots)
    Assert.Contains(trashDir, roots)
    Assert.Contains(homeDir, roots)
    Assert.DoesNotContain(nested, roots)
    Assert.DoesNotContain(plain, roots)

[<Fact>]
let ``tryRun writes stamp only after a successful empty walk`` () =
    let dataDir = newTempDir ()
    match DailyGitSave.tryRun dataDir utcDay with
    | Ok true -> ()
    | other -> Assert.Fail($"expected walked, got {other}")
    let text = File.ReadAllText(DailyGitSave.stampPath dataDir).Trim()
    Assert.Equal("2026-08-28", text)

[<Fact>]
let ``tryRun skips when today's stamp is already set`` () =
    let dataDir = newTempDir ()
    requireOk "stamp" (DailyGitSave.writeStamp dataDir "2026-08-28")
    match DailyGitSave.tryRun dataDir utcDay with
    | Ok false -> ()
    | other -> Assert.Fail($"expected skip, got {other}")

[<Fact>]
let ``tryRun leaves stamp unset when a repo commit fails`` () =
    let dataDir = newTempDir ()
    fakeGit dataDir
    match DailyGitSave.tryRun dataDir utcDay with
    | Error _ -> ()
    | Ok v -> Assert.Fail($"expected error, got Ok {v}")
    Assert.False(File.Exists(DailyGitSave.stampPath dataDir))

[<Fact>]
let ``start does not wait on git when today's stamp is set`` () =
    let dataDir = newTempDir ()
    requireOk "stamp" (DailyGitSave.writeStamp dataDir "2026-08-28")
    let hung = TaskCompletionSource<unit>()
    let running = DailyGitSave.start dataDir hung.Task utcDay
    Assert.True(running.Wait(TimeSpan.FromSeconds(2.0)))

[<Fact>]
let ``start does not walk until whenReady completes`` () = task {
    let dataDir = newTempDir ()
    let tcs = TaskCompletionSource<unit>()
    let running = DailyGitSave.start dataDir tcs.Task utcDay
    do! Task.Delay(80)
    Assert.False(running.IsCompleted)
    Assert.False(File.Exists(DailyGitSave.stampPath dataDir))
    tcs.SetResult()
    do! running
    Assert.True(File.Exists(DailyGitSave.stampPath dataDir))
}

[<SkippableFact>]
let ``walk commitAll DataDir then immediate child`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    Directory.CreateDirectory home |> ignore
    initRepo dataDir
    initRepo home
    File.WriteAllText(Path.Combine(dataDir, "root.txt"), "r")
    File.WriteAllText(Path.Combine(home, "child.txt"), "c")
    match DailyGitSave.tryRun dataDir utcDay with
    | Ok true -> ()
    | other -> Assert.Fail($"expected walked, got {other}")
    let rootMsg =
        GitSave.runGit dataDir "log -1 --pretty=%s" |> requireOk "root log"
    let childMsg =
        GitSave.runGit home "log -1 --pretty=%s" |> requireOk "child log"
    Assert.Equal(DailyGitSave.commitMessage, rootMsg.Trim())
    Assert.Equal(DailyGitSave.commitMessage, childMsg.Trim())
    match DailyGitSave.tryRun dataDir utcDay with
    | Ok false -> ()
    | other -> Assert.Fail($"expected same-day skip, got {other}")
