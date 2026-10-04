# 01 — Incremental search fill and resume

**Type:** research
**Status:** defined
**Blocked by:** None

## 1. Question

How does the existing incremental client search fill one screen and resume?

Report current behavior only. Do not propose the Search spec. Do not treat the destination's trash rule or page bound as a finding.

1. **Start** — What starts the walk, and which Graph it walks.
2. **Screen** — What one screen of hits is, and what asks for the next screen.
3. **Pick** — What `searchPickSetRoot` does on a pick.
4. **Trash** — Whether the walk skips trash, and what changes when the walk starts in trash.
5. **Root** — What root the walk uses.

Primary sources start at [ViewModelSearch.fs](src/Shared/ViewModelSearch.fs) (`searchNodes`, `startSearch`, `SearchCursor`, `takeResults`) and [SearchDialog.fs](src/Client/SearchDialog.fs). Cite each claim from those sources or from tests that lock them.

Write the findings to [incremental search fill and resume](plan/online-search/reports/incremental-search-fill-and-resume.md).
