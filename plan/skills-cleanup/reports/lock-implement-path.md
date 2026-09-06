# Lock: `/implement` is the entry

Earlier locks stay. This lock answers the implement-path question only. Keep QA is locked in [[plan/skills-cleanup/reports/lock-keep-qa.md]]. Triage deletion is locked in [[plan/skills-cleanup/reports/lock-delete-triage.md]].

**Entry.** Alan wants `/implement`. The entry skill is [[.agents/skills/implement/SKILL.md]].

**Ditch later.** [[.cursor/rules/testing-workflow.mdc]] (the stop-for-review always-apply loop) is ditched. Do not delete or edit it in this charting pass.

**Augment by reference.** [[.agents/skills/tdd/SKILL.md]], [[.cursor/skills/implement-fsharp-feature/SKILL.md]], and [[.cursor/skills/add-shared-test/SKILL.md]] (implement-fsharp-feature already points at add-shared-test) stay. They must augment `/implement` and each other, not contradict or duplicate a second loop. Do not treat tdd as leftover-to-delete.

Later obedience punch list (do not do in this charting pass):

- Remove [[.cursor/rules/testing-workflow.mdc]] and drop it from always-apply / gambol.mdc indexes.
- Make tdd and implement-fsharp-feature agree with `/implement` by reference (no copied red-green or stop-for-review loop).
- Ask-Matt already routes to `/implement`; keep that, and name the F# augmentations so it agrees with gambol.mdc (Q1).

This pass did not edit skill or rule bodies.
