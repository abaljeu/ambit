# Trim rejected-plans report

Date: 2026-08-19

## Line-count reduction

| File | Before | After | Δ |
| --- | ---: | ---: | ---: |
| [[map.md]] | 183 | 147 | −36 |
| [[design.md]] | 125 | 123 | −2 |
| [[spec.md]] | 107 | 103 | −4 |
| [[g-decision-report.md]] | 94 | 17 | −77 |
| [[client-vs-server-replan-report.md]] | 51 | *deleted* | −51 |
| [[rejection-decision.md]] | 33 | *deleted* | −33 |
| [[to-design-report.md]] | 35 | 35 | 0 |
| [[to-spec-report.md]] | 34 | 34 | 0 |
| [[project.md]] | 6 | 6 | 0 |
| **Net** | **668** | **464** | **−204** |

## Files changed

### [[map.md]]

Removed: "How the discussion started" narrative; long Event Sourcing / genesis replay rationale; extended id-anchored Replace ambiguity prose; duplicated five-point client-vs-server replan rationale (now one link to [[design.md#Client vs server replan]]); repeated G/C resolution paragraphs; verbose audit caveats under open question A.

Kept: all eight verified knowns; cheap-win decision; slice 1 Reject path; client merge-sync protocol and slice table; open questions D–F; one-line rejections with pointers.

### [[design.md]]

Merged two Rejected-deepenings bullets into one line + links. **Client vs server replan** section unchanged (canonical full rationale).

### [[spec.md]]

Trimmed Further Notes: removed weak-form "what would change in this spec" essay and redundant G/roadmap prose. **Acceptance criteria and Out of Scope bullets unchanged** (slice 1 intact).

### [[g-decision-report.md]]

Reduced to decision stub + links to [[map.md]] and [[design.md]] (WORK.md still references this file).

### Deleted

- [[client-vs-server-replan-report.md]] — fully redundant with [[design.md#Client vs server replan]].
- [[rejection-decision.md]] — decision C folded into [[map.md]]; no unique content.

### [[to-design-report.md]]

Removed stale [[rejection-decision.md]] input reference.

## Judgment calls

| Topic | Kept | Deleted |
| --- | --- | --- |
| Event Sourcing rejection | One sentence in map Decisions | Multi-paragraph genesis/parser argument |
| Id-anchored Replace ambiguity | One line + [[child-occurrence-uniqueness.md]] link | Strong-form re-litigation in map and spec Further Notes |
| Client vs server replan | Full section in design.md only | Five-point list in map, g-decision-report, deleted replan report |
| Slice layering table | Once in map.md | Duplicate in g-decision-report |
| Weak-form spec drift note | — | Entire paragraph (slice 1 criteria already explicit in spec body) |
| Verified knowns 1–8 | All retained | Minor compression in knowns 5–8 only |
| Open forks (ack shape, partial batch, optimistic rebuild) | One bullet list in map | Repeated in g-decision-report body |

## Unchanged

- [[spec.md]] user stories and testing scenarios (slice 1).
- [[project.md]] summary (no [[index.md]] regen).
- Evidence docs: [[child-occurrence-uniqueness.md]], [[replace-span-cas-feasibility.md]].
