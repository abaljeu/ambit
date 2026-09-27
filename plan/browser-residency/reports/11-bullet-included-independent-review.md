# 11 — Migrate Bullet and Included readers — independent review

## Standards

1. **Must-fix — bare ids in agent-produced reports.** [Refer by name](.agents/rules/refer-by-name.md) requires every issue, section, and list item reference to include its name, but [11 — Migrate Bullet and Included readers — review](11-bullet-included-review.md) uses bare issue, specification-story, and story-subpoint ids; [11 — Migrate Bullet and Included readers — Spec review](11-bullet-included-spec-review.md) names six stories only by number; and [Shared test failures after 11 — Migrate Bullet and Included readers](11-shared-test-failures-diagnosis.md) repeatedly names the issue by number alone and combines hypothesis list-item ids without their names. The mechanical scan reported six occurrences; inspection found the additional bare story and hypothesis references.

## Spec

No findings.

Summary: Standards 1 finding; worst is the must-fix bare-id violation. Spec 0 findings; no issue.
