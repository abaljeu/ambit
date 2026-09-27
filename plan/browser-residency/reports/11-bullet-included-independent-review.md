# 11 — Migrate Bullet and Included readers — independent review

## Standards

1. **Must-fix — bare ids in agent-produced reports.** [Refer by name](.agents/rules/refer-by-name.md) requires every issue, section, and list item reference to include its name, but [11 — Migrate Bullet and Included readers — review](11-bullet-included-review.md) uses bare references such as “ticket 26.2,” “spec story 11,” and “33.1”; [11 — Migrate Bullet and Included readers — Spec review](11-bullet-included-spec-review.md) names stories only as “10, 11, 12, 26, 33, and 34”; and [Shared test failures after 11 — Migrate Bullet and Included readers](11-shared-test-failures-diagnosis.md) repeatedly says “ticket 11” and refers to hypothesis list items only as “A+C” and “D+F.” The mechanical scan reported six of these occurrences; inspection found the additional bare story and hypothesis references.

## Spec

No findings.

Summary: Standards 1 finding; worst is the must-fix bare-id violation. Spec 0 findings; no issue.
