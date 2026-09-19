# Code review: Cancel by Focus

Independent two-axis review of [10 — Cancel by Focus](plan/llm-connector/issues/10-cancel-by-focus.md). Range: three-dot `origin/staging...HEAD` (merge-base `f4bdf872`; tip `6230600a`). Spec: that ticket plus [llm-connector architecture](plan/llm-connector/arch.md) Story path **Cancel by Focus**, modules **CoreMailbox / CoreMsg / CoreActorPool**, **Run Agent Actor**, **CloudAgents**, Locked **CloudAgents DLL interface**, **CloudAgents setFake**, and **Live Actor chrome**. Subject: CoreMailbox `cancelByFocus`; Run Agent cancel token; CloudAgents `setFake` `waitForCancel`; `liveFocusIds` drop; no Browser chrome. Axis drafts: [Standards axis](code-review-standards-cancel-by-focus.md), [Spec axis](code-review-spec-cancel-by-focus.md). Mechanical scan: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` printed FILE-growth on [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) 394→423, [History.fs](src/Shared/History.fs) 742→743, and two test files (size rule does not apply to tests). measure-fs-size: new product bindings ≤26 lines. **Status:** stays `coded`. A report is not approval.

## Verdict

**Approve with nits.** The in-scope cancel path is present: `cancelByFocus` orders Cancelled against Change; ActorFinished is `ActorCancelled` with no Error and no response Change; Run Agent calls existing CloudAgents `cancel` and leaves the poll loop; drop is observed via `liveFocusIds`; Focus Children stay except earlier accepted Changes; Browser chrome stays on core-creation 21/22. Worst Standards hit is the 82-line test member `Change before Cancel keeps the accepted children`. Worst Spec hit is extra CloudAgents public `waitForCancel` and `fakeCancelCount` beyond the locked `start` / `poll` / `cancel` / `waitUntilComplete` face. `waitForCancel` is a hanging-`setFake` test seam, not a reshape of those four calls.

## Standards

Range: three-dot `origin/staging...HEAD` (`6230600a`). Scan printed FILE-growth on [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) and [History.fs](src/Shared/History.fs). Product `cancelByFocus` (8), `dispatchCancelActor` (24), `pollUntilDone` (26), `waitForCancel` (3): under 40, no mutable keyword. Core does not reference CloudAgents. No Browser chrome edits. Drop proof uses `liveFocusIds`. `trySecretForFocus` is the cancel lookup, not the observation surface.

### Hard violations

#### File size — [fsharp-source.md](.agents/rules/fsharp-source.md)

[CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) FILE 394→423: already over 400 or new file over 400; change increased it. [History.fs](src/Shared/History.fs) FILE 742→743: already over 400 or new file over 400; change increased it. Scan also printed [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) 592→594 and [EventTests.fs](tests/Shared.Tests/EventTests.fs) 480→490. File-size does not apply to tests.

#### Function size — [fsharp-source.md](.agents/rules/fsharp-source.md) (40 lines per function; tests included)

[CancelByFocusTests.fs](tests/Server.Tests/CancelByFocusTests.fs) member `Change before Cancel keeps the accepted children`: lines 270–351 (82 lines). Same file member `cancel by Focus accepts Cancelled and drops the live row`: lines 190–230 (41 lines).

#### Mutable — [fsharp-source.md](.agents/rules/fsharp-source.md) (do not use mutable)

[AgentRunner.fs](src/CloudAgents/AgentRunner.fs) Fake adds `cancelled` and `cancelCount` refs plus `ManualResetEvent` `cancelPulse`, on the existing Fake `ref`/`lock` seam.

#### Exceptions — [fsharp-source.md](.agents/rules/fsharp-source.md) (do not use Exceptions)

```
posted.TrySetException(
    Exception(err))
