# Spec review — 09 framework failure preserves children

Range: `99a13d9a...HEAD` (`8aee70ee`, `59eb7b7e`, `24238b4f`). Workplace: `cursor/code-review-framework-failure-preserves-children-60c8` at `24238b4f`. Spec: [09 — Agent failure preserves children](../issues/09-agent-failure-preserves-children.md) §1–§2 and [llm-connector architecture](../arch.md) Story path **Agent failure preserves children** hops 1–2. §3 is out of scope.

This report is not approval. Ticket Status stays `coded`.

## Faithfulness

Tests-only plus hop `[x]` is faithful for this proof ticket. No Server or Shared edit is in the delta. [TestActor](../../../src/Server/TestActor.fs) already returns `ActorFailed` without hello (`Op.Replace` of Focus Children). `actorStop` writes `EventBody.ActorStop(focusId, ActorFailed)` with empty `commandName`. `finish` drops the live row and secret. The new fact in [TestActorCommandErrorTests](../../../tests/Server.Tests/TestActorCommandErrorTests.fs) is the §2 proof that older non-hello facts did not give (they did not seed Children).

Focused tests: `TestActorCommandError` 8 passed; `TestActorHello` 8 passed.

## (a) Missing or partial

1. Partial vs "queue Failed → ActorFinished and drop live row/secret" — the new fact asserts live Focus is gone (`liveFocusIds`). It does not observe the secret after `finish`.
2. Partial vs "Run Command text that selects TestActor and ends ActorFailed (e.g. `?test unknown`)" — the proof seeds that text and calls `CoreMailbox.startActor`. It does not Run through `/ambit/command`.

Hop 2 is observed as `ActorFailed` plus one ActorStop after ActorStart. `EventBody.ActorStop` has no provider field. The test does not inspect Graph Event JSON. For TestActor that is enough.

## (b) Scope creep

No product behaviour was added. Ticket rewrite, hop rewrite, project notes, and the proof report stay inside the named range. `project.md` Stage `slice` → `build` is bookkeeping, not a spec behaviour.

## (c) Looks implemented but wrong

None. Seeded Owned Children stay. EventLog has one Change (the seed). Tail after ActorStart is one ActorStop `ActorFailed`. Live Focus row is gone. Hello Replace is not on this path.
