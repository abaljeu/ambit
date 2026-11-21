Check: pass

# 16 — Rename the client start request to ClientRequestActor

Ticket: [[16-delete-the-old-order-cases-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

## 1. Standards

- **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` exited 0. The measure line is `internal` in `src/Server/Core/MailboxHost.fs` at 14 lines, under the 40-line limit. No `LONG`, `TAB`, `MUTABLE`, `BARE_ID`, `PHRASE`, `BLANK_BLANK`, or `FILE` line.
- **Rules.** The diff matches `.agents/rules/fsharp-source.md`, `.agents/rules/core-api.md`, `.agents/rules/writing-markdown.md`, and `.agents/rules/refer-by-name.md`.
- **Size.** The source diff is `src/Shared/History.fs`, `src/Shared/EventJson.fs`, `src/Shared/MailboxLog.fs`, `src/Server/Core/MailboxHost.fs`, and `src/Server/Core/CoreMailboxLoop.fs`. The test diff is `tests/Shared.Tests/EventJsonTests.fs` and `tests/Server.Tests/TestActorCommandErrorTests.fs`.
- **Name.** **Judgement.** Mysterious Name. `decodeOrderStartActor` in `src/Shared/EventJson.fs` keeps that name and builds `Order.ClientRequestActor`. The wire kind on that decoder stays `startActor`. Source: [Smell baseline](plan/context/SMELLS.md).
- **Spread.** **Judgement.** Shotgun Surgery, suppressed. Renaming one `Order` case touches each match. The ticket Outputs name those files. Same source as the name judgement.

## 2. Spec

- **Spec file.** `plan/actor-as-client/spec.md` is absent. The ticket is the spec.
- **Case.** `Order.ClientRequestActor` of `ActorStart` stays on `Order`, beside `Order.StartActor` of `ActorOrder`. A search of `src/` and `tests/` for `StartActorDeprecated` is empty.
- **Request name.** `EventBody.ActorStart` and `(|ActorStart|_|)` keep those names. Both build and match `Order.ClientRequestActor`.
- **Wire.** `encodeOrder` and `decodeOrder` keep wire kind `startActor` for `Order.ClientRequestActor`. Wire kind `actorOrder` stays on `Order.StartActor`.
- **Log arm.** `historyBody` keeps the `Order.ClientRequestActor` arm and returns `Ok (EventBody.Order order)`.
- **Queue.** `MailboxHost.postPlacedOrder` is the queue entry for an arbitrary `PlacedOrder`. For `Order.ClientRequestActor` it returns `Error "ClientRequestActor is retired"` before `placeForCaller`. `postActorOrder` and `postBuiltOrder` place `Order.StartActor` and call `postPlacedOrder`.
- **Other posts.** `messageForPosted` builds `PlaceOrder` for Undo, Redo, and a Change whose id is zero. `parseBuild` builds `PlaceOrder` for a Change of origin Parse.
- **Place.** `placeOrder` replies with `MailboxHost.clientRequestRetired` for `Order.ClientRequestActor`.
- **Direct append.** `finishReadyStart` appends `EventBody.ActorStart` when the start returns a secret. `dispatchStartLoadSaveCommand` is the only caller of `dispatchActorStartResult`. `CoreActorPool.startLoadSaveCommand` returns `Ok None` for Desk and Git, so `dispatchActorStartResult` returns before `finishReadyStart`. The ticket lists `finishReadyStart` under Do not load.
- **Conversion.** `dispatchPostEvent` matches `EventBody.ActorStart`. `postActorStart` queues `?test` through `queueAdmittedTest`, `?ai` through `queueAdmittedAi`, and a query through `queueAdmittedQuery`. Each builds an `ActorOrder`, and `postBuiltOrder` places `Order.StartActor`.
- **Client.** `RunLaunch.queueStart` builds `EventBody.ActorStart`. `src/Client` builds no `ActorOrder`.

## 3. Arch

- **Place actor order.** The checked line matches `postPlacedOrder` and `placeOrder`. The error text is `ClientRequestActor is retired`, and that order stays off the queue.
- **Log actor order.** The checked line matches `historyBody` for `Order.StartActor` and `Order.ClientRequestActor`.
- **Named start today.** The checked line says a placed `Order.ClientRequestActor` stays out of the mailbox log. `placeOrder` replies with the retired error for that case.
- **Posted run.** The checked line matches `postActorStart`. The admitted names are `test`, `ai`, and a query. Every other name replies `Error "unknown actor"`.
- **Current op.** [Op](doc/current/op.md) says `Order.ClientRequestActor` carries `ActorStart` and `EventBody.ActorStart` builds that order. `src/Shared/History.fs` matches that line.

## 4. Tests

- **Suite.** `bash scripts/check-test.sh 2966` printed `DONE` and `/tmp/ambit-full-suite-20261113-163717.log`. The log ends `full suite finished`. `scripts/check-test.sh` has no execute bit, so the bare path reports `Permission denied`.
- **Server.** `tests/Server.Tests` passed 765, failed 0, skipped 1. The skip is ``Change.upload structure persistence preserves existing file mtime``.
- **Shared.** `tests/Shared.Tests` passed 1940, failed 0, skipped 3. The skips are ``reuses existing owned path without duplicating``, ``planServerFilePresentOps transitions only matching absent files``, and ``listForPush without git still returns walk``.
- **Refusal.** ``postPlacedOrder refuses ClientRequestActor and appends nothing`` calls `MailboxHost.postPlacedOrder` with `Order.ClientRequestActor`, expects `Error "ClientRequestActor is retired"`, and expects `eventPast` to match the log from before the call.
- **Wire.** ``Ev JSON round-trips each EventBody.Order case`` builds `Order.ClientRequestActor`, expects the encoded event to contain `startActor`, and expects the event to round-trip.

## 5. Miss

- No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- **Accepted.** **Problem.** These lines still use the title 16 — Delete the old Order cases.
  - These five say 16 deletes `Order.StartActorDeprecated`.
    - [15 — Delete the old Actor starts](plan/actor-as-client/issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md)
    - [18 — Place ActorOrder through placeOrder](plan/actor-as-client/issues/18-place-actororder-through-placeorder/18-place-actororder-through-placeorder-ticket.md)
    - [20 — Move Desk Load and Desk Save](plan/actor-as-client/issues/20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md)
    - [25 — Remove the coreMailboxStartActor door](plan/actor-as-client/issues/25-remove-the-coremailboxstartactor-door/25-remove-the-coremailboxstartactor-door-ticket.md)
    - [27 — Reject unknown Run commands before the queue](plan/actor-as-client/issues/27-reject-unknown-run-commands-before-the-queue/27-reject-unknown-run-commands-before-the-queue-ticket.md)
  - [24 — Move the remaining ActorStart writers onto StartActor](plan/actor-as-client/issues/24-move-the-remaining-actorstart-writers/24-move-the-remaining-actorstart-writers-ticket.md) says 16 owns that case, its codec, and its log arm.
  - [26 — Retire the posted named start](plan/actor-as-client/issues/26-retire-the-posted-named-start/26-retire-the-posted-named-start-ticket.md) says 16 keeps `Order.StartActorDeprecated`.
- **Fix.** Point those lines at 16 — Rename the client start request to ClientRequestActor. The case stays, under the new name.
- **Accepted.** **Problem.** [Message shape](plan/core-refinement/cqrs-messages.md) says 16 deletes `Order.StartActorDeprecated` and that no reader keeps an old row.
- **Fix.** Say the case is `Order.ClientRequestActor`, the wire kind stays `startActor`, and `historyBody` still accepts a stored row.
