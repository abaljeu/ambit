# 122 — Document state moves by Notice

Branch: dev.

Copied the ticket headings from [Ticket work](plan/context/ticket-work.md) into [122 — Document state moves by Notice](plan/core-refinement/issues/122-document-state-moves-by-notice.md).

Headings added, in template order:

- 1. Type
- 2. Blockers
- 3. Look at
- 4. Context
- 5. Changes
- 6. Tests
- 7. Done

Filled slots from the direction already on the ticket:

- Type is `coding`.
- Changes keeps the Notice direction: send Notice from Load, Parse, and Persist; push Notices so the result is idempotent and identical to SetDocumentState; then drop SetDocumentState and it still works.
- Tests keeps the direction: no new test is required, and the old tests should still work.
- Done keeps the three template pointer lines.

Blockers, Look at, and Context stay empty headings. The template shows those headings with no pointer lines, and this pass adds no file list or design.
