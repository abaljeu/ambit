# 2026-11-03 Actor start decisions

Docs only. No `src/` or `tests/` edits.

## Decisions

Alan, 2026-11-03.

[x] `startFunction` is the one pool start.
[o] `startActor` is the original start.
[o] `startPeerActor` was never supposed to exist.
[ ] 15 — Delete the old Actor starts deletes `startActor`, `startPeerActor`, and `CoreMsg.StartLoadSaveCommand`. `startFunction` stays.
[ ] `Order.RunActor` stores the command name and the FocusId.
[o] A new `CoreMsg` case is the start path.
[x] The actor function's input is `FunctionRun` (`getGraph` and the Actor credential) and `CoreChangesDoor`.
[o] The start provides `ActorStart` of `zoomId`, `focusId`, `commandId`, `graphIds`, and `eventId`.
[ ] Anything else the actor function needs is in the closure the dispatcher builds.
[x] The pool mints the Actor session id, as today.
[ ] The LLM HTTP session is in the AI Actor closure. 08 — Dispatch the AI Actor is unblocked on that split.
[ ] `runSchedule` is renamed `launchPending` on 15 — Delete the old Actor starts.
[o] `Order.StartActor` is a logged start. 15 — Delete the old Actor starts deletes it completely. No reader keeps an old row.
[ ] Tickets 07, 08, 09, 10, and 80 do not write `Order.StartActor`. They write `Order.RunActor`.
[o] `Order.LoadSave` is the git Load and Save log. Git Load and Save log `Order.RunActor`.
[ ] Nothing writes `Order.LoadSave` before 15. 15 deletes `Order.LoadSave` and its `PeerActorName`.
[ ] A Desk Load or Save logs `Order.RunActor` and runs no actor function.
[ ] Remote or cross-process start is out of scope for this cutover. The later question is on the map: how a command reaches a worker in another process.
[x] An Actor is a thread from the .NET thread pool. It runs an actor function. Alan's words: "The Actor is the thread. It runs an actor function."
[o] An Actor is the actor function.
[ ] Find and Move `recordSearchStart` logs `Order.RunActor`. 09 — Dispatch the query command deletes `CoreMsg.RecordSearchStart`. The search-start log is `Order.RunActor`. That ticket is 7 source files and 3 test files. The size limit is 8 source files and 4 test files.
[o] The browser `ActorStart` payload, including `graphIds`, changes in this cutover. It stays for this cutover.
[ ] Per-run endpoints: the endpoint varies by what is being run, each with its own payload.
[x] The dispatcher module is `src/Server/ActorDispatch.fs`.

## Tickets

[ ] 06 — Log Order.RunActor. Folder name stays `06-add-the-function-and-focus-start`.
[ ] 07 — Dispatch the test command. Does not write `Order.StartActor`.
[ ] 08 — Dispatch the AI Actor. Does not write `Order.StartActor`.
[ ] 09 — Dispatch the query command. Find and Move `recordSearchStart` logs `Order.RunActor`. This ticket deletes `CoreMsg.RecordSearchStart` and its arms in `CoreMailboxLoop` and `CoreMailboxEvents`. It adds no `Order`.
[ ] 10 — Dispatch git Load and Save. This ticket converts every caller of `CoreMsg.StartLoadSaveCommand` to an actor launch through `ActorDispatch.fs` and `Order.RunActor`. After this ticket, nothing sends `StartLoadSaveCommand`. It logs `Order.RunActor`. It does not write `Order.LoadSave`. The source files are 6 and the test files are 4.
[ ] 11 — Move door and case tests.
[ ] 12 — Move pool and issue tests.
[ ] 13 — Move persist and command tests.
[ ] 14 — Move runtime and session tests.
[ ] 15 — Delete the old Actor starts. Also deletes `Order.StartActor`, `Order.LoadSave`, and `CoreMsg.StartLoadSaveCommand`, with no reader for old rows. Renames `runSchedule` to `launchPending`. The named outputs are 9 source files and 2 test files. `CoreMailboxLoad.fs` joins them when `dispatchStartLoadSaveCommand` has no caller left, which is 10 source files.
[ ] 80 — Place Actor starts through PlaceOrder. Places `Order.RunActor`. Does not write `Order.StartActor` or `Order.LoadSave`.

