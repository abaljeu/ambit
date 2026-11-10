# 10 — Start git Load

Ticket: [[10-dispatch-git-load-and-save-ticket.md]]
Spec: `plan/actor-as-client/spec.md` is absent. This project has no `spec.md`. The ticket is the spec.
Arch: [[plan/actor-as-client/arch.md]]

## 1. Standards

- **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` printed no line and exited 0. No `LONG`, `TAB`, `MUTABLE`, `BARE_ID`, `PHRASE`, or `FILE` line.
- **Range.** The diff is this review, `CONTEXT.md` in this folder, and the Status cell for 10 — Start git Load in `plan/actor-as-client/sequence.md`. The same diff also carries 19 — Start git Save.

## 2. Spec

- **Met.** The Working inputs match the code. The source files stay as they are on staging. The edit count stays 0.
- **Checklist.** Each checklist sentence matches the code cited here. No checklist sentence is stale.
- **One job.** `GithubTransportActor.start` builds the Load `ActorOrder` and the mailbox places `Order.StartActor`. `Api.postLoadSaveCommand` reaches that post through `router.startCommand`. A Desk Load runs no Actor.
- **ActorOrder.** `ActorOrder` in `src/Shared/History.fs` is `actor`, `actorNodeId`, and `actorName`. `Order` keeps its current cases, including `Order.StartActor` of `ActorOrder`.
- **Route.** `RouteRegistration.registerStateRoutes` maps `MapPost "/ambit/load-save-command"`. The Desk arm calls `CoreMailboxApi.startLoadSaveCommand`. The other arm calls `GithubTransportActor.start`. `LoadSavePath` is `Git` or `Desk`, so that other arm is the Git path.
- **Load function.** `GithubTransportActor.start` calls `loadActorFunction` for Load and `saveActorFunction` for Save. Both calls then use `CoreMailboxApi.coreMailboxStartActorFromFunction`.
- **Order builder.** `coreMailboxStartActorFromFunction` calls `ActorOrderFromStart.orderFromPlacement`. `orderFromPlacement` calls `orderFromStart`. The actor node id comes from `focusIdOf`, which reads `ActorStart.focusId`. The Load name comes from `transportActorName`, which returns `github-load` for Load. That function is private. `peerName` and `start` are the callers.
- **Queue.** `MailboxHost.postActorOrder` places `Order.StartActor` on the mailbox queue and waits for the `EventId` reply.
- **Pool first.** `CoreMailboxLoop.placeOrder` sends `Order.StartActor` to `replyActorOrder`. `replyActorOrder` calls `admitCaller`, then `scheduleActorOrder`. `scheduleActorOrder` calls `CoreActorPool.startFunction`, and on `Ok` it calls `CoreActorPool.schedule`. `startFunction` is `runStartFunction`, which calls `admitFunctionStart` and then `putFunctionStart`. `schedule` is `runSchedule`, which launches the `ActorFunction`. `replyAffirmedStart` appends `EventBody.Order` of `Order.StartActor` only after `scheduleActorOrder` returns `Ok`. A refusal from `admitCaller` or from `scheduleActorOrder` replies with the failure. `replyAffirmedStart` does not run, so the log gains no `Order.StartActor` row. Load and Save share this path. The operation match only chooses `loadActorFunction` or `saveActorFunction`.
- **Same rule as the query start.** [09 — Start the query command](../09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md) puts that pool-first reply in `replyActorOrder`. Git Load uses that same function through `coreMailboxStartActorFromFunction`.
- **Desk builder.** `CoreMailboxApi.startLoadSaveCommand` is the only constructor of `CoreMsg.StartLoadSaveCommand`. `RouteRegistration.registerStateRoutes` calls it on the Desk arm.
- **Plain Load.** ``plain Load of a File or Directory ignores a Git path`` in `tests/Server.Tests/ParseThreadLoadTests.fs` calls `CoreMailboxApi.startLoadSaveCommand` with `LoadSavePath.Git`. `CoreMailboxLoad.dispatchStartLoadSaveCommand` calls `DeskLoadPush.runLoadSave`. `runLoadSave` calls `PathPick.effectiveLoadPath`. A plain Load of a File or a Directory becomes `LoadSavePath.Desk` there. `CoreActorPool.startLoadSaveCommand` then returns `Ok None`.
- **Desk Load facts.** These facts call `CoreMailboxApi.startLoadSaveCommand`: ``held Desk Load does not push and the follow-up Load does``, ``Desk Load pushes a Workspace``, ``Desk Load command pushes a Directory and a File``, ``Directory Load push reconciles when the parse thread pops it``, ``File Load push parses when the parse thread pops it``, ``second Load of a Parsed File is parsed again``, ``second Load of a Current File lands its parse Event on a client that still holds Current``, and ``second Load of a Parsed Directory reconciles and does not requeue``. Later parse facts call `userLoad`, and `userLoad` calls `CoreMailboxApi.mbLoadNode`.
- **Desk Load in the command file.** ``Desk request reaches pool without starting Peer Actor``, ``plain Load of a non-Workspace returns Desk when a remote exists``, ``plain Load of a File or Directory stays Desk when a remote exists``, ``Load on a file parses that file``, and ``Desk load-save places its order and runs no Actor`` call `CoreMailboxApi.startLoadSaveCommand`. The last of those asserts an empty live set and one stored `Order.StartActor` with an empty actor name. That row is `CoreMailboxLoad.logDeskOrder`, which runs for a Desk path and uses `stubActor`.
- **Callers.** A search of `src/` and `tests/` finds `CoreMailboxApi.startLoadSaveCommand` in `src/Server/RouteRegistration.fs`, `tests/Server.Tests/LoadSaveCommandTests.fs`, and `tests/Server.Tests/ParseThreadLoadTests.fs`. The constructor `StartLoadSaveCommand(` appears only in `CoreMailboxApi.startLoadSaveCommand`.
- **Save callers.** Desk Save still calls that builder from the Desk arm of `registerStateRoutes`, `LoadSaveCommandTests.productionRouter`, and `LoadSaveCommandTests.routeMatrixRouter`. Those callers stay for [19 — Start git Save](../19-dispatch-git-save/19-dispatch-git-save-ticket.md).
- **Pool result.** `CoreActorPool.startLoadSaveCommand` returns `Ok None` for `LoadSavePath.Desk` and for `LoadSavePath.Git`.
- **Deleted peer start.** `startPeerActor` and `registerPeer` are absent from `src/Server/Core/CoreActorPool.fs` and from `src/Server/RouteRegistration.fs`. `registerPeer` remains the forbidden name in `tests/Server.Tests/AppGithubTransportBoundaryTests.fs`.
- **Kept bounds.** This review adds no `src/Server/ActorDispatch.fs`, writes no `Order.StartActorDeprecated`, and leaves `DeskLoadPush.runLoadSave` as it is. [20 — Move Desk Load and Desk Save](../20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md) still owns the deletion of `CoreMsg.StartLoadSaveCommand`.
- **Blocker.** [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md) is already on staging.
- **Reference.** **Load and save** names `GithubTransportActor.start` as the Git Load post and names `Api.postLoadSaveCommand` as the caller of `startCommand`. The open **Git Load or Save** path is the same order: **Curried actor**, **Post actor order**, **Pool start**, **Pool launch**, then **Place actor order**.

## 3. Arch

- No findings.

## 4. Tests

- **Suite.** `bash scripts/check-test.sh 3028` printed `DONE` and `/tmp/ambit-full-suite-20261110-205803.log`. The log ends `full suite finished`. `tests/Server.Tests` passed 762, skipped 1, failed 0. `tests/Shared.Tests` passed 1937, skipped 3, failed 0. The run uses `bash` because `scripts/check-test.sh` has no execute bit.
- **Load seam.** `tests/Server.Tests/GithubTransportActorTests.fs` calls `GithubTransportActor.start` for Load. ``git Load tells the client the actor completed``, ``git Load gates pull from subnode Focus``, ``git Load posts that Workspace id and does not reconcile``, and ``git Load ends when the pull fails and posts nothing`` are those facts. The pull-failure fact requires `Ok` from `start`, then `ActorFailed`. That failure is inside the launched actor. It is not a pool refusal.
- **Desk seam.** `tests/Server.Tests/LoadSaveCommandTests.fs` keeps the Desk Load checks named in the Spec list.
- **Plain Load seam.** `tests/Server.Tests/ParseThreadLoadTests.fs` keeps ``plain Load of a File or Directory ignores a Git path``.

## 5. Miss

- No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- No findings.
