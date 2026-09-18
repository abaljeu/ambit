# Code review — [34b — Outside Core lifecycle proof](../issues/34b-outside-core-lifecycle-proof.md)

Independent review. Not approval. Ticket Status left `coded`.
**Pin:** current 34b seams at `HEAD` `936d2e156fe00b13c2a7dd19a650a029fe15565a`. Pre-34b `271ba142` three-dot is mixed with later SES and stretch; that dump is not the review range. Post-SES remade Change → Ev, ActorStarted → `EventBody.ActorStart`, ActorFinished → `EventBody.ActorStop`, and mailbox History → EventLog. Those remaps are the current 34b fulfillment, not 34b defects.
**Spec:** [34b — Outside Core lifecycle proof](../issues/34b-outside-core-lifecycle-proof.md); arch Story path [Outside Core lifecycle proof](plan/core-creation/arch.md); notes [34b-corrections](34b-corrections.md), [34b inject actors and mailbox History](34b-inject-actors-mailbox-history.md).
**Mechanical scan:** [CoreMailbox.fs](src/Server/Core/CoreMailbox.fs), [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs), [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs), [CoreRuntime.fs](src/Server/Core/CoreRuntime.fs), [CoreChanges.fs](src/Server/Core/CoreChanges.fs), [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs), [TestActor.fs](tests/Server.Tests/TestActor.fs), [TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs).
**Focused tests:** `dotnet test tests/Server.Tests --filter FullyQualifiedName~TestActorHello` — 8 passed, 0 failed.

## Standards

### Hard violations

- [TestActor.fs](tests/Server.Tests/TestActor.fs) `dispatch`: `let mutable result` plus `try`/`with` and `failwith "test exception"`. [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md): no mutable; no Exceptions (use Error types).
- [TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs) `waitForActorFinished` / `waitForLiveRowDrop`: `let mutable found` / `dropped` in poll loops. Same no-mutable rule.
- [TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs) 470 lines; [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) 593 lines (owns `startActor` / `actorStop` / `eventHistory`). Same file: ≤400 lines.
- `34b section7 outside proof` lines 325–434 (110); `TestActor hello observes ActorStarted before output` lines 204–254 (51). Same file: ≤40 lines/function.
- [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) `runStartActor` line 98 (109 chars): `"graphIds required: client must provide Included context (SiteMap under Zoom, honoring Fold)"`. Same file: ≤100 chars/line. (`runMsg` at 40 is at the cap, not over.)
- Draft Ev / `ActorStart.eventId` minted with `EventId.fromJson 0` in HelloTests and DoorTests. [.agents/rules/core-api.md](.agents/rules/core-api.md): `fromJson`/`toJson` only when serializing; drafts use `EventId.zero`.
- Unused `actorCaller` in HelloTests (lines 68–71, no call sites). [.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md): remove unused bindings your change introduced.

Confirmed present, not violations: `CoreMailbox.eventHistory` / `startActor` / `actorStop`; `TestActor.hello` uses `EventId.zero`. Arch-lock mutable live table in `CoreActorPool.create` not flagged.

### Judgement smells

- **Mysterious Name** — hello draft claims Browser, then posts as Actor: `authority = Authority "Browser"` then `coreChanges.asCaller(caller).postEvents` with `Authority "Actor"` ([TestActor.fs](tests/Server.Tests/TestActor.fs) 15–29). `prepare` overwrites authority; the Browser field is leftover.
- **Duplicated Code** — each HelloTests fact repeats NewNode/`actor-test`/postGraphOnly/`startActor`/wait.
- **Duplicated Code** / **Speculative Generality** — `admitCaller` re-does `CoreAuth.admit (pool.isLive …)` while `CoreActorPool.admit` is unused by the mailbox.
- **Repeated Switches** — `Authority "Actor"` vs `_` in `admitCaller` and again in `dispatchActorStop`.

## Spec

Question for this axis: are the demands of [34b — Outside Core lifecycle proof](../issues/34b-outside-core-lifecycle-proof.md) true in the software at `HEAD`? Extra behavior is ignored. SES remaps are the current fulfillment. Persist-across-restart is out of scope.

### Demands that hold

§1 one mailbox door (`startActor`, `actorStop`, `postEvents`, `eventHistory`). §2 in-loop `pool.startActor`, EventLog ActorStart, then `pool.schedule`; reply does not wait for the body; Actor `postEvents` admits only while `isLive`; `ActorStop` then `finish` drops the live row and secret; public identity stays on EventLog. §3 client `graphIds` (no server Zoom expand); `actor-*` CSS or text selects `test`; `startActor` creates identities and the live row only. §4 one mailbox EventLog; ActorStart/ActorStop have no Ops so they are not Undo targets; exactly one ActorStop on the hello path. §5 TestActor interprets `hello`, posts one Owned child text `hello` under Focus, then `ActorStop ActorSucceeded`; no asserts in TestActor. §6 `CoreBoot.Actors` is injected; CoreRuntime does not hardcode TestActor; production `Actors = []`. §7 outside proof enters at CoreMailbox, no HTTP; Graph shows the hello child; newest-head order is ActorStart then output then one ActorStop; live row is gone after finish; secret admit is `pool.isLive`, so the dropped row no longer admits.

### Notes that do not falsify 34b

§7.5 “secret no longer admits output” is true in software (`finish`/`drop` removes the live map entry; Actor admit is `isLive`). The §7 test checks `liveFocusIds` empty, not a second `postEvents` with the old secret. Same fact.
If ActorStart persist fails after `startActor`, the live row is not dropped. That error path is not the hello demand. Happy-path 8 TestActorHello tests passed.
`schedule` passes the StartActor Browser handle; TestActor rebinds Actor + secret via `asCaller`. Admit still requires the live row. Hello draft authority Browser is overwritten by Core from the admitted Caller.

## Summary

Standards: 7 hard, 4 judgement. Worst in-axis: TestActor `mutable` + Exceptions, and the 110-line §7 test (file also over 400).
Spec: 0 unmet 34b demands. Worst in-axis: none. Residuals are test-proof style and a persist-fail live-row edge, not a failed checkbox.

**Recommendation: approve → Status done.** 34b demands are true at `HEAD`. Happy-path `TestActorHello` is 8 passed / 0 failed. Standards notes (test mutability, file/function size, `EventId.fromJson 0` in fixtures, unused `actorCaller`) and the persist-fail live-row edge are non-blocking. Do not rewrite 34b for SES remaps; post-SES already owns that repair. Ticket Status left `coded` (Ambot sets `done`).