## doc/current files changed

[ ] `doc/current/actors.md`
[ ] `doc/current/ai-agent-protocol.md`
[ ] `doc/current/api.md`
[ ] `doc/current/http-contract.md`
[ ] `doc/current/mailbox.md`
[ ] `doc/current/op.md`
[ ] `doc/current/query-actor.md`
[ ] `doc/current/search-actor.md`
[x] This follow-up edits none of those `doc/current` files.

## Counts checked 2026-11-03

Counts are file lengths on staging. Line numbers are the `RecordSearchStart` and `StartLoadSaveCommand` sites.

[ ] `src/Server/SearchActor.fs` is 90 lines. `functionStart` is lines 71-90.
[ ] `src/Server/Core/CoreMailboxActors.fs` is 258 lines. `startChosen` is lines 82-94.
[ ] `src/Server/Core/CoreMailboxApi.fs` is 400 lines. `recordSearchStart` is lines 308-314. `startLoadSaveCommand` is lines 325-338.
[ ] `src/Server/Core/CoreMsg.fs` is 116 lines. `StartLoadSaveCommand` is lines 19-24. `RecordSearchStart` is lines 26-29.
[ ] `src/Server/Core/CoreMailboxLoop.fs` is 464 lines. `StartLoadSaveCommand` is lines 22-24 and line 72. `RecordSearchStart` is lines 25-27 and line 73.
[ ] `src/Server/Core/CoreMailboxEvents.fs` is 138 lines. `StartLoadSaveCommand` is lines 46-47 and 64-65. `RecordSearchStart` is line 48 and lines 66-67.
[ ] `src/Server/Core/CoreMailboxLoad.fs` is 75 lines. `dispatchStartLoadSaveCommand` is lines 34-55.
[ ] `src/Server/Core/CoreActorPool.fs` is 417 lines.
[ ] `src/Shared/History.fs` is 571 lines.
[ ] `src/Shared/EventJson.fs` is 454 lines.
[ ] `src/Shared/MailboxLog.fs` is 60 lines.
[ ] `src/Server/ActorDispatch.fs` is absent. 09 — Dispatch the query command adds it.
[ ] `tests/Server.Tests/QueryActorTests.fs` is 333 lines.
[ ] `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` is 152 lines.
[ ] `tests/Server.Tests/SearchActorTests.fs` is 288 lines.
[ ] `tests/Shared.Tests/EventJsonTests.fs` is 399 lines.
[ ] `tests/Shared.Tests/MailboxLogTests.fs` is 118 lines.
[ ] The only constructor of `CoreMsg.RecordSearchStart` is `recordSearchStart`. `RouteRegistration.searchDoor` line 229 and `SearchActorTests.fs` line 234 call that function.
[ ] The only constructor of `CoreMsg.StartLoadSaveCommand` is `startLoadSaveCommand`. Callers of the function are `RouteRegistration.fs` line 349, `LoadSaveCommandTests.fs`, and `ParseThreadLoadTests.fs`.

## Order

[ ] 111 — Place search start as an order, and post SearchStopped is owned by Ambot2. It shrinks to posting `Notice.SearchStopped` and removing `CoreMsg.RecordSearchStop`. It runs after 15 — Delete the old Actor starts.
[ ] Ambot2 is writing the `CoreMsg.PostGraphOnly` removal now. That ticket is built before the cutover builds 06 — Log Order.RunActor through 15 — Delete the old Actor starts.
[ ] 112 — Place Cancel through PlaceOrder is being rewritten as a ticket only. It is not ready to build. Its place is after Record the one function Actor start (PR #514).
[ ] This Project does not edit tickets 111 through 118 in core-refinement.

## Open questions

[ ] `CoreEventDispatch.actorStart` is `src/Server/Core/CoreEventDispatch.fs` lines 72-77. Its only caller is the `RecordSearchStart` arm. 09 — Dispatch the query command deletes that arm and leaves the function. No ticket in this Project deletes `actorStart`.
[x] 10 — Dispatch git Load and Save owns the caller conversion. After that ticket, nothing sends `StartLoadSaveCommand`. 15 — Delete the old Actor starts deletes the message. Alan, 2026-11-03.
