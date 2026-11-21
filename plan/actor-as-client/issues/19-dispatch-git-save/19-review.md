# 19 — Start git Save

Ticket: [[19-dispatch-git-save-ticket.md]]
Spec: `plan/actor-as-client/spec.md` is absent. This project has no `spec.md`. The ticket is the spec.
Arch: [[plan/actor-as-client/arch.md]]

## 1. Standards

- **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` printed no line and exited 0. No `LONG`, `TAB`, `MUTABLE`, `BARE_ID`, `PHRASE`, or `FILE` line.
- **Range.** The diff is this review, `CONTEXT.md` in this folder, and the Status cell for 19 — Start git Save in `plan/actor-as-client/sequence.md`. The same diff also carries 10 — Start git Load.

## 2. Spec

- **Met.** The Working inputs match the code. The source files stay as they are on staging. The edit count stays 0.
- **Checklist.** Each checklist sentence matches the code cited here. No checklist sentence is stale.
- **One job.** `GithubTransportActor.start` builds the Save `ActorFunction` in `saveActorFunction` and `MailboxHost.postActorOrder` puts `Order.StartActor` of `ActorOrder` on the mailbox queue. `Api.postLoadSaveCommand` reaches that post through `router.startCommand`. A Desk Save runs no Actor.
- **ActorOrder.** `ActorOrder` in `src/Shared/History.fs` is `actor`, `actorNodeId`, and `actorName`. `Order` keeps its current cases, including `Order.StartActor` of `ActorOrder`.
- **Route.** `RouteRegistration.registerStateRoutes` maps `MapPost "/ambit/load-save-command"`. The Desk arm calls `CoreMailboxApi.startLoadSaveCommand`. The other arm calls `GithubTransportActor.start`. `LoadSavePath` is `Git` or `Desk`, so that other arm is the Git path. `githubOperation` maps Save to `GithubTransportOperation.Save`.
- **Save function.** `GithubTransportActor.start` calls `saveActorFunction` for Save. That result is the `actor` on the placement. `start` then calls `CoreMailboxApi.coreMailboxStartActorFromFunction`.
- **Order builder.** `coreMailboxStartActorFromFunction` calls `ActorOrderFromStart.orderFromPlacement`. The actor node id comes from `focusIdOf`. The Save name comes from `transportActorName`, which returns `github-save` for Save. That function is private. `peerName` and `start` are the callers.
- **Queue.** `MailboxHost.postActorOrder` places `Order.StartActor` on the mailbox queue and waits for the `EventId` reply.
- **Pool first.** `CoreMailboxLoop.placeOrder` sends `Order.StartActor` to `replyActorOrder`. `replyActorOrder` calls `admitCaller`, then `scheduleActorOrder`. `scheduleActorOrder` calls `CoreActorPool.startFunction`, and on `Ok` it calls `CoreActorPool.schedule`. `startFunction` admits through `admitFunctionStart` and `putFunctionStart`. `schedule` launches the `ActorFunction` through `runSchedule`. `replyAffirmedStart` appends `EventBody.Order` of `Order.StartActor` only after `scheduleActorOrder` returns `Ok`. A refusal from `admitCaller` or from `scheduleActorOrder` replies with the failure. `replyAffirmedStart` does not run, so the log gains no `Order.StartActor` row. Save uses the same `start` path as Load. The operation match only chooses `saveActorFunction`.
- **Same rule as the query start.** [09 — Start the query command](../09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md) puts that pool-first reply in `replyActorOrder`. Git Save uses that same function.
- **Desk builder.** `CoreMailboxApi.startLoadSaveCommand` is the only constructor of `CoreMsg.StartLoadSaveCommand`. `RouteRegistration.registerStateRoutes` calls it on the Desk arm.
- **Desk routers.** `LoadSaveCommandTests.productionRouter` and `LoadSaveCommandTests.routeMatrixRouter` call `CoreMailboxApi.startLoadSaveCommand` when the path is Desk, and they call `GithubTransportActor.start` when the path is Git.
- **Save callers.** The Save callers of `CoreMailboxApi.startLoadSaveCommand` are those three Desk arms. A search shows no Git Save call of that function. ``every Git choice starts the Server Peer Actor`` includes Save with pre-pick `git` and Save with pre-pick `plain`, asserts `LoadSavePath.Git`, and goes through `routeMatrixRouter`, so that Save calls `GithubTransportActor.start`.
- **Load calls stay.** [10 — Start git Load](../10-dispatch-git-load-and-save/10-dispatch-git-load-and-save-ticket.md) still has Load calls of `CoreMailboxApi.startLoadSaveCommand` in `src/Server/RouteRegistration.fs`, `tests/Server.Tests/LoadSaveCommandTests.fs`, and `tests/Server.Tests/ParseThreadLoadTests.fs`. ``plain Load of a File or Directory ignores a Git path`` passes `LoadSavePath.Git` into that builder. `PathPick.effectiveLoadPath` makes that plain Load Desk inside `DeskLoadPush.runLoadSave`.
- **Desk Save stays.** ``plain Save with no GitHub remote takes the Desk path`` and ``explicit Desk Save with a GitHub remote takes the Desk path`` use `productionRouter` and assert `LoadSavePath.Desk`. Those Desk sends stay for [20 — Move Desk Load and Desk Save](../20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md).
- **Pool result.** `CoreActorPool.startLoadSaveCommand` returns `Ok None` for `LoadSavePath.Desk` and for `LoadSavePath.Git`.
- **Deleted peer start.** `startPeerActor` and `registerPeer` are absent from `src/Server/Core/CoreActorPool.fs` and from `src/Server/RouteRegistration.fs`. `registerPeer` remains the forbidden name in `tests/Server.Tests/AppGithubTransportBoundaryTests.fs`.
- **Kept bounds.** This review adds no `src/Server/ActorDispatch.fs`, writes no `Order.StartActorDeprecated`, and leaves `DeskLoadPush.fs` unchanged. 20 — Move Desk Load and Desk Save still owns the deletion of `CoreMsg.StartLoadSaveCommand`.
- **Blockers.** [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md) is already on staging. The Git Load post for 10 — Start git Load is the same `GithubTransportActor.start` path.
- **Reference.** **Load and save** names `GithubTransportActor.start` as the Git Save post and names `Api.postLoadSaveCommand` as the caller of `startCommand`. The open **Git Load or Save** path is the same order: **Curried actor**, **Post actor order**, **Pool start**, **Pool launch**, then **Place actor order**.

## 3. Arch

- No findings.

## 4. Tests

- **Suite.** `bash scripts/check-test.sh 3028` printed `DONE` and `/tmp/ambit-full-suite-20261110-205803.log`. The log ends `full suite finished`. `tests/Server.Tests` passed 762, skipped 1, failed 0. `tests/Shared.Tests` passed 1937, skipped 3, failed 0. The run uses `bash` because `scripts/check-test.sh` has no execute bit.
- **Save seam.** `tests/Server.Tests/GithubTransportActorTests.fs` calls `GithubTransportActor.start` for Save. ``git Save gates commit then releases before push``, ``git Save persists focus then commits pulls and pushes``, ``git Save marks an unparsed file parsed and writes it before commit``, ``git Save on a Normal Node leaves that node unchanged then commits pulls and pushes``, ``git Save stops when the pull fails and does not push``, and the Save row of ``Load and Save return the same conflict reject shape`` are those facts.
- **Desk seam.** `tests/Server.Tests/LoadSaveCommandTests.fs` keeps the Desk Save checks ``plain Save with no GitHub remote takes the Desk path`` and ``explicit Desk Save with a GitHub remote takes the Desk path``.
- **Git seam.** ``routed Git Save commits then pushes through Peer Actor`` and the Save rows of ``every Git choice starts the Server Peer Actor`` call `GithubTransportActor.start` through the Git arm.

## 5. Miss

- No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- No findings.
