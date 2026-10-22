module GitRunTests

open System
open System.Diagnostics
open System.IO
open System.Threading
open System.Threading.Tasks
open Gambol.Shared
open Xunit

let private requireGit (dir: string) (arguments: string) =
    match GitRun.gitExec dir arguments with
    | Ok _ -> ()
    | Error err -> Assert.Fail(err)

let rec private waitForLock (path: string) (left: int) =
    if File.Exists path then true
    elif left <= 0 then false
    else
        Thread.Sleep 15
        waitForLock path (left - 1)

[<Fact>]
let ``second git waits instead of failing on index.lock`` () =
    let root =
        Path.Combine(
            Path.GetTempPath(),
            "gambol-git-gate-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(root) |> ignore
    let note = Path.Combine(root, "note.txt")
    requireGit root "init -b main"
    File.WriteAllText(note, "one")
    requireGit root "add note.txt"
    requireGit root
        "-c user.email=gambol@test -c user.name=gambol commit -m first"
    requireGit root "config filter.slow.clean \"sleep 2\""
    File.WriteAllText(
        Path.Combine(root, ".gitattributes"),
        "note.txt filter=slow\n")
    File.WriteAllText(note, "two")
    File.WriteAllText(Path.Combine(root, "other.txt"), "o")
    let addTask =
        Task.Run(fun () -> GitRun.gitExec root "add note.txt")
    try
        let lockPath = Path.Combine(root, ".git", "index.lock")
        Assert.True(
            waitForLock lockPath 200,
            "add did not create index.lock")
        let sw = Stopwatch.StartNew()
        let second = GitRun.gitExec root "add other.txt"
        sw.Stop()
        match second with
        | Error err -> Assert.Fail(err)
        | Ok _ ->
            Assert.True(
                sw.ElapsedMilliseconds >= 500L,
                "second git did not wait for the work tree")
    finally
        try addTask.Wait() with _ -> ()
        try Directory.Delete(root, true) with _ -> ()
    match addTask.GetAwaiter().GetResult() with
    | Error err -> Assert.Fail(err)
    | Ok _ -> ()
