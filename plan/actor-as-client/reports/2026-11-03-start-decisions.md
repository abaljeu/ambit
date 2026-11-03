# 2026-11-03 Actor start decisions

Docs only. No `src/` or `tests/` edits.

## Decisions

Alan, 2026-11-03.

[x] `startFunction` is the one pool start.
[o] `startActor` is the original start.
[o] `startPeerActor` was never supposed to exist.
[ ] 15 — Delete the old Actor starts deletes `startActor` and `startPeerActor` only. `startFunction` stays.
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
[ ] Find and Move `recordSearchStart` logs `Order.RunActor`. It is in 09 — Dispatch the query command. That ticket stays inside the size limit: 4 source files and 3 test files.
[o] The browser `ActorStart` payload, including `graphIds`, changes in this cutover. It stays for this cutover.
[ ] Per-run endpoints: the endpoint varies by what is being run, each with its own payload.
[x] The dispatcher module is `src/Server/ActorDispatch.fs`.

## Tickets

[ ] 06 — Log Order.RunActor. Folder name stays `06-add-the-function-and-focus-start`.
[ ] 07 — Dispatch the test command. Does not write `Order.StartActor`.
[ ] 08 — Dispatch the AI Actor. Does not write `Order.StartActor`.
[ ] 09 — Dispatch the query command. Scope now includes Find and Move `recordSearchStart` as `Order.RunActor`.
[ ] 10 — Dispatch git Load and Save. Logs `Order.RunActor`. Does not write `Order.LoadSave`.
[ ] 11 — Move door and case tests.
[ ] 12 — Move pool and issue tests.
[ ] 13 — Move persist and command tests.
[ ] 14 — Move runtime and session tests.
[ ] 15 — Delete the old Actor starts. Also deletes `Order.StartActor` and `Order.LoadSave`, with no reader for old rows. Renames `runSchedule` to `launchPending`.
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

## Open questions
