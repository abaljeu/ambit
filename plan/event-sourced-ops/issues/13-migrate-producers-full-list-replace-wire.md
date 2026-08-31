# 13 — Migrate producers to full-list Replace wire shape

**Context:** Wire contract is full-list Replace only: `parentId` plus the parent's complete `oldList` and `newList` at common prior ([[../details/replace-amendment.md]] §1). Partial span Replace — any non-zero `index`, or lists shorter than the full parent children — is not valid on the wire. Issue 05 amend/merge applies to wire-valid full-list posts; current Client and Shared planners still emit span/partial ops (catalogue: [[../reports/wire-full-list-replace-contract.md]], [[plan/relaxed-concurrency/replace-span-cas-feasibility.md]]).

**What to build:** Each producer that plans child-list edits emits one full-list Replace per parent per Change (`index = 0`, complete `oldChildren` / `newChildren` from the planning anchor). Span helpers may remain internal to apply/replay until §10 field rename. No new Reject path; amended full-list posts continue through ticket 04 rewind/replay.

**Blocked by:** 05 — Child-list Accept Both (merge/amend path)

**See also:** [[../details/replace-amendment.md]] §6, §10, [[../details/conflict-resolution.md]] Kind 3

**Status:** done

- [x] Every Client/Shared Change planner emits only full-list Replace on the wire (no span at `index > 0`; no partial lists at `index = 0`).
- [x] Cross-parent move, paste, delete, import, join, lazy-load reconciliation, and file-node insert paths covered.
- [x] Focused tests updated; existing merge/amend tests still green.
