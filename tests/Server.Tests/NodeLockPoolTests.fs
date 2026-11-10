module Gambol.Server.Tests.NodeLockPoolTests

open System
open System.Threading
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private assertOk (label: string) (result: Result<unit, NodeLockRejection>) =
    match result with
    | Ok () -> ()
    | Error rejection -> failwith $"{label}: {rejection}"

let private node id text name owner kind =
    Node.Create(
        id,
        text = text,
        name = Filename.create name,
        owner = owner,
        kind = kind)

let private addOwned (parentId: NodeId) (childIds: NodeId list) (graph: Graph) =
    Graph.replace parentId 0 [] (ChildNode.owners childIds) graph
    |> requireOk "replace"

let private poolGraph () =
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let otherId = NodeId.New()
    let siblingId = NodeId.New()

    let graph =
        Graph.create ()
        |> Graph.addDetachedNode (
            node wsId "home" "home" Graph.workspacesId (Special Workspace))
        |> Graph.addDetachedNode (
            node dirId "docs" "docs" wsId (Special Directory))
        |> Graph.addDetachedNode (
            node fileId "note" "note.txt" dirId (Special File))
        |> Graph.addDetachedNode (
            node otherId "other" "other.txt" dirId (Special File))
        |> Graph.addDetachedNode (
            node siblingId "sib" "sib.txt" wsId (Special File))
        |> addOwned Graph.workspacesId [ wsId ]
        |> addOwned wsId [ dirId; siblingId ]
        |> addOwned dirId [ fileId; otherId ]

    graph, wsId, dirId, fileId, otherId, siblingId

let private disagreeGraphs () =
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()

    let bare =
        Graph.create ()
        |> Graph.addDetachedNode (
            node wsId "home" "home" Graph.workspacesId (Special Workspace))
        |> Graph.addDetachedNode (
            node dirId "docs" "docs" wsId (Special Directory))
        |> Graph.addDetachedNode (
            node fileId "note" "note.txt" dirId (Special File))
        |> addOwned Graph.workspacesId [ wsId ]

    let fileUnderDir =
        bare
        |> addOwned wsId [ dirId ]
        |> addOwned dirId [ fileId ]

    let fileBesideDir =
        bare
        |> addOwned wsId [ dirId; fileId ]

    fileUnderDir, fileBesideDir, dirId, fileId

let private stayedOutside
    (queued: int)
    (entered: TaskCompletionSource<unit>)
    =
    (NodeLockPool.waitUntilQueued queued).Wait()
    Assert.False(entered.Task.IsCompleted)
    Assert.True(NodeLockPool.pendingCount () > 0)

let private holdUntil
    (graph: Graph)
    (nodeId: NodeId)
    (release: TaskCompletionSource<unit>)
    (entered: TaskCompletionSource<unit>)
    =
    Task.Run(fun () ->
        NodeLockPool.withLock graph nodeId (fun () ->
            entered.SetResult()
            release.Task.Wait())
        |> assertOk "hold")

let private enter
    (graph: Graph)
    (nodeId: NodeId)
    (entered: TaskCompletionSource<unit>)
    =
    Task.Run(fun () ->
        NodeLockPool.withLock graph nodeId (fun () -> entered.SetResult())
        |> assertOk "enter")

let private enterAfterRelease
    (graph: Graph)
    (nodeId: NodeId)
    (release: TaskCompletionSource<unit>)
    (entered: TaskCompletionSource<unit>)
    =
    Task.Run(fun () ->
        NodeLockPool.withLock graph nodeId (fun () ->
            Assert.True(release.Task.IsCompleted)
            entered.SetResult())
        |> assertOk "enter after release")

let private holdAndReject
    (graph: Graph)
    (fileId: NodeId)
    (release: TaskCompletionSource<unit>)
    (entered: TaskCompletionSource<unit>)
    (nested: TaskCompletionSource<Result<unit, NodeLockRejection>>)
    (absent: TaskCompletionSource<Result<unit, NodeLockRejection>>)
    =
    Task.Run(fun () ->
        NodeLockPool.withLock graph fileId (fun () ->
            entered.SetResult()
            nested.SetResult(NodeLockPool.withLock graph fileId (fun () -> ()))
            let absentId = NodeId.New()

            absent.SetResult(
                NodeLockPool.withLock (Graph.create ()) absentId (fun () -> ()))

            release.Task.Wait()
            failwith "holder raises")
        |> assertOk "holder")

