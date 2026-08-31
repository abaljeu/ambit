# Reform build-upon execution report

Date: 2026-08-23
Branch: `w/relaxed-concurrency` (cut from `selective-client-sync`)

## Summary

Reformed relaxed-concurrency as a **build-upon layer** on event-sourced-ops. Removed pre-ESO implementation content (slice specs, merge protocols, gate-removal plans). Synced ESO cross-links and WORK board. Regenerated project index.

## Files changed

### relaxed-concurrency

| File | Change |
| --- | --- |
| [[project.md]] | Stage `done`; build-upon summary; ESO links |
| [[map.md]] | Full rewrite: Role, knowns 1–8 (known 3 past tense), shared rejections, ESO foundation pointer, open D–F + resolved A/B/C/G one-liners; removed cheap-win, client merge-sync, slice tables, related-later-work |
| [[spec.md]] | Replaced with ESO handoff stub |
| [[design.md]] | Replaced with handoff stub + no-server-weak-form rationale |
| [[git.md]] | Created — project branch record |
| [[replace-span-cas-feasibility.md]] | Staleness note; ViewModelJoinOps row → Faithful; ranked fixes updated |

### event-sourced-ops (connecting references only)

| File | Change |
| --- | --- |
| [[../event-sourced-ops/project.md]] | One line: rc map as build-upon sibling |
| [[../event-sourced-ops/overview.md]] | One sentence: siblings build upon this foundation |
| [[../event-sourced-ops/details/relation-to-relaxed-concurrency.md]] | rc aligned as build-upon layer; slice 1 delivered note |
| [[../event-sourced-ops/details/as-implemented-facts.md]] | Gate removed; wikilinks to rc audits/map |
| [[../event-sourced-ops/details/open-questions.md]] | Removed stale slice 1 pending; frontier D–F pointer |

### Repo-wide

| File | Change |
| --- | --- |
| [[../index.md]] | Regenerated — relaxed-concurrency stage `done` |
| [[../../WORK.md]] | Removed blocked slices 2–3 entry; removed ViewModelJoinOps pending entry (verified fixed in code) |

## Unchanged (verified accurate)

- [[child-occurrence-uniqueness.md]] — no edits needed; still accurate evidence for rejecting strong id-anchored Replace.

## Code verification

- `ViewModelJoinOps.removeCurrentChildOp` reads live `g.nodes.[parentId].children` and calls `ChildListWire.removeRange` — join-on-Ref fabrication gap is fixed.
- Global revision gate absent from `FileAgent.applyBatch` — consistent with ESO issue 02 delivery.

## Items for user review

1. **Frontier D–F** — still open in [[map.md]] with no owner; re-open relaxed-concurrency stage only if one gets an implementation track.
2. **Join-on-Ref test coverage** — audit notes fixed producer but no dedicated test yet (optional add).
3. **ESO relation doc tensions section** — left intact per plan; only light factual updates (slice 1 delivered, rc as build-upon).
4. **No commit** — per instructions; all changes are dirty on `w/relaxed-concurrency`.
