# 135 — Core inventory: core vs non-core, and Notice posters

Static read of staging at `db80420d` (before #413, which deletes `Api.applyParseFile` and the parse endpoint). Rule: only the core may issue Notices to the mailbox.


Rule under test: only the core (code running inside a mailbox handler on the mailbox thread) may issue Notices.
Queue entry for Notices: `CoreMailboxApi.postNotice host n` → `MailboxHost.postItem (CoreQueue.Notice n)` (CoreMailboxApi.fs:19).
Handled by `CoreMailboxLoop.dispatchItem` → `CoreEventDispatch.logNotice` + `CoreMailboxLoop.applyNotice` (→ CoreMailboxFilesAxis).
Access control today: `postNotice` is `internal` (whole Gambol.Server assembly + InternalsVisibleTo tests); `CoreChangesDoor.host` is a public record field, so any holder of a door can post Notices.

## 1. Core files (walk from CoreMailboxLoop, direct refs only, bounded by fsproj order)

| File | Role |
|---|---|
| Core/CoreMailboxLoop.fs | Mailbox pump; `dispatchItem`/`runMsg`/`dispatchAsk`; `applyNotice`; makeMailBox/start |
| Core/CoreMailboxEvents.fs | Handler context; PostEvent/PostGraphOnly/PlaceOrder dispatch; `runBounded` |
| Core/CoreMailboxActors.fs | StartActor/StartPeerActor/ActorStop/Cancel handlers |
| Core/CoreMailboxLoad.fs | StartLoadSaveCommand handler |
| Core/CoreMailboxFilesAxis.fs | Notice → documentState axis writes (parse/persist/pulling) |
| Core/CoreEventDispatch.fs | Commit/append Ev, logNotice, writeAxis, markForPersist, lifecycle events |
| Core/DeskLoadPush.fs | Load-save push used by CoreMailboxLoad |
| Core/CoreActorPool.fs | Actor registry/start rows (handler side); actor bodies run off-thread via Async.Start |
| Core/Session.fs, Core/CoreCredentials.fs | Authority/admission checks in handlers |
| Core/CoreMsg.fs | CoreMsg/Mailbox/CoreQueue/PersistHandlers types |
| Core/CoreChanges.fs, Core/CoreActorNames.fs, Core/CoreChangesDoor.fs | Caller/Accepted/door types |
| Core/FileAgent.fs | PersistHandlers closures (applyEvent→processPostEvents, appendEvent, replaceGraph) run on mailbox thread; also boot + PersistThread wiring |
| Core/DbAgent.fs | Same for DB persistence; replaceGraph writes notice row |
| DocumentPersistChange.fs | validatePathMoves/validateGraphDiskEffects called from FileAgent/DbAgent applyEvent |
| EventLogFile.fs | appendEvent from FileAgent handler |
| Database.fs, DatabaseProjection.fs | DbAgent handler persistence (projection, writeNoticeRow, noticesBeside) |
| (edge) SearchActor.fs | Only `isQueryRequest`/`functionStart` run in StartActor handler; walk runs off-thread |
| (edge) Core/PersistThread.fs, Core/PersistRequestStack.fs | Core-owned, but runs on its own thread "gambol-persist" |
| (wiring, not handler) Core/MailboxHost.fs, CoreMailboxApi.fs, CoreDoor.fs, CoreMailbox.fs, CoreRuntime.fs | Queue door / composition / boot |

Not core: DocumentLoader.fs (boot load only), HttpResponseLog/ServerRunRegistry (logging/registry utilities).

## 2. Pure deps used by core (src/Shared)
ChangeAmendment, DocumentOpImpact, Ev/History (Ev, EventBody, Notice), EventId, EventLog, EventJson, MailboxLog, GraphMutate, GraphQuery, GraphSpan, GraphProjection, Node, DocumentState, ParseState, NodeUpdateTime, ProjectionOwnershipRepair, LoadSubject, LoadPathChoice, PathPick, CommandRequest, CssClass, DocumentArtifactPath, DocumentAssembly, DocumentParseLimits, DocumentPartition, DocumentPathMove, SystemDirectoryPersist.

## 3. Notice posters (enqueue CoreQueue.Notice)

| file:line | function | Notice(s) | core/outside | how it gets the host |
|---|---|---|---|---|
| Api.fs:270 | `postEvents` (via `inferredNodeEvents`, :247) | `noticesBeside` → NodeUploading, NodeReceived, MarkUnparsed, NodeParsed | outside (HTTP handler) | `handle.host` from CoreChangesDoor passed per request (RouteRegistration `persistence.Core.host`) |
| Api.fs:478 | `applyParseFile` (parse endpoint) | NodeParsed | outside (HTTP) | `handle.host` (door) |
| ParseThread.fs:194 | `bootCore` → `markUnparsed` (called in `afterPost` :67) | MarkUnparsed | outside (parse thread) | `core.host` from CoreRuntime it boots itself |
| ParseThread.fs:199 | `bootCore` → `finishParse` (called in `afterPost` :77) | NodeParsed | outside (parse thread) | `core.host` |
| LazyLoadReconciliationServer.fs:285 | `postParsedFromPulling` (called :304) | NodeParsed (PullingFile→Current ops) | outside (reconcile server) | `listed.handle.host` (door passed in) |
| LazyLoadReconciliationServer.fs:431 | `reconcileReceivedFiles` `beside` | NodeReceived | outside (git pull reconcile) | `workspace.handle.host` (door) |
| Core/PersistThread.fs:42 | `runRequest` | NodePersisted | edge: core-owned, persist thread | bound slot: `bindPersistFinished` ← CoreMailbox.fs:29 `postNotice created` |
| Core/PersistThread.fs:48 | `runRequest` | PersistFailed | edge: persist thread | same slot |
| Core/FileAgent.fs:108-119 | `postGraphOnlyNotices` (from applyEvent, graphOnly only) | noticesBeside set | core (mailbox thread, re-enqueues) | `persistFinished` slot |
| Core/DbAgent.fs:313-317 | `commitPostChange` (graphOnly only) | `DatabaseProjection.noticesBeside` set | core (mailbox thread, re-enqueues) | `persistFinished` slot |

Related, no enqueue (Notice-tagged direct axis writes): CoreEventDispatch.fs:219 `markForPersist` (NodePersisting, core); CoreMailboxFilesAxis.fs:20 `markUnparsedThenPush` (MarkUnparsed) — core when run from handlers, but also exposed as the `push` returned by `CoreRuntime.createPublishing` to ParseThread (ParseThread.fs:50, off-thread graph write; dormant while `requeueOnUnparsed = false`).
Non-Notice door posts (CoreMsg via CoreDoor/CoreMailboxApi), for scale: RouteRegistration 4, TestActor 4, GithubTransportActor 3, Api 2, RunAgentActor 2, LLRS 1, ParseThread 1, RouteAuthentication 1, SearchActor 1.

## 4. Violation summary
10 enqueue sites: 6 outside, 2 edge (persist thread), 2 core.
- HTTP Api (2): postEvents inferred Notices; parse endpoint NodeParsed. → infer in core after commit (135); parse endpoint goes with 126.
- ParseThread (2 + 1 dormant off-thread axis write): MarkUnparsed, NodeParsed. → core infers from the Parse Change (lead NodeParsed).
- Reconciliation server (2): NodeParsed from PullingFile, NodeReceived. → core infers from reconcile Change / Load.
- PersistThread (2, edge): NodePersisted/PersistFailed from its own thread via bound slot. → decide whether persist completion counts as core.
- Agents (2, core but re-enqueue): FileAgent/DbAgent graphOnly noticesBeside. → compliant by rule; duplicate of Api inference path.
- Access (structural): `postNotice` internal + public `CoreChangesDoor.host` → any door holder can post. → narrow access to the loop.
