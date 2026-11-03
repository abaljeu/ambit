# 2026-11-03 Actor start decisions

Docs only. No `src/` or `tests/` edits.

## Tickets

[ ] 06 — Add the function-and-focus start. Expand. Over the size limit because the new `Order` and `CoreMsg` cases are exhaustive.
[ ] 07 — Dispatch the test command.
[ ] 08 — Dispatch the AI Actor. Stops until Alan says where the session id is minted.
[ ] 09 — Dispatch the query command.
[ ] 10 — Dispatch git Load and Save.
[ ] 11 — Move door and case tests.
[ ] 12 — Move pool and issue tests.
[ ] 13 — Move persist and command tests.
[ ] 14 — Move runtime and session tests.
[ ] 15 — Delete the old Actor starts.
[ ] 80 — Place Actor starts through PlaceOrder. Rewritten. Same file name.

The caller move is split because [Size limit](../../context/size-limit.md) is 8 source files and 4 test files, and the research counted about 10 to 13 source files and 20 to 25 Server test files.

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

[ ] The logged `Order` cannot store a function. Alan said the new start and its Order require the function and the FocusId. He did not say what the Order stores. `Order.StartActor` is not on the delete list.
[ ] The case names of the new `CoreMsg` and the new `Order` are not chosen. The tickets say "the function-and-focus start".
[ ] `runSchedule` passes `CoreChangesDoor` when it calls the function. Alan named the start arguments and said the session id is not a function-input field. He did not say whether the door stays a parameter. Ticket 06 copies the door from `runSchedule` and stops if that list is wrong.
[ ] The pool mints `sessionId` inside `putActorStart` and `putFunctionStart`. The new start cannot take that id as an argument. Ticket 08 stops until Alan says where it is minted so the dispatcher can close over the same id the live table uses.
[ ] `startFunction` is not on Alan's delete list. Ticket 15 deletes it because the decision is one start, and stops if Alan keeps it.
[ ] `Order.LoadSave` still carries `PeerActorName`. Desk with no Actor stays on ticket 80. Alan did not say whether `Order.LoadSave` remains.
[ ] A named registry for remote or cross-process start was left as fog on 2026-09-27. Alan decided the in-process dispatcher. He did not mention a remote start.
[ ] `GLOSSARY.md` still says an Actor is "a Core-managed execution thread". This pass did not edit the glossary.
[ ] Find and Move `RecordSearchStart` is bookkeeping and does not run the pool. Alan named browser Run and git Load and Save. Ticket 09 does not move `recordSearchStart`.
[ ] The browser still posts an `ActorStart` record, including `graphIds`. The dispatcher closes over those ids on the server. Alan did not say the browser payload drops `graphIds`.
[ ] `plan/core-refinement/arch.md` had no sentence "An Actor runs on a thread". The compact page did. A dated note on the master records the thread pool so the two pages match.
[ ] The dispatcher file in the tickets is `src/Server/ActorDispatch.fs`. Alan did not name the file.
