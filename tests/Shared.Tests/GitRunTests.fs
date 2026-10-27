module GitRunTests

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO
open System.Threading
open Gambol.Shared
open Xunit

let private requireGit (dir: string) (arguments: string) =
    match GitRun.gitExec dir arguments with
    | Ok _ -> ()
    | Error err -> Assert.Fail(err)

let private holdLimit = TimeSpan.FromSeconds(30.0)

let private enqueueGit
    (outcomes: ConcurrentQueue<Result<string, string>>)
    (workDir: string)
    (arguments: string)
    =
    try
        outcomes.Enqueue(GitRun.gitExec workDir arguments)
    with ex ->
        outcomes.Enqueue(Error ex.Message)

let private startGit
    (outcomes: ConcurrentQueue<Result<string, string>>)
    (workDir: string)
    (arguments: string)
    =
    let thread =
        Thread(fun () -> enqueueGit outcomes workDir arguments)
    thread.IsBackground <- true
    thread.Start()
    thread

let private peekOutcome
    (outcomes: ConcurrentQueue<Result<string, string>>)
    =
    match outcomes.TryPeek() with
    | true, value -> Some value
    | false, _ -> None

let private failOutcome (prefix: string) (outcome: Result<string, string> option) =
    match outcome with
    | Some (Error err) -> Assert.Fail(prefix + err)
    | Some (Ok _) -> ()
    | None -> Assert.Fail(prefix + "produced no result")

let private requirePending
    (prefix: string)
    (outcome: Result<string, string> option)
    =
    match outcome with
    | Some (Error err) -> Assert.Fail(prefix + err)
    | Some (Ok _) -> Assert.Fail(prefix + "succeeded before index.lock existed")
    | None -> ()

let private waitForHeldLock
    (lockPath: string)
    (first: ConcurrentQueue<Result<string, string>>)
    =
    let sw = Stopwatch.StartNew()
    let rec loop () =
        if File.Exists lockPath then
            ()
        elif peekOutcome first |> Option.isSome then
            requirePending
                "add finished before index.lock existed: "
                (peekOutcome first)
            loop ()
        elif sw.Elapsed > holdLimit then
            Assert.Fail("add did not create index.lock")
        else
            Thread.Sleep 15
            loop ()
    loop ()

let private blockedOnGate (thread: Thread) =
    thread.IsAlive
    && thread.ThreadState.HasFlag ThreadState.WaitSleepJoin

/// The second call is inside GitRun's work-tree gate when this returns.
/// The gate blocks that thread in WaitSleepJoin until the first git finishes.
let private waitUntilSecondBlocks
    (thread: Thread)
    (outcomes: ConcurrentQueue<Result<string, string>>)
    =
    let sw = Stopwatch.StartNew()
    let rec loop (sawBlocked: bool) =
        match peekOutcome outcomes with
        | Some (Error err) -> Assert.Fail(err)
        | Some (Ok _) ->
            Assert.Fail("second git finished while index.lock was held")
        | None when blockedOnGate thread ->
            if sawBlocked then
                ()
            else
                Thread.Sleep 15
                loop true
        | None when not thread.IsAlive ->
            Assert.Fail("second git ended without a result")
        | None when sw.Elapsed > holdLimit ->
            Assert.Fail("second git did not wait for the work tree")
        | None ->
            Thread.Sleep 15
            loop false
    loop false

let private writeHoldScript (path: string) =
    File.WriteAllText(
        path,
        "#!/bin/sh\nwhile [ ! -f \"$1\" ]; do\n    sleep 0.02\ndone\ncat\n")

let private tryJoin (thread: Thread) =
    thread.Join holdLimit

[<Fact>]
let ``second git waits instead of failing on index.lock`` () =
    let root =
        Path.Combine(
            Path.GetTempPath(),
            "gambol-git-gate-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(root) |> ignore
    let release = Path.Combine(root, "release-clean")
    let script = Path.Combine(root, "hold-clean.sh")
    writeHoldScript script
    let note = Path.Combine(root, "note.txt")
    let first = ConcurrentQueue<Result<string, string>>()
    let second = ConcurrentQueue<Result<string, string>>()
    let threads = ResizeArray<Thread>()
    let releaseHold () =
        if not (File.Exists release) then
            File.WriteAllText(release, "go")
    try
        requireGit root "init -b main"
        File.WriteAllText(note, "one")
        requireGit root "add note.txt"
        requireGit root
            "-c user.email=gambol@test -c user.name=gambol commit -m first"
        requireGit root
            ("config filter.slow.clean \"sh "
             + script
             + " "
             + release
             + "\"")
        File.WriteAllText(
            Path.Combine(root, ".gitattributes"),
            "note.txt filter=slow\n")
        File.WriteAllText(note, "two")
        File.WriteAllText(Path.Combine(root, "other.txt"), "o")
        let lockPath = Path.Combine(root, ".git", "index.lock")
        let firstThread = startGit first root "add note.txt"
        threads.Add firstThread
        waitForHeldLock lockPath first
        Assert.True(File.Exists lockPath, "add did not create index.lock")
        let secondThread = startGit second root "add other.txt"
        threads.Add secondThread
        waitUntilSecondBlocks secondThread second
        Assert.True(
            File.Exists lockPath,
            "index.lock was not held while the second git waited")
        releaseHold ()
        if not (tryJoin firstThread) then
            Assert.Fail("first git did not finish")
        if not (tryJoin secondThread) then
            Assert.Fail("second git did not finish")
        failOutcome "" (peekOutcome first)
        failOutcome "" (peekOutcome second)
    finally
        try releaseHold () with _ -> ()
        for thread in threads do
            try thread.Join holdLimit |> ignore with _ -> ()
        try Directory.Delete(root, true) with _ -> ()
