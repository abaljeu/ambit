# Commit leftover 25 review docs

Date: 2026-09-06

## Product commit

- Hash: `eba2f5383210485a3fa3a00214eb78ba89447359`
- Message: Bind Browser Changes on the Core object so HTTP posts one nested handle instead of unpacking admission.
- Includes [[../issues/25-bind-changes-at-core-seam.md|25 (Bind Changes at the Core seam)]] (Status `done`), [[implement-issue-25.md]], and the `src/` / `tests/` bind. Issue Time/Status was already in that commit. No leftover product files.

## This follow-up

[[scripts/commit.sh]] with an explicit file list. Records the independent review and the pre-implement read note so they sit with 25.

- [[code-review-issue-25.md]] — independent review of `eba2f53`. Parent **pass**. Standards: **1 (Mysterious Name)** on the refuse-family test; **2 (Middle Man)** on `changesBound`. Spec: **1 (Tests hit a test-built bind)** on HTTP Adapter tests. Parse leftover is out of 25.
- [[read-issue-25.md]] — read note before implement. Status at read was `needs-triage`.

Project Stage stays `active`. [[plan/index.md]] was not staged: the diff is llm-connector wording, skills-cleanup summary, and row order, not a Core creation Stage change.

## Left unstaged

- [[CONTEXT.md]] (Run Agent glossary)
- [[plan/index.md]] (llm-connector and skills-cleanup)
- [[plan/llm-connector/]]