```

in the probe Actor of [CancelByFocusTests.fs](tests/Server.Tests/CancelByFocusTests.fs).

#### Plan text — [markdown-writing.md](.agents/rules/markdown-writing.md) (`[label](path)`); [refer-by-name.md](.agents/rules/refer-by-name.md) (number and name)

[spec.md](plan/llm-connector/spec.md): `[[issues/10-cancel-by-focus.md|10]]` is an Obsidian labeled wikilink and uses the id only. [map.md](plan/llm-connector/map.md): rewritten list uses `[[path|label]]`; `[[issues/09-agent-failure-preserves-children.md|09]]` uses the id only. [project.md](plan/llm-connector/project.md) ticket line `[[issues/10-cancel-by-focus.md|10 — Cancel by Focus]]` is a labeled wikilink. Notes uses markdown.

### Judgement-call smells

**Duplicated Code.** `clearFake` copied in [AgentRunnerFakeTests.fs](tests/CloudAgents.Tests/AgentRunnerFakeTests.fs), [AgentAskTests.fs](tests/Server.Tests/AgentAskTests.fs), [CancelByFocusTests.fs](tests/Server.Tests/CancelByFocusTests.fs). Three hang-then-cancel Facts share one shape.

**Speculative Generality.** Public test helpers on the CloudAgents DLL:

```
let waitForCancel (timeoutMs: int) : bool =
    Fake.waitForCancel timeoutMs
let fakeCancelCount () = Fake.requestedCancels ()
```

Counts: 9 hard, 2 smells. Worst hard: 82-line test member `Change before Cancel keeps the accepted children`.

## Spec

Spec: [10 — Cancel by Focus](plan/llm-connector/issues/10-cancel-by-focus.md); architecture Story path **Cancel by Focus**, modules **CoreMailbox / CoreMsg / CoreActorPool**, **Run Agent Actor**, **CloudAgents**, and Locked **CloudAgents DLL interface** / **CloudAgents setFake** / **Live Actor chrome** in [llm-connector architecture](plan/llm-connector/arch.md). Range: `origin/staging...HEAD`.

### (a) Missing or partial

None in the in-scope slice. Story 4.1 Browser cancel and Story 4.5 Browser live projection stay open. Ticket: “do not require Browser live-Actor UI (core-creation 21/22)”. No Client or HTTP cancel decode is in the range.

### (b) Not asked for

1. **CloudAgents public helpers beyond the locked DLL face.** Arch: “Existing public API: start / poll / cancel / waitUntilComplete (keep; do not reshape for Ambit)” and `setFake: (StartArgs -> AgentResult) option -> bool`. The diff adds `AgentRunner.waitForCancel` and `fakeCancelCount`. `waitForCancel` is a test seam: a hanging `StartArgs -> AgentResult` handler can wait for cancel without a new `setFake` type. It is not a reshape of start, poll, cancel, or waitUntilComplete. `fakeCancelCount` is extra; Poll status `Cancelled` already shows “CloudAgents cancel is requested”.

### (c) Implemented but wrong

None.

### Seams

1. **Cancel vs Change.** Ticket: “Change-before-Cancel applies; Cancel-before-Change rejects; late duplicate completion after cancel is ignored.” One mailbox runs `PostEvent` and `CancelActor` in queue order. `dispatchCancelActor` writes `ActorCancelled` then `finish` (token cancel + drop). After drop, Actor admit fails, so a late Change or `actorStop` does not append a second ActorFinished.
2. **ActorFinished.** Ticket: “ActorFinished without Error or response Change.” Event body is `ActorStop(focusId, ActorCancelled)`. EventJson encodes `result` as `"cancelled"`. Run Agent returns `CompleteCancelled` and skips `postReplace`. No Undo.
3. **CloudAgents cancel and stop poll.** Arch Run Agent Actor: “On cancel token: request CloudAgents cancel and stop.” `pollUntilDone` registers `OnCancel` and checks the token, calls `AgentRunner.cancel`, and leaves the poll loop.
4. **liveFocusIds drop.** Ticket: “observed via liveFocusIds (secrets are not an observation surface).” Proof uses `pool.liveFocusIds ()`.
5. **Children preserved.** Ticket: “Focus Children unchanged aside from earlier accepted Changes.” Cancel-before-Change keeps seed Children; Change-before-Cancel keeps the earlier accepted child.
6. **waitForCancel.** See (b). Required test seam for a hanging `setFake` handler. Not a reshape of start / poll / cancel / waitUntilComplete.
7. **No Browser chrome.** Ticket: “this ticket may prove cancel through Core / harness without UI chrome.” Seam “Browser cancel ↔ Core Cancelled” stays open.

One finding. Worst: extra CloudAgents public APIs `waitForCancel` and `fakeCancelCount`.

## Summary

Standards: 9 hard + 2 smells; worst within axis: 82-line test member `Change before Cancel keeps the accepted children`. Spec: 1 finding; worst within axis: extra CloudAgents public `waitForCancel` and `fakeCancelCount`. Verdict: Approve with nits. Status stays `coded`.
