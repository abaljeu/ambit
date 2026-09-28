# 19 — File Newer / Graph Newer

**Type:** grilling
**Status:** done
Blocked by: [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md)
Actual: 5m

## 1. Question

- [x] How do File Newer and Graph Newer relate to Unparsed, Persist, and Parse?
- [x] Is there a conflicted DocumentState when both sides changed?

## 2. Answer

Locked 2026-09-28 (Alan, chat). Related formulation to [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md) and [18 — Parse Actor stack and file-lock ownership](18-parse-actor-stack-and-file-lock-ownership.md).

File Newer / Graph Newer is the formulation. In principle a conflicted state exists. **Resolution is not to have the conflicted state.** Do not invent a Conflicted `DocumentState`.

**File Newer and Unparsed are equivalent.**

If something other than Persist changes the file, Unparsed applies and remains until Parse releases it. Git Load pull is that case: [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md) step 3 sets Unparsed on each modified file while Workspace is still Reconciling.

After Parse: if no merging → everything Current. If merging → Graph is newer.

If edits occur when Graph == file, Graph becomes newer.

Simpler rule:

- If anything changes the file → set Unparsed.
- If anything changes the Graph → set Unpersisted.
- Parse takes precedence.
- After Parse and Unparsed is off, Persist the Unpersisted.

Unpersisted is Graph Newer. It is a new Graph state alongside Unparsed (`DocumentState` today is `Current` | `Unparsed` | `NoServerFile` in [[src/Shared/Model.fs]]). That is implement, not an open decision.

Pipeline alignment: github → file → Unparsed → parse → (merge) graph → Unpersisted → Persist → file. Persist does not set Unparsed.

Map gist: [[../map.md]] Decisions so far item 19.

## Notes

- Reconciling (Workspace / Directory) stays the lock in [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md). It is not File Newer or Graph Newer.
- Parse stack and Unparsed ownership: [18 — Parse Actor stack and file-lock ownership](18-parse-actor-stack-and-file-lock-ownership.md). Parse takes precedence; Parse releases Unparsed; then Persist Unpersisted.

## Comments

- 2026-09-28: Alan locked in chat. Status `done`. File Newer = Unparsed. Graph Newer = Unpersisted (new). No Conflicted state. Parse first; then Persist Unpersisted.

## Time

- 2026-09-28 5m — recorded File Newer / Graph Newer formulation from chat
