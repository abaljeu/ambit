module Gambol.Server.Tests.NodeEnclosureLockTests

open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private owned = ChildNode.owners

let private enclosureGraph () : Graph * NodeId * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let siblingId = NodeId.New()
    let wsNode =
        Node.Create(
            wsId,
            text = "home",
            name = Filename.create "home",
            owner = Graph.workspacesId,
            kind = Special Workspace)
    let dirNode =
        Node.Create(
            dirId,
            text = "docs",
            name = Filename.create "docs",
            owner = wsId,
            kind = Special Directory)
    let fileNode =
        Node.Create(
            fileId,
            text = "note",
            name = Filename.create "note.txt",
            owner = dirId,
            kind = Special File)
    let siblingNode =
        Node.Create(
            siblingId,
            text = "sib",
            name = Filename.create "sib.txt",
            owner = wsId,
            kind = Special File)
    let graph1 = Graph.addDetachedNode wsNode graph0
    let graph2 = Graph.addDetachedNode dirNode graph1
    let graph3 = Graph.addDetachedNode fileNode graph2
    let graph4 = Graph.addDetachedNode siblingNode graph3
    let graph5 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph4
        |> requireOk "workspaces->ws"
    let graph6 =
        Graph.replace wsId 0 [] (owned [ dirId; siblingId ]) graph5
        |> requireOk "ws->dir-and-sibling"
    let graph7 =
        Graph.replace dirId 0 [] (owned [ fileId ]) graph6
        |> requireOk "dir->file"
    graph7, wsId, dirId, fileId, siblingId

let private indexOf (id: NodeId) (nodes: NodeId list) =
    List.findIndex (fun candidate -> candidate = id) nodes

let private isDirectory (node: Node) =
    match node.kind with
    | Special Directory -> true
    | _ -> false

[<Fact>]
let ``directory lock blocks a descendant, allows a sibling, and a workspace lock blocks its file``
    ()
    =
    let graph, wsId, dirId, fileId, siblingId = enclosureGraph ()
    let releaseDir = TaskCompletionSource<unit>()
    let dirEntered = TaskCompletionSource<unit>()
    let childEntered = TaskCompletionSource<unit>()
    let childAttempted = TaskCompletionSource<unit>()
    let dirLock =
        Task.Run(fun () ->
            NodeEnclosureLock.withNodeLock dirId (fun () ->
                dirEntered.SetResult()
                releaseDir.Task.Wait()))
    dirEntered.Task.Wait()
    let child =
        Task.Run(fun () ->
            childAttempted.SetResult()
            NodeEnclosureLock.withPersist graph fileId (fun _ ->
                childEntered.SetResult()))
    childAttempted.Task.Wait()
    let sibling =
        Task.Run(fun () ->
            NodeEnclosureLock.withPersist graph siblingId (fun _ -> ()))
    sibling.Wait()
    Assert.False(childEntered.Task.Wait(100))
    releaseDir.SetResult()
    childEntered.Task.Wait()
    dirLock.Wait()
    child.Wait()

    let releaseWs = TaskCompletionSource<unit>()
    let wsEntered = TaskCompletionSource<unit>()
    let siblingEntered = TaskCompletionSource<unit>()
    let siblingAttempted = TaskCompletionSource<unit>()
    let wsLock =
        Task.Run(fun () ->
            NodeEnclosureLock.withNodeLock wsId (fun () ->
                wsEntered.SetResult()
                releaseWs.Task.Wait()))
    wsEntered.Task.Wait()
    let siblingWait =
        Task.Run(fun () ->
            siblingAttempted.SetResult()
            NodeEnclosureLock.withPersist graph siblingId (fun _ ->
                siblingEntered.SetResult()))
    siblingAttempted.Task.Wait()
    Assert.False(siblingEntered.Task.Wait(100))
    releaseWs.SetResult()
    siblingEntered.Task.Wait()
    Task.WaitAll(wsLock, siblingWait)

[<Fact>]
let ``node enclosure acquire drains an in-flight persist and a parse read`` () =
    let graph, _, dirId, fileId, _ = enclosureGraph ()
    let releasePersist = TaskCompletionSource<unit>()
    let releaseParse = TaskCompletionSource<unit>()
    let releaseLock = TaskCompletionSource<unit>()
    let persistEntered = TaskCompletionSource<NodeId list>()
    let parseEntered = TaskCompletionSource<unit>()
    let lockEntered = TaskCompletionSource<unit>()
    let lateEntered = TaskCompletionSource<unit>()
    let lateAttempted = TaskCompletionSource<unit>()
    let persist =
        Task.Run(fun () ->
            NodeEnclosureLock.withPersist graph fileId (fun ids ->
                persistEntered.SetResult ids
                releasePersist.Task.Wait()))
    let parseRead =
        Task.Run(fun () ->
            NodeEnclosureLock.withParseRead graph fileId (fun () ->
                parseEntered.SetResult()
                releaseParse.Task.Wait()))
    let stored = persistEntered.Task.Result
    Assert.Contains(dirId, stored)
    parseEntered.Task.Wait()
    let holder =
        Task.Run(fun () ->
            NodeEnclosureLock.withNodeLock dirId (fun () ->
                lockEntered.SetResult()
                releaseLock.Task.Wait()))
    Assert.False(lockEntered.Task.Wait(100))
    let late =
        Task.Run(fun () ->
            lateAttempted.SetResult()
            NodeEnclosureLock.withPersist graph fileId (fun _ ->
                lateEntered.SetResult()))
    lateAttempted.Task.Wait()
    Assert.False(lateEntered.Task.Wait(100))
    releasePersist.SetResult()
    Assert.False(lockEntered.Task.Wait(100))
    releaseParse.SetResult()
    lockEntered.Task.Wait()
    Assert.False(lateEntered.Task.Wait(100))
    releaseLock.SetResult()
    lateEntered.Task.Wait()
    Task.WaitAll(persist, parseRead, holder, late)

[<Fact>]
let ``persist grant stores the owner chain and includes the directory node`` () =
    let graph, _, dirId, fileId, _ = enclosureGraph ()
    Assert.Equal(Some dirId, GraphQuery.enclosing graph isDirectory fileId)
    let fileChain = NodeEnclosureLock.withPersist graph fileId id
    Assert.Contains(fileId, fileChain)
    Assert.Contains(dirId, fileChain)
    Assert.True(indexOf fileId fileChain < indexOf dirId fileChain)
    let dirChain = NodeEnclosureLock.withPersist graph dirId id
    Assert.Contains(dirId, dirChain)
