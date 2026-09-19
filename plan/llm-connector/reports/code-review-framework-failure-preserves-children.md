# Code review — framework half of [09 — Agent failure preserves children](../issues/09-agent-failure-preserves-children.md)

Independent review. Not approval. Ticket Status left `coded`. Do not treat this report as a `done` stamp.

**Verdict: Approve with nits.**

**Pin:** implement tip `24238b4f` on `cursor/agent-failure-preserves-children-7297`. User named `origin/staging`; three-dot `origin/staging...HEAD` also contains land `99a13d9a` (08 / 11 / 12 tickets). This review is only the 09 implement delta `99a13d9a...HEAD`.

**Commits:** `8aee70ee` Unblock failure-preserves for TestActor framework proof; `59eb7b7e` Prove TestActor failure preserves Focus Children; `24238b4f` Mark 09 framework hops coded and record the proof.

**Spec:** [09 — Agent failure preserves children](../issues/09-agent-failure-preserves-children.md) §1 Framework and §2 Proof (TestActor). §3 AI-Actor erase is out of scope. Arch Story path **Agent failure preserves children** hops 1–2 in [llm-connector architecture](../arch.md).

**Mechanical scan:** `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` and `--diff 99a13d9a`. Stdout empty both times (`scan: none`). measure-fs-size skips `tests/` for 40-line discovery.

**Focused tests (this review):** `TestActorCommandError` 8 passed; `TestActorHello` 8 passed.

Axis drafts: [Standards](code-review-standards-framework-failure-preserves-children.md), [Spec](code-review-spec-framework-failure-preserves-children.md). Axes stay separate below.

## Standards

Range: `99a13d9a...HEAD` (09 implement only). Mechanical scan: none. [TestActorCommandErrorTests](../../../tests/Server.Tests/TestActorCommandErrorTests.fs) is 384 lines; [fsharp-source](../../../.agents/rules/fsharp-source.md) file-size cap does not apply to tests. Stage `build` on [llm-connector](../project.md) matches [project status](../../../doc/agents/project-status.md) First implement. Draft Events use `EventId.zero` per [core API](../../../.agents/rules/core-api.md). No added long lines; no `mutable`; no new production Exceptions; 4-space indent.

### Documented standards (hard)

#### 1. Forty-line function

[fsharp-source](../../../.agents/rules/fsharp-source.md): 40 lines or less per function. The test file-size note does not drop that rule. Scan skips `tests/` for 40-line discovery; this is from the file.

The Fact `TestActor non-hello preserves Focus Children and posts no Change` (lines 239–284) is 46 lines.

#### 2. Refer by name on [llm-connector project](../project.md)

[refer-by-name](../../../.agents/rules/refer-by-name.md): never refer by only the id; the name wraps the link.

New Notes line: `AI-Actor erase stays after [[issues/08-agent-ask-from-what-i-see.md|08]] and [[issues/12-replace-focus-children-from-reply.md|12]]`. Labels are ids only.

New Implementation tickets line: `AI-Actor erase deferred until 08 and 12`. Ids only; no names.

Pre-existing `[[path|label]]` wikilinks in `plan/` are match-existing-style. [markdown-writing](../../../.agents/rules/markdown-writing.md) labeled-link form is not a miss on those. New ticket and report links that wrap a name are fine.

### Baseline smells (judgement)

#### 1. Duplicated Code — seed helpers

`seedCommand` and new `seedCommandWithChildren` share mint, `EventId.zero` draft, `postGraphOnly`, and `requireOk`.

#### 2. Duplicated Code — fail-and-wait block

The new Fact repeats the existing Theory start / wait / `ActorFailed` / live-row-gone shape.

### Not findings

No unused leftovers from this delta ([core-agent-behavior](../../../.agents/rules/core-agent-behavior.md) Surgical). No consecutive blank lines in new Markdown. Arch hop 3 names [08 — Run Agent Actor calls CloudAgents](../issues/08-agent-ask-from-what-i-see.md) and [12 — Replace Focus Children from reply](../issues/12-replace-focus-children-from-reply.md). Surgical: test plus plan notes; no production edit.

## Spec

Range: `99a13d9a...HEAD`. Spec: [09 — Agent failure preserves children](../issues/09-agent-failure-preserves-children.md) §1–§2 and [llm-connector architecture](../arch.md) Story path **Agent failure preserves children** hops 1–2. §3 is out of scope.

### Faithfulness

Tests-only plus hop `[x]` is faithful for this proof ticket. No Server or Shared edit is in the delta. [TestActor](../../../src/Server/TestActor.fs) already returns `ActorFailed` without hello (`Op.Replace` of Focus Children). `actorStop` writes `EventBody.ActorStop(focusId, ActorFailed)` with empty `commandName`. `finish` drops the live row and secret. The new fact in [TestActorCommandErrorTests](../../../tests/Server.Tests/TestActorCommandErrorTests.fs) is the §2 proof that older non-hello facts did not give (they did not seed Children).

### (a) Missing or partial

1. Partial vs "queue Failed → ActorFinished and drop live row/secret" — the new fact asserts live Focus is gone (`liveFocusIds`). It does not observe the secret after `finish`.
2. Partial vs "Run Command text that selects TestActor and ends ActorFailed (e.g. `?test unknown`)" — the proof seeds that text and calls `CoreMailbox.startActor`. It does not Run through `/ambit/command`.

Hop 2 is observed as `ActorFailed` plus one ActorStop after ActorStart. `EventBody.ActorStop` has no provider field. The test does not inspect Graph Event JSON. For TestActor that is enough.

### (b) Scope creep

No product behaviour was added. Ticket rewrite, hop rewrite, project notes, and the proof report stay inside the named range. `project.md` Stage `slice` → `build` is bookkeeping, not a spec behaviour.

### (c) Looks implemented but wrong

None. Seeded Owned Children stay. EventLog has one Change (the seed). Tail after ActorStart is one ActorStop `ActorFailed`. Live Focus row is gone. Hello Replace is not on this path.

## Summary

Standards: 3 hard, 2 judgement. Worst in-axis: 46-line Fact (over the 40-line rule).
Spec: 2 partial, 0 missing, 0 creep, 0 wrong. Worst in-axis: proof observes live-row drop, not the secret after `finish`.

**Recommendation: Approve with nits.** §1–§2 and framework hops 1–2 hold at this tip. Nits are test function length, two refer-by-name ids in [llm-connector project](../project.md), and proof-style partials. Ticket Status left `coded`.
