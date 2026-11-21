# 07 — Start the test command

Ticket: [[07-dispatch-the-test-command-ticket.md]]
Spec: `plan/actor-as-client/spec.md` is absent. This project has no `spec.md`. The ticket is the spec.
Arch: [[plan/actor-as-client/arch.md]]

## 1. Standards

1. **Scan.** `python` is absent. `python3 scripts/standards-scan.py --diff origin/staging` printed the touched bindings. Each is under 40 lines. The longest is `queueTestOrder` at 35 lines. No `LONG`, `TAB`, `MUTABLE`, `BARE_ID`, `PHRASE`, or `FILE` line.
2. **Door adapters.** **Judgement.** Duplicated Code. `doorPost` and `doorStop` in `src/Server/TestActor.fs` repeat `CoreDoor.actorChanges`. Source: [[plan/context/SMELLS.md]]. The hunk is `CoreDoor.postEvents (CoreDoor.forCaller (actorCaller secret) door)`.
3. **Two actor callers.** **Judgement.** Duplicated Code. `actorCaller` is private in `src/Server/TestActor.fs` and in `src/Server/Core/CoreMailboxLoop.fs`. Source: [[plan/context/SMELLS.md]]. The hunk is `{ authority = Authority "Actor"; name = ""; secret = secret }`.
4. **Copied order builder.** **Judgement.** Duplicated Code. `postOrder` is the same helper in `tests/Server.Tests/TestActorHelloTests.fs` and `tests/Server.Tests/TestActorCommandErrorTests.fs`. Source: [[plan/context/SMELLS.md]]. The hunk is `MailboxHost.postActorOrder host testCaller order`.
5. **Stale schedule comment.** **Judgement.** Mysterious Name. `tests/Server.Tests/TestActorHelloTests.fs` still says the reply is from `startActor`. The fact calls `postActorOrder`. Source: [[plan/context/SMELLS.md]]. The hunk is `startActor replies after schedule`.

## 2. Spec

1. **One job.** A Run whose command text selects `test` queues `Order.ActorOrder`. `postActorStart` in `src/Server/Core/CoreMailboxActors.fs` calls `MailboxHost.postActorOrder`. The function on the order is `TestActor.functionActorAt`, closed over the Run actor node id and the Run focus id.
2. **Other names.** `postChosenStart` still calls `startChosen`. `startActor` still starts names other than `test`.
3. **Contract.** `RouteRegistration` boot actors list the `ai` actor. The `test` pair is removed from `pool.register`.
4. **Bounds.** The diff adds no route, no handler, no `ActorDispatch.fs`, no `actorForCommand`, and no `Order.StartActor` or `Order.RunActor`.
5. **Named tests.** The hello facts and the `?test` command-error facts call `MailboxHost.postActorOrder`.
6. **ActorStart row.** **Extra.** `queueTestOrder` appends `EventBody.ActorStart` for the submission id, then queues the order. The one job asks for `Order.ActorOrder` on the queue. The start row keeps the events-door ack and a replay of that submission.
7. **Ping on the order path.** **Extra.** `functionActorAt` routes `ping` to `pong` because it shares `runBehavior` with the pool `functionActor`. The old registered `actorFn` treated a non-hello body as `ActorFailed`.
8. **Pool door.** **Extra.** `poolActor` in `src/Server/Core/CoreMailboxLoop.fs` builds `ActorChanges` with the credential `startFunction` mints. The ticket outputs do not name that file. [18 — Place ActorOrder through PlaceOrder](../18-place-actororder-through-placeorder/18-place-actororder-through-placeorder-ticket.md) places the client's `Caller` on `Mailbox.PlaceOrder`. The actor posts with Authority `Actor` while `pool.isLive` holds, which matches the receipt rule.

## 3. Arch

1. **Curried name.** Alan, 2026-11-05: keep `functionActor` for the pool and `functionActorAt` for the order. Arch Test command shows `functionActor : NodeId -> ActorFunction`. This change does not rename. `functionActorAt` also takes the focus id, because `graphNow` does not set focus to the order focus. Not a blocker after that instruction.
2. **Posted run.** `postActorStart` reads the actor node id, applies the test function, builds `ActorOrder` with `focusId` and `actorName`, and calls `postActorOrder` with the client `Caller`.

## 4. Tests

1. **Suite.** `bash scripts/check-test.sh 6320` printed `DONE` and `/tmp/ambit-full-suite-20261105-183814.log`. The log ends `full suite finished`. `tests/Server.Tests` passed 744, skipped 1, failed 0. `tests/Shared.Tests` passed 1933, skipped 3, failed 0. `scripts/check-test.sh` is not executable. The bare path prints `Permission denied`.
2. **Hello seam.** ``TestActor hello posts one Owned child text hello under Focus`` calls `postActorOrder` and asserts one Owned child text `hello` under focus.
3. **Command-error seam.** ``TestActor non-hello command fails without Owned child`` and ``TestActor ?test hello posts Owned hello and succeeds`` call `postActorOrder`.
4. **Other names in the same file.** ``unregistered Actor name fails start`` and ``production events door records a failed ActorStop for an unknown actor`` still enter through `startActor` or a posted `ActorStart`. They assert the unregistered-name failure. Process step 3 keeps that path for names other than `test`.

## 5. Miss

1. No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

1. No findings.