let private assertRequestWaits
    (heldGraph: Graph)
    (heldId: NodeId)
    (requestGraph: Graph)
    (requestId: NodeId)
    =
    let release = TaskCompletionSource<unit>()
    let heldEntered = TaskCompletionSource<unit>()
    let requestEntered = TaskCompletionSource<unit>()
    let held = holdUntil heldGraph heldId release heldEntered
    heldEntered.Task.Wait()
    let request = enter requestGraph requestId requestEntered
    (NodeLockPool.waitUntilQueued 2).Wait()
    Assert.False(requestEntered.Task.IsCompleted)
    Assert.Equal(1, NodeLockPool.pendingCount ())
    release.SetResult()
    requestEntered.Task.Wait()
    Task.WaitAll(held, request)
    Assert.Equal(0, NodeLockPool.entryCount ())

let private waitFault (holder: Task) =
    try
        holder.Wait()
        false
    with
    | :? AggregateException -> true

[<Fact>]
let ``a request waits when the graphs disagree on a held file`` () =
    let fileUnderDir, fileBesideDir, dirId, fileId = disagreeGraphs ()
    assertRequestWaits fileBesideDir fileId fileUnderDir dirId
    assertRequestWaits fileUnderDir dirId fileBesideDir fileId

[<Fact>]
let ``a thrown wait drops the pending entry`` () =
    let graph, _, _, fileId, _, _ = poolGraph ()
    let release = TaskCompletionSource<unit>()
    let holderEntered = TaskCompletionSource<unit>()
    let worker = TaskCompletionSource<Thread>()
    let holder = holdUntil graph fileId release holderEntered
    holderEntered.Task.Wait()

    let waiter =
        Task.Run(fun () ->
            worker.SetResult(Thread.CurrentThread)

            NodeLockPool.withLock graph fileId (fun () -> ())
            |> assertOk "waiter")

    try
        let thread = worker.Task.Result
        (NodeLockPool.waitUntilQueued 2).Wait()
        thread.Interrupt()
        Assert.True(waitFault waiter)
        Assert.Equal(1, NodeLockPool.entryCount ())
        Assert.Equal(0, NodeLockPool.pendingCount ())
    finally
        release.TrySetResult() |> ignore
        holder.Wait()

    Assert.Equal(0, NodeLockPool.entryCount ())

[<Fact>]
let ``taking a directory waits while a descendant file is held and a sibling file enters``
    ()
    =
    let graph, _, dirId, fileId, _, siblingId = poolGraph ()
    let releaseFile = TaskCompletionSource<unit>()
    let fileEntered = TaskCompletionSource<unit>()
    let dirEntered = TaskCompletionSource<unit>()
    let siblingEntered = TaskCompletionSource<unit>()
    let fileHold = holdUntil graph fileId releaseFile fileEntered
    fileEntered.Task.Wait()
    let dirWait = enter graph dirId dirEntered
    stayedOutside 2 dirEntered
    let sibling = enter graph siblingId siblingEntered
    sibling.Wait()
    Assert.True(siblingEntered.Task.IsCompleted)
    stayedOutside 2 dirEntered
    releaseFile.SetResult()
    dirEntered.Task.Wait()
    Task.WaitAll(fileHold, dirWait)

[<Fact>]
let ``taking a workspace waits while a descendant file is held and a file waits while an ancestor is held``
    ()
    =
    let graph, wsId, dirId, fileId, _, siblingId = poolGraph ()
    let releaseSibling = TaskCompletionSource<unit>()
    let siblingEntered = TaskCompletionSource<unit>()
    let workspaceEntered = TaskCompletionSource<unit>()
    let siblingHold = holdUntil graph siblingId releaseSibling siblingEntered
    siblingEntered.Task.Wait()
    let workspaceWait = enter graph wsId workspaceEntered
    stayedOutside 2 workspaceEntered
    releaseSibling.SetResult()
    workspaceEntered.Task.Wait()
    Task.WaitAll(siblingHold, workspaceWait)
    let releaseDir = TaskCompletionSource<unit>()
    let dirEntered = TaskCompletionSource<unit>()
    let childEntered = TaskCompletionSource<unit>()
    let dirHold = holdUntil graph dirId releaseDir dirEntered
    dirEntered.Task.Wait()
    let childWait = enter graph fileId childEntered
    stayedOutside 2 childEntered
    releaseDir.SetResult()
    childEntered.Task.Wait()
    Task.WaitAll(dirHold, childWait)

