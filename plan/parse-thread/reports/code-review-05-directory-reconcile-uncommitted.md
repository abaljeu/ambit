6# Code review — Directory reconcile

Range: uncommitted changes against HEAD. Product change: [05 — Directory reconcile](plan/parse-thread/issues/05-directory-reconcile.md).

## Standards

**Hard violation.** [F# source](.agents/rules/fsharp-source.md) forbids a whole-structure rebuild once per item. A per-item `Graph.fromNodes` is quadratic across a batch. Cost the change for 10,000 ops.

[DirectoryReconcile.createMissing](src/Shared/dotnet/DirectoryReconcile.fs) plans and applies one full-list `Op.Replace` for each missing file. [GraphMutate.replace](src/Shared/GraphMutate.fs) calls `GraphBuild.fromNodes` when `isAppend` is false. `isAppend` stays false for an end-append onto a child list that already has children. The parse thread posts that op list and rebuilds the graph again. `unusedOwnedName` also walks `ownedArtifactsInDirectory` once per name.

## Spec

(b) [History](src/Shared/History.fs) sends every `Op.Replace` through [DocumentPartition.replaceBlockedByInaccessible](src/Shared/DocumentPartition.fs). An inaccessible outline member no longer blocks a Replace when that member id stays in both child lists. [05 — Directory reconcile](plan/parse-thread/issues/05-directory-reconcile.md) asks for create ops that append File Nodes under the Directory Node. This change also loosens Replace for every other caller.

(c) A disk-newer File Node comes back as `Op.SetDocumentState` to `DocumentState.Unparsed`. That op does not write `parseState`. A node that became Current through ParseFinished stays `ParseState.Parsed`. [ParseThread](src/Server/ParseThread.fs) skips a popped node in that state. The named push does not parse the newer disk. `SetDocumentState` also writes `updateTime`. A later reconcile can miss the same file.

(d) `DirectoryReconcile.Input` is `dataDir`, `graph`, and `directoryId`. The ticket names the inputs as a Directory Node and the Body. The Body is absent from that record. `DirectoryReconcile.Output` is `ops` and `push`. That output matches the ops and the push names the ticket asks to return.

Standards 1 (worst: per-item `Graph.fromNodes` in `createMissing`). Spec 3 (worst: disk-newer `SetDocumentState` leaves `parseState` Parsed, so the parse thread skips the pushed file).
