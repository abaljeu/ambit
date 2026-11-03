# 2026-11-03 Actor start decisions

Docs only. No `src/` or `tests/` edits.

## Decisions

Alan, 2026-11-03, answering the open questions from the first pass.

[x] `startFunction` is the one pool start.
[o] `startActor` is the original start.
[o] `startPeerActor` was never supposed to exist.
[ ] 15 — Delete the old Actor starts deletes `startActor` and `startPeerActor` only. `startFunction` stays.
[ ] `Order.RunActor` stores the command name and the FocusId.
[o] A new `CoreMsg` case is the start path.
[x] The function input is `FunctionRun` (`getGraph` and the Actor credential) and `CoreChangesDoor`.
[o] The start provides `ActorStart` of `zoomId`, `focusId`, `commandId`, `graphIds`, and `eventId`.
[ ] Anything else the Actor needs is in the closure the dispatcher builds.
[x] The pool mints the Actor session id, as today.
[ ] The LLM HTTP session is in the AI Actor closure. 08 — Dispatch the AI Actor is unblocked on that split.
[ ] `runSchedule` is renamed `launchPending` on 15 — Delete the old Actor starts. It launches pending work with `Async.Start` on the row cancel token.

## Tickets

[ ] 06 — Log Order.RunActor. The folder name stays `06-add-the-function-and-focus-start`. It no longer adds a pool start or a `CoreMsg` case. It was not merged: the Order is shared by the dispatcher tickets and by 80 — Place Actor starts through PlaceOrder. Four source files, inside the size limit.
[ ] 07 — Dispatch the test command. Calls `startFunction`.
[ ] 08 — Dispatch the AI Actor. Unblocked. The closure holds the LLM HTTP session. The Actor session id stays pool-minted.
[ ] 09 — Dispatch the query command. Keeps `startFunction`.
[ ] 10 — Dispatch git Load and Save. Calls `startFunction`.
[ ] 11 — Move door and case tests.
[ ] 12 — Move pool and issue tests.
[ ] 13 — Move persist and command tests.
[ ] 14 — Move runtime and session tests.
[ ] 15 — Delete the old Actor starts. Does not delete `startFunction`. Renames `runSchedule` to `launchPending`.
[ ] 80 — Place Actor starts through PlaceOrder. Places `Order.RunActor`. Adds no `CoreMsg` case.

The caller move stays split because [Size limit](../../context/size-limit.md) is 8 source files and 4 test files.

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

[ ] `Order.StartActor` is not on the delete list. Alan added `Order.RunActor` and did not say `Order.StartActor` leaves.
[ ] `Order.LoadSave` still carries `PeerActorName`. A Desk Load or Save that runs no Actor stays on 80 — Place Actor starts through PlaceOrder. Alan did not say whether `Order.LoadSave` remains.
[ ] A named registry for remote or cross-process start was left as fog on 2026-09-27. Alan decided the in-process dispatcher. He did not mention a remote start.
[ ] `GLOSSARY.md` still says an Actor is "a Core-managed execution thread". This pass did not edit the glossary.
[ ] Find and Move `RecordSearchStart` is bookkeeping and does not run the pool. Alan named browser Run and git Load and Save. 09 — Dispatch the query command does not move `recordSearchStart`.
[ ] The browser still posts an `ActorStart` record, including `graphIds`. Alan did not say the browser payload drops `graphIds`.
[ ] The dispatcher file in the tickets is `src/Server/ActorDispatch.fs`. Alan did not name the file.
