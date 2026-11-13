Check: pass

# 26 — Retire the posted named start

Ticket: [[26-retire-the-posted-named-start-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

## 1. Standards

- **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` exited 0. The measure line is `admittedQueue` at 20 lines, under the 40-line limit. No `LONG`, `TAB`, `MUTABLE`, `BARE_ID`, `PHRASE`, `BLANK_BLANK`, or `FILE` line.
- **Rules.** The diff matches `.agents/rules/fsharp-source.md`, `.agents/rules/core-api.md`, `.agents/rules/writing-markdown.md`, and `.agents/rules/refer-by-name.md`.

## 2. Spec

- **Spec file.** `plan/actor-as-client/spec.md` is absent. The ticket is the spec.
- **One job.** `postActorStart` replies `Error "unknown actor"` when `admittedQueue` returns `None`. `postChosenStart`, `startChosen`, `failedMessage`, and `recordFailedStart` are gone from `src/` and `tests/`.
- **Admitted names.** The `test`, `ai`, and query arms still queue. `dispatchStartActor`, `coreMailboxStartActor`, `Order.StartActorDeprecated`, and `EventBody.ActorStart` stay.
- **Submission id.** Admitted `test`, `ai`, and query starts go through `MailboxHost.postPlacedOrder` with `orderId` set to the posted submission id. `MailboxHost.postActorOrder` still mints a new id, and that mint cannot satisfy the resubmit. Step 9 stores `Order.StartActor` for `?test hello` and returns that stored event on the second post of the same submission. `postActorStart` does that through `storedBySubmission` before `admittedQueue`, so the second post does not schedule another actor.
- **Duplicate and catch-up.** `CoreEventDispatch.appendLifecycle` still calls `catchUpNextId` before it assigns the stored id. The caller check is `storedBySubmission` on the posted submission id. A sequential resubmit finds the stored `Order.StartActor`. A batch resubmit still uses `cursorAfterDuplicateChange`, so a later stored Change keeps the ack cursor at the log tip.

## 3. Arch

- **Posted run.** `queueAdmittedTest`, `queueAdmittedAi`, and `queueAdmittedQuery` still build an `ActorOrder` and place `Order.StartActor`. A posted name with no function start replies from `postActorStart` and does not reach the pool.
- **Module map.** The diff leaves `placeOrder`, `startFunction`, and `EventBody.ActorStart` in place. `dispatchStartActor` still calls `pool.startActor`.

## 4. Tests

- **Suite.** `bash scripts/check-test.sh 2965` printed `DONE` and `/tmp/ambit-full-suite-20261113-023833.log`.
- **Server.** `tests/Server.Tests` passed 778, failed 0, skipped 1. The skip is ``Change.upload structure persistence preserves existing file mtime``.
- **Shared.** `tests/Shared.Tests` passed 1940, failed 0, skipped 3. The skips are ``reuses existing owned path without duplicating``, ``planServerFilePresentOps transitions only matching absent files``, and ``listForPush without git still returns walk``.
- **Unknown reply.** ``edit then unknown ActorStart replies unknown actor`` and ``edit then rejected ActorStart replies unknown actor`` expect HTTP 400, the text `unknown actor`, and no `EventBody.ActorStart` or `EventBody.ActorStop` for that line.
- **Production door.** ``production events door replies unknown actor`` expects HTTP 400 and `unknown actor` for `?unknown` and `?nope`, and no lifecycle row for that line.
- **Orders.** ``Undo and Redo post as orders; Cancel posts as PostEvent`` keeps the Undo, Redo, and Cancel posts. The posted root start is gone.
- **Resubmit.** ``postEvent returns the stored start when that start stops at once`` posts `?test hello`, expects `Order.StartActor` with actor name `test`, and expects the second post of that submission to return the same stored event.
- **Gate.** ``posted gate start replies unknown actor`` expects `Error "unknown actor"` and no `EventBody.ActorStart` or `EventBody.ActorStop`.
- **Deleted tests.** ``ActorStart body does not run inside the mailbox turn``, ``postCommand through CoreMailbox encodes ActorStart in latestId events``, and ``postCommand startActor can produce hello child`` are gone.

## 5. Miss

- No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- No findings.
