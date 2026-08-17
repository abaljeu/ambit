# Warm parse dual-Owner HITL audit

Verdict: not established

Board advice: block [[tmp/warm-parse-dual-owner-fix.md]] on the user HITL check below. Do not remove it as accomplished.

## Claimed outcome

The exact claim requires a Current File Node to run a warm File Load after the reclaim-versus-TRASH fix, with no dual-Owned result. Shared implementation and tests are supporting evidence, but they do not satisfy this explicit HITL claim.

## Durable evidence

- Commit `b5a4ea827a94598b363bd15fb7b7a978c6bd0397` added the reclaim-versus-TRASH implementation in [[src/Shared/documents/DocumentColdParse.fs]], the focused regression in [[tests/Shared.Tests/ImportDocumentTests.fs]], improved ownership diagnostics in [[src/Shared/History.fs]], and the WORK item in the same commit.
- The linked [[tmp/warm-parse-dual-owner-fix.md]] is absent, is ignored by the repository tmp rule, has no tracked git history, and therefore contains no available durable HITL result.
- A git content-history search finds only the commit that introduced the WORK item. No later commit records its removal or an accomplished manual result.
- The current planner excludes an Owned Node reclaimed by another overlay parent from Delete-to-TRASH planning, then applies the Children replacement. The focused test `planParseFile Current warm overlay reparent does not dual-Own` checks that the Change applies, full ownership validation succeeds, the Node is Owned by the new parent, and the Node is not in TRASH.
- On 2026-08-16, the exact focused test passed: failed 0, passed 1, skipped 0.
- [[tmp/opendrive-ownership-load-fix.md]] records a later and different warm plain-text Ref-deferral fix. Its residual-risks section still requires a HITL Load, and its WORK section says the reclaim-versus-TRASH item remains distinct. It does not establish this claim.
- No durable verification record or user statement says that the exact Current warm File Load reparent case passed after commit `b5a4ea8`.

## Result

The code-level regression is present and green. The required Browser-to-Server Load path was not manually verified in the available durable evidence. The verdict must remain `not established`.

## Smallest manual check

1. Use a mapped text File Node that is already Current with this content:

```text
Section
	Item
Other
```

1. Change the mapped file on disk to this content:

```text
Section
Other
	Item
```

1. Select that File Node and run Load.
1. Report whether Load finishes without an ownership HTTP 400, Item has one Owned appearance under Other and none under Section or TRASH, and Check Graph reports no dual-Owned error.

One answer is enough: “Pass” if all checks hold, or provide the Load or Check Graph error text.