[<Fact>]
let ``a second request on a held node waits and the same thread is rejected`` () =
    let graph, _, _, fileId, _, _ = poolGraph ()
    let release = TaskCompletionSource<unit>()
    let holderEntered = TaskCompletionSource<unit>()
    let waiterEntered = TaskCompletionSource<unit>()
    let nested = TaskCompletionSource<Result<unit, NodeLockRejection>>()
    let absent = TaskCompletionSource<Result<unit, NodeLockRejection>>()

    let holder =
        holdAndReject graph fileId release holderEntered nested absent

    holderEntered.Task.Wait()
    Assert.Equal(Error NodeLockRejection.HolderHoldsLock, nested.Task.Result)
    Assert.Equal(Error NodeLockRejection.NodeAbsent, absent.Task.Result)
    let waiter = enter graph fileId waiterEntered
    stayedOutside 2 waiterEntered
    release.SetResult()
    waiterEntered.Task.Wait()
    waiter.Wait()
    Assert.True(waitFault holder)

[<Fact>]
let ``a pending directory lock runs before a new descendant file`` () =
    let graph, _, dirId, fileId, otherId, siblingId = poolGraph ()
    let releaseFile = TaskCompletionSource<unit>()
    let releaseDir = TaskCompletionSource<unit>()
    let fileEntered = TaskCompletionSource<unit>()
    let dirEntered = TaskCompletionSource<unit>()
    let otherEntered = TaskCompletionSource<unit>()
    let siblingEntered = TaskCompletionSource<unit>()
    let fileHold = holdUntil graph fileId releaseFile fileEntered
    fileEntered.Task.Wait()
    let dirHold = holdUntil graph dirId releaseDir dirEntered
    stayedOutside 2 dirEntered
    let otherWait = enter graph otherId otherEntered
    stayedOutside 3 otherEntered
    let sibling = enter graph siblingId siblingEntered
    sibling.Wait()
    Assert.True(siblingEntered.Task.IsCompleted)
    stayedOutside 3 dirEntered
    stayedOutside 3 otherEntered
    releaseFile.SetResult()
    dirEntered.Task.Wait()
    stayedOutside 2 otherEntered
    releaseDir.SetResult()
    otherEntered.Task.Wait()
    Task.WaitAll(fileHold, dirHold, otherWait)

[<Fact>]
let ``a parse read under a locked directory waits until release`` () =
    let graph, _, dirId, fileId, _, siblingId = poolGraph ()
    let releaseDir = TaskCompletionSource<unit>()
    let dirEntered = TaskCompletionSource<unit>()
    let parseEntered = TaskCompletionSource<unit>()
    let siblingEntered = TaskCompletionSource<unit>()
    let dirHold = holdUntil graph dirId releaseDir dirEntered
    dirEntered.Task.Wait()
    let parseWait = enterAfterRelease graph fileId releaseDir parseEntered
    stayedOutside 2 parseEntered
    let sibling = enter graph siblingId siblingEntered
    sibling.Wait()
    Assert.True(siblingEntered.Task.IsCompleted)
    stayedOutside 2 parseEntered
    releaseDir.SetResult()
    parseEntered.Task.Wait()
    Task.WaitAll(dirHold, parseWait)
    let releaseParse = TaskCompletionSource<unit>()
    let parseHeld = TaskCompletionSource<unit>()
    let dirWaits = TaskCompletionSource<unit>()
    let siblingAgain = TaskCompletionSource<unit>()
    let parseHold = holdUntil graph fileId releaseParse parseHeld
    parseHeld.Task.Wait()
    let dirWait = enterAfterRelease graph dirId releaseParse dirWaits
    stayedOutside 2 dirWaits
    let siblingDuringParse = enter graph siblingId siblingAgain
    siblingDuringParse.Wait()
    Assert.True(siblingAgain.Task.IsCompleted)
    stayedOutside 2 dirWaits
    releaseParse.SetResult()
    dirWaits.Task.Wait()
    Task.WaitAll(parseHold, dirWait)
