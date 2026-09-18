# Code review — [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md) 4.2 Other tests do not lock serials

Range: uncommitted vs `HEAD` (`git diff HEAD`). Axis files: [code-review-48-standards](code-review-48-standards.md), [code-review-48-spec](code-review-48-spec.md). Prior combined review of the EventId DU work stays at [code-review-48-eventid-zero-and-positive-int](code-review-48-eventid-zero-and-positive-int.md). A report is not approval.

## Standards

Range: uncommitted vs `HEAD` (`git diff HEAD`), plus untracked [EventIdFixtures](tests/Shared.Tests/EventIdFixtures.fs). [core-agent-behavior.md](.agents/rules/core-agent-behavior.md) in the tree is out of this ticket.

### 1. Hard violations

1. File over 400 lines — [DatabaseProjectionContractTests](tests/Server.Tests/DatabaseProjectionContractTests.fs) FILE 624->625. Rule: [fsharp-source.md](.agents/rules/fsharp-source.md) 400 lines or less per file; if a file is already longer, only restructure to split if the change would increase it.
2. File over 400 lines — [DbAgentTests](tests/Server.Tests/DbAgentTests.fs) FILE 597->602. Same rule. Line wraps for `storedId` and SQL interpolation increased the file.
3. Labeled link not project-root relative — [markdown-writing.md](.agents/rules/markdown-writing.md): other local files use a path relative to the project root. The implement comment originally used `../../../tests/Shared.Tests/EventIdFixtures.fs`. That path was corrected to [EventIdFixtures](tests/Shared.Tests/EventIdFixtures.fs) after this axis ran.

### 2. Judgement smells

Per [SMELLS.md](.agents/skills/code-review/SMELLS.md), smells are heuristics, not hard violations.

1. Shotgun Surgery — One serial unlock edits many test files. Typical hunk: `EventId.fromJson 4` replaced by `EventIdFixtures.storedId 4`. The fixture gathers the walk; the scatter is the ticket surface.
2. Duplicated Code — Wire asserts repeat `EventId.toJson (EventIdFixtures.storedId n)`.

### 3. Checks without findings

[EventIdFixtures.storedId](tests/Shared.Tests/EventIdFixtures.fs) starts at `EventLog.empty.nextId` and walks with `EventId.next`. EventId builder tests in [EventTests](tests/Shared.Tests/EventTests.fs) still use `fromJson`. No added F# line over 100 characters. No product Shared or Server source in this range.

Worst hard issue: DbAgentTests growth 597->602.

## Spec

Range: uncommitted vs `HEAD` (`git diff HEAD`). Spec: [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md). Spec Task spawn failed (`resource_exhausted`); the Spec file is the same brief run by the parent.

### 1. Missing or partial

1. **Wire JSON fixtures still name serials** — Spec: “They obtain stored ids from EventLog. They do not lock 1, 2, 3, or another stored serial.” [SerializationTests](tests/Shared.Tests/SerializationTests.fs) still decodes `{"r":3,...}` and `{"r":4,...}` and asserts `storedId 3` / `storedId 4`. Those literals lock wire 3 and 4.

### 2. Scope creep

1. **core-agent-behavior.md** — Spec What-to-build is EventId, EventLog, drafts, tests, unique persist. Diff includes [core-agent-behavior.md](.agents/rules/core-agent-behavior.md) dropping `dotnet build` from the toolchain gate. That file is not this ticket.

### 3. Looks implemented but wrong

1. **storedId ordinal equals today’s serial** — Spec: “They obtain stored ids from EventLog.” [EventIdFixtures.storedId](tests/Shared.Tests/EventIdFixtures.fs) starts at `EventLog.empty.nextId` and walks `EventId.next`. While empty `nextId` is `Int 1`, `storedId n` is still `Int n`.

### 4. In spec and present

EventId `Zero | Int`, `next` of Zero, EventLog assigner, get-all via Zero, drafts at Zero, and EventId builder `fromJson` tests are already in Shared. Non-builder `EventId.fromJson N` constructors are gone except those builder facts.

## Summary

Standards: 3 hard, 2 judgement; worst is [DbAgentTests](tests/Server.Tests/DbAgentTests.fs) growth. Spec: 1 missing/partial, 1 creep, 1 wrong; worst is SerializationTests JSON still locking wire 3 and 4.
