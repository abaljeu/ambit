module Gambol.Shared.Tests.WorkspaceUploadTests

open Xunit
open Gambol.Shared
open Gambol.Shared.CommandEntry

let private fileId = NodeId.New()
let private wsId = NodeId.New()
let private dirId = NodeId.New()

[<Fact>]
let ``web Upload on file plans ParseServerDisk`` () =
    Assert.Equal(
        WorkspaceUploadAction.ParseServerDisk fileId,
        WorkspaceUpload.plan false false (Some(ParseFile fileId)))

[<Fact>]
let ``web Upload on directory plans ReconcileServerDisk`` () =
    Assert.Equal(
        WorkspaceUploadAction.ReconcileServerDisk,
        WorkspaceUpload.plan false false (Some(ReconcileDirectory dirId)))

[<Fact>]
let ``web Upload on named workspace plans ReconcileServerDisk`` () =
    Assert.Equal(
        WorkspaceUploadAction.ReconcileServerDisk,
        WorkspaceUpload.plan false false (Some(ReconcileWorkspace wsId)))

[<Fact>]
let ``desktop Upload on file plans DesktopPush with parse`` () =
    Assert.Equal(
        WorkspaceUploadAction.DesktopPush(Some fileId),
        WorkspaceUpload.plan true false (Some(ParseFile fileId)))

[<Fact>]
let ``desktop Upload on directory plans DesktopPush without parse`` () =
    Assert.Equal(
        WorkspaceUploadAction.DesktopPush None,
        WorkspaceUpload.plan true false (Some(ReconcileDirectory dirId)))

[<Fact>]
let ``Workspaces Upload requires desktop`` () =
    Assert.Equal(
        WorkspaceUploadAction.CreateWorkspaceFromFolder,
        WorkspaceUpload.plan true true None)
    match WorkspaceUpload.plan false true None with
    | WorkspaceUploadAction.Unavailable _ -> ()
    | other -> Assert.Fail($"expected Unavailable, got {other}")

[<Fact>]
let ``Upload available on web for file and directory`` () =
    Assert.True(
        WorkspaceUpload.isAvailable false false (Some(ParseFile fileId)))
    Assert.True(
        WorkspaceUpload.isAvailable
            false
            false
            (Some(ReconcileDirectory dirId)))
    Assert.False(WorkspaceUpload.isAvailable false true None)
    Assert.True(WorkspaceUpload.isAvailable true true None)

[<Fact>]
let ``Upload cannot start from revision 14706 while its prior submit is in flight`` () =
    let pending =
        { id = 14706
          changeId = System.Guid.NewGuid()
          ops = [] }
    let syncInfo =
        { SyncInfo.initial with
            syncState = Sending 1
            pendingChanges = [ pending ] }
    Assert.False(WorkspaceUpload.canStart syncInfo)
    Assert.True(WorkspaceUpload.canStart SyncInfo.initial)

[<Fact>]
let ``one Upload sequences all parse phases instead of racing revision 14706`` () =
    let request id =
        Effect.ContinueParseFile(id, None, "parsed: ", "file.md")
    let first = request (NodeId.New())
    let second = request (NodeId.New())
    match WorkspaceUpload.sequenceParseEffects [ first; second ] with
    | [ Effect.ContinueUploadParses [ queuedFirst; queuedSecond ] ] ->
        Assert.Equal(first, queuedFirst)
        Assert.Equal(second, queuedSecond)
    | other -> Assert.Fail($"expected one sequential parse queue, got {other}")
