module Gambol.Server.Tests.NodeLockPoolTests

open System
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

let private stayedOutside (entered: TaskCompletionSource<unit>) =
    Assert.False(entered.Task.Wait(100))

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

let private waitFault (holder: Task) =
    try
        holder.Wait()
        false
    with
    | :? AggregateException -> true

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
    stayedOutside dirEntered
    let sibling = enter graph siblingId siblingEntered
    sibling.Wait()
    Assert.True(siblingEntered.Task.IsCompleted)
    stayedOutside dirEntered
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
    stayedOutside workspaceEntered
    releaseSibling.SetResult()
    workspaceEntered.Task.Wait()
    Task.WaitAll(siblingHold, workspaceWait)
    let releaseDir = TaskCompletionSource<unit>()
    let dirEntered = TaskCompletionSource<unit>()
    let childEntered = TaskCompletionSource<unit>()
    let dirHold = holdUntil graph dirId releaseDir dirEntered
    dirEntered.Task.Wait()
    let childWait = enter graph fileId childEntered
    stayedOutside childEntered
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
    stayedOutside waiterEntered
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
    stayedOutside dirEntered
    let otherWait = enter graph otherId otherEntered
    stayedOutside otherEntered
    let sibling = enter graph siblingId siblingEntered
    sibling.Wait()
    Assert.True(siblingEntered.Task.IsCompleted)
    stayedOutside dirEntered
    stayedOutside otherEntered
    releaseFile.SetResult()
    dirEntered.Task.Wait()
    stayedOutside otherEntered
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
    stayedOutside parseEntered
    let sibling = enter graph siblingId siblingEntered
    sibling.Wait()
    Assert.True(siblingEntered.Task.IsCompleted)
    stayedOutside parseEntered
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
    stayedOutside dirWaits
    let siblingDuringParse = enter graph siblingId siblingAgain
    siblingDuringParse.Wait()
    Assert.True(siblingAgain.Task.IsCompleted)
    stayedOutside dirWaits
    releaseParse.SetResult()
    dirWaits.Task.Wait()
    Task.WaitAll(parseHold, dirWait)
