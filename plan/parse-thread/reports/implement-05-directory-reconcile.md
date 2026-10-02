# Implement 05 — Directory reconcile

1. **Result** — [05 — Directory reconcile](../issues/05-directory-reconcile.md) is implemented. Directory reconcile and Workspace reconcile are the same scan. The parse thread calls it when the popped node is a Directory Node or a Workspace Node. On finish it calls `finishParse`, which is [Core mailbox](../../../src/Server/Core/CoreMailbox.fs) `addInMsg` of InMsg ParseFinished. The thread does not edit the graph axes. No commit.

2. **Files**
   1. **Directory reconcile** — [DirectoryReconcile.fs](../../../src/Shared/dotnet/DirectoryReconcile.fs) and [Gambol.Shared.DotNet.fsproj](../../../src/Shared/dotnet/Gambol.Shared.DotNet.fsproj). `planDirectoryReconcile` takes `dataDir`, the graph, and the directory id. The body is `DocumentPartition.memberNodeIds` for that node. The scan lists files in that directory and does not read the Directory File. Missing files become File Nodes through `FileNodeOps.planCreateOwnedFile`, appended in alphabetical order. A disk-newer File Node is returned Unparsed and named on `push`.
   2. **Parse thread** — [ParseThread.fs](../../../src/Server/ParseThread.fs), [RouteRegistration.fs](../../../src/Server/RouteRegistration.fs), and [ParseThreadLoadTests.fs](../../../tests/Server.Tests/ParseThreadLoadTests.fs). A Directory Node or Workspace Node runs Directory reconcile, posts the ops, pushes the named File Nodes, then `finishParse`. A File Node still uses `planParseFile`. A node whose parse state is Parsed is skipped.
   3. **Apply guard** — [DocumentPartition.fs](../../../src/Shared/DocumentPartition.fs) `replaceBlockedByInaccessible`, called from [History.fs](../../../src/Shared/History.fs). An Unparsed Directory or Workspace can gain a File Node while its outline siblings stay. That is what lets the create ops apply.
   4. **Tests** — [DirectoryReconcileTests.fs](../../../tests/Shared.Tests/DirectoryReconcileTests.fs) and [Gambol.Shared.Tests.fsproj](../../../tests/Shared.Tests/Gambol.Shared.Tests.fsproj).
   5. **Plan** — [project.md](../project.md) Stage `build`. [arch.md](../arch.md) Directory reconcile hops checked. [spec.md](../spec.md) coding note. [05 — Directory reconcile](../issues/05-directory-reconcile.md) Status `coded`.

3. **Verification**
   1. **Shared tests** — `dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~DirectoryReconcileTests|FullyQualifiedName~HistoryTests"`. Passed: 67. Failed: 0.
   2. **Parse thread tests** — `dotnet test tests/Server.Tests/Gambol.Server.Tests.fsproj -c Debug --filter "FullyQualifiedName~ParseThreadLoadTests" -o D:/AppData/Local/Temp/gambol-server-tests`. Passed: 4. Failed: 0. The first run could not copy into `src/Server/bin` because `netcoredbg.exe` held those files. The temp output run compiled and passed.
   3. **Client compile gate** — Not run. The Client project does not reference Shared dotnet or Server.

4. **Status** — Ticket **Status:** `coded`. Project **Stage:** `build`. **Finished:** not set. Project **Actual:** `1h 40m`. Ticket **Actual:** `1.5h`.

5. **Acceptance** — Every checklist item on [05 — Directory reconcile](../issues/05-directory-reconcile.md) is checked. Nothing on that list was left unfinished. New File Nodes are not named for push. The ticket names push for the disk-newer File Node.

6. **Next step** — Review [05 — Directory reconcile](../issues/05-directory-reconcile.md). Review sets `done`. This session did not.
