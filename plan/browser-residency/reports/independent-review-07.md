# Independent review — 07 Expand Want

Range: `origin/staging...HEAD` (`90aa79b1`). Ticket [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md). Locked wire: request field `want` as a `NodeId` list, always sent, empty `[]`; answer `nodes` plus `childMap` on `ChangeSuccessResponse`; `ApiVersion.current` is 13; Load `packages` remain; production doors stay on [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md) through [11 — Migrate Bullet, Included, and bootstrap wants](plan/browser-residency/issues/11-migrate-bullet-included-and-bootstrap-wants.md).

## 1. Verdict

**Needs work.** The Want compose seam, `installWantAnswer`, and additive wire match the locked grill and the expand-only cut. Two [arch.md](plan/browser-residency/arch.md) checkmarks claim work that is not in this diff. Uncheck those before treating [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md) as review-clean. Do not squash-land.

## 2. Must-fix

1. **Uncheck Shared segment 6 or add Included.** [arch.md](plan/browser-residency/arch.md) Shared segments item 6 is `[x] Visible-closure bootstrap (reserved Children + Zoom ancestors + Included)`. [map.md](plan/browser-residency/map.md) Destination names that same set. `ResidentProjection.visibleClosureGraph` loads reserved parents, owner ancestors, Zoom, and those Nodes' direct Children only. It does not walk Included. Story path 2.1 is still `[ ]`, which is honest. Segment 6 is not. Module map ResidentProjection 2.1 is also `[x]` after the sentence dropped Included. Fix: leave the helper as reserved-plus-ancestors for this expand, uncheck segment 6, and keep Included on [02 — Lock bootstrap visible-closure set](plan/browser-residency/issues/02-lock-bootstrap-visible-closure.md) / [11 — Migrate Bullet, Included, and bootstrap wants](plan/browser-residency/issues/11-migrate-bullet-included-and-bootstrap-wants.md). Or add Included parents to `loadedParentIds` if this ticket must own the full set.
2. **Uncheck story 21.2 SyncLogic half.** [arch.md](plan/browser-residency/arch.md) story 21.2 is `[x] SyncLogic / ResidentProjection installs those edges`. `ResidentProjection.installWantAnswer` does. `SyncLogic.applySyncResponse` still calls `installPackages` only. [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md) owns SyncLogic install after the Event tail.

## 3. Nice-to-have

1. **Move `ownerAncestorIds` onto GraphQuery.** [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) says check `GraphQuery` before a new owner-chain walk. `enclosing` already walks `ownerParentByChild`. Collect-ancestors belongs there.
2. **Build `visibleClosureGraph` through `installWantAnswer`.** The helper rebuilds a node Map and calls `Graph.fromNodes`. The Shared test already installs `visibleClosureWantAnswer` through `installWantAnswer`.
3. **Pin the current bootstrap subset with a test.** Add a case that a deep Included descendant under Zoom is not Resident yet. That stops [11 — Migrate Bullet, Included, and bootstrap wants](plan/browser-residency/issues/11-migrate-bullet-included-and-bootstrap-wants.md) from treating this helper as the full Destination set.
4. **Keep `EventBatch` as the production post-Event body until a coordinated cut.** `ChangeRequest.want` is required. [src/Server/Api.fs](src/Server/Api.fs) `postEvents` still decodes `EventBatch`. That is correct for expand. [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md) and [10 — Migrate Browser poll, post-Event, and boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md) need a dual-run decode or a single cut so old `{ events }` bodies still apply.

## 4. Tests

Reviewed this run (not the author's claim):

1. `dotnet test tests/Shared.Tests --filter FullyQualifiedName~WantTests` — 11 passed, including compose-then-install, Fold, dangling refuse, absent key vs `[]`, and `installPackages` beside `installWantAnswer`.
2. `dotnet test tests/Shared.Tests --filter FullyQualifiedName~SerializationTests` — 47 passed, including always-send `want`, Want-answer `nodes`/`childMap` round-trip, omit-fields decode, and Load `packages` still decode.
3. `dotnet test tests/Shared.Tests --filter FullyQualifiedName~SyncLogicTests|BootCachePollTests|LargeChangeApplyTests` — 56 passed after the empty `nodes`/`childMap` plumbing.
4. `./scripts/client.sh build` — Fable Client compile gate succeeded.

Want.compose two-tier is sequential: first Unloaded Included, then those Children after install plus SiteMap expand. That is the only shape that works when Child ids are not Resident yet. No throttle. One compose function; no second App Want.

## 5. Standards

Hard documented-standard violations: none. Mechanical scan (`python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`) reported no over-limit bindings. Axis report: [standards-review-07.md](standards-review-07.md).

1. Feature Envy (judgement) — `ownerAncestorIds` walks `graph.ownerParentByChild` instead of GraphQuery. See Nice-to-have 1.
2. Duplicated Code (judgement) — `visibleClosureGraph` repeats the `installWantAnswer` merge. See Nice-to-have 2.

## 6. Spec

Axis report: [spec-review-07.md](spec-review-07.md).

1. Visible-closure omits Included versus [map.md](plan/browser-residency/map.md) Destination and [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md) item 2.4. See Must-fix 1.
2. No scope creep. Production [App.fs](src/Client/App.fs) does not attach Want. [Api.fs](src/Server/Api.fs) only fills empty `nodes` / `childMap` so `ChangeSuccessResponse` compiles. `LoadResponse.packages` remain. `ApiVersion.current` is 13.
3. Dishonest Shared segment 6 and story 21.2. See Must-fix 1 and Must-fix 2.

[01 — Lock Sync want + edges/Nodes package shape](plan/browser-residency/issues/01-lock-sync-want-package-shape.md), [02 — Lock bootstrap visible-closure set](plan/browser-residency/issues/02-lock-bootstrap-visible-closure.md), [03 — Lock ongoing want priority and when wants are attached](plan/browser-residency/issues/03-lock-ongoing-want-priority.md), and [06 — Dual-run vs migrate explicit Load Fetch](plan/browser-residency/issues/06-dual-run-vs-migrate-explicit-load.md) stay `needs-info`. This expand follows the locked grill on the ticket, not a closed grill write-up.

## 7. Summary

Standards: 2 judgement findings (worst: custom owner-chain walk). Spec: 3 findings (worst: Shared segment 6 claims Included). Ticket Status stays `coded`.

## 8. Honesty fix

2026-09-26: Alan accepted reserved-plus-ancestors for this expand (do not add Included on [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md)). [arch.md](plan/browser-residency/arch.md) Shared segment 6 is unchecked. Story 2.1 stays unchecked. Story 21.2 names `installWantAnswer` only; new 21.4 leaves SyncLogic Want-answer install to [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md). Module map and seam 3 say reserved-plus-ancestors, not Included. Ticket 07 Status stays `coded`. Must-fix honesty items are addressed. Nice-to-haves stay open.
