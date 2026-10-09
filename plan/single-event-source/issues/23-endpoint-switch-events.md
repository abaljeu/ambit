# 23 — Endpoint switch

**Status:** `defined`
**Type:** coding

**Binding arch:** [single-event-source architecture](../arch.md) (Event source order: the post sentence).

## Context

The ordered list has two doors and one handler. The client posts to `/ambit/changes`. This ticket posts that list to `/ambit/events` and removes `/ambit/changes`. The handler stays `Api.postEvents`. One fix. The list, the order, and the credential check stay as they are.

[22 — One ordered event stream](22-ordered-event-stream.md) is `coded`. It posts the pending queue, including [RunLaunch.queueStart](../../../src/Shared/RunLaunch.fs) lists, to `/ambit/changes`.

## Current state

Both routes call `Api.postEvents`.

1. **Changes route** — [RouteRegistration.fs](../../../src/Server/RouteRegistration.fs) line 295 maps `POST /ambit/changes`.
2. **Events route** — The same file, line 309, maps `POST /ambit/events` to the same handler.
3. **Log** — [HttpResponseLog.fs](../../../src/Server/HttpResponseLog.fs) line 39 lists `/ambit/changes`. Line 40 lists `/ambit/events`.

Client callers. Each one builds a `/changes` URL and posts the ordered list.

1. **Pending queue** — [App.fs](../../../src/Client/App.fs) `runSubmitPendingBatch` line 269. `SubmitPendingBatch` at line 89 calls it. [RunLaunch.queueStart](../../../src/Shared/RunLaunch.fs) line 16 enqueues ActorStart onto `syncInfo.pending` from [Commands.fs](../../../src/Client/Commands.fs) line 114. That list leaves through line 269. `queueCancel` uses the same post.
2. **Upload structure** — [App.fs](../../../src/Client/App.fs) line 113, `ContinuePostUploadStructure`.
3. **Sync post** — [UpdateWorkspaceSync.fs](../../../src/Client/UpdateWorkspaceSync.fs) `applyAndPostSync` line 94.
4. **Desktop session** — [AmbitSession.fs](../../../src/Shared/dotnet/AmbitSession.fs) `postOps` line 162.

## What to build

Every caller posts `/events`. The `/ambit/changes` route and its log entry go away.

1. [ ] Pending queue — `runSubmitPendingBatch` posts `/{file}/events`.
2. [ ] Upload structure — line 113 posts `/{file}/events`.
3. [ ] Sync post — `applyAndPostSync` posts `/{file}/events`.
4. [ ] Desktop session — `postOps` posts `/events`. The error text names that path.
5. [ ] Delete the route — Remove the `MapPost` at [RouteRegistration.fs](../../../src/Server/RouteRegistration.fs) line 295. The line 309 route stays.
6. [ ] Delete the log entry — Remove `"/ambit/changes"` at [HttpResponseLog.fs](../../../src/Server/HttpResponseLog.fs) line 39. Keep `"/ambit/events"`.
7. [ ] Doc — [api.md](../../../doc/current/api.md) line 21 names `POST /ambit/events` as the events door.
8. [ ] Arch sentence — The post sentence in [single-event-source architecture](../arch.md) names `POST /ambit/events`.
9. [ ] Tests — Point each post below at `/ambit/events`. [TestActorCommandErrorTests.fs](../../../tests/Server.Tests/TestActorCommandErrorTests.fs) line 339. [LazyLoadReconciliationServerTests.fs](../../../tests/Server.Tests/LazyLoadReconciliationServerTests.fs) lines 747, 775, and 810. [ChangeEndpointResilienceTests.fs](../../../tests/Server.Tests/ChangeEndpointResilienceTests.fs) lines 96, 101, 137, 158, and 169. [BrowserCredentialTests.fs](../../../tests/Server.Tests/BrowserCredentialTests.fs) lines 51, 72, 96, and 155. [ApiPostCommandTests.fs](../../../tests/Server.Tests/ApiPostCommandTests.fs) line 201. [HttpResponseLogTests.fs](../../../tests/Server.Tests/HttpResponseLogTests.fs) lines 61, 69, 82, and 181.
10. [ ] Test — A pending batch that contains a `RunLaunch.queueStart` list posts to `POST /ambit/events`. `POST /ambit/changes` is absent.
