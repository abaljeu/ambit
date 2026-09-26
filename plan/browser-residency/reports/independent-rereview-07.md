# Independent re-review — 07 Expand Want

Range: `origin/staging...HEAD` (`7f3875fc`). Ticket [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md). Prior write-up: [independent review — 07 Expand Want](independent-review-07.md). This report is not approval. Status stays `coded`. Do not squash-land.

Locked wire: request field `want` as a `NodeId` list, always sent, empty `[]`; answer `nodes` plus `childMap` on `ChangeSuccessResponse`; `ApiVersion.current` is 13; Load `packages` remain; production doors stay on [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md) through [11 — Migrate Bullet, Included, and bootstrap wants](plan/browser-residency/issues/11-migrate-bullet-included-and-bootstrap-wants.md).

## 1. Verdict

**Good.** Alan can accept. The two prior must-fixes are honest on this tip. The code still matches the locked expand cut. No remaining must-fix. Ticket Status stays `coded`.

## 2. Prior must-fixes

1. **Shared segment 6.** [arch.md](plan/browser-residency/arch.md) Shared segments item 6 is `[ ] Visible-closure bootstrap (reserved Children + Zoom ancestors + Included)`. Story 2.1 stays `[ ]`. Module map [3. ResidentProjection](plan/browser-residency/arch.md) interface 1 and seam 3 say reserved-plus-ancestors, not Included. `loadedParentIds` is reserved parents, owner ancestors, and Zoom. That is the expand helper Alan accepted. Included stays [02 — Lock bootstrap visible-closure set](plan/browser-residency/issues/02-lock-bootstrap-visible-closure.md) / [11 — Migrate Bullet, Included, and bootstrap wants](plan/browser-residency/issues/11-migrate-bullet-included-and-bootstrap-wants.md).
2. **Story 21.2 SyncLogic half.** Story 21.2 is `[x] ResidentProjection.installWantAnswer installs those edges`. New 21.4 is `[ ] SyncLogic.applySyncResponse installs Want-answer edges after Events` and names [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md). `applySyncResponse` still calls `installPackages` only.

## 3. Locked expand cut

1. `Want.compose` lists Included that miss Children. The next compose after install plus SiteMap expand lists those Children. No reserved or Zoom third tier. No throttle. One compose function.
2. `installWantAnswer` installs edges and Nodes separately and refuses a dangling edge. Absent key stays Unloaded. `[]` is a Loaded leaf.
3. Additive `want` on `SyncWant` / `PollRequest` / `ChangeRequest`. Additive `nodes` and `childMap` on `ChangeSuccessResponse`.
4. `ApiVersion.current` is 13.
5. `LoadResponse.packages` and `packageChildMap` remain. `installPackages` still works.
6. Production doors are not switched. [App.fs](src/Client/App.fs) does not call `Want.compose`. [UpdateCodec.fs](src/Client/UpdateCodec.fs) still encodes `EventBatch`. [Api.fs](src/Server/Api.fs) `postEvents` still decodes `EventBatch` and fills empty `nodes` / `childMap`. `getPoll` does the same. `getState` still uses `bootstrapGraph` (old ROOT path).

## 4. Spec-axis Zoom note (not a must-fix)

Axis report [spec-rereview-07.md](spec-rereview-07.md) says adding Zoom to `loadedParentIds` copies Zoom `childMap`, so Zoom's direct Children become Resident, and that that set is Included. The prior review already named this shape: reserved parents plus owner ancestors plus Zoom; first-rank Children of those parents are Resident; deeper Included is not. That is why Shared segment 6 stays unchecked. This re-review does not reopen it as a must-fix.

## 5. Tests

Reviewed this run (not the author's claim):

1. `dotnet test tests/Shared.Tests --filter FullyQualifiedName~WantTests` — 11 passed.
2. `dotnet test tests/Shared.Tests --filter FullyQualifiedName~SerializationTests` — 47 passed.
3. `dotnet test tests/Shared.Tests --filter FullyQualifiedName~SyncLogicTests|BootCachePollTests|LargeChangeApplyTests` — 56 passed.
4. `./scripts/client.sh build` — Fable Client compile gate and bundle succeeded.

## 6. Standards

Axis report: [standards-rereview-07.md](standards-rereview-07.md). Mechanical scan (`python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`) printed no over-limit F# bindings.

1. Bare number (hard) — prior review notes [independent-review-07.md](independent-review-07.md) and [spec-review-07.md](spec-review-07.md) write "item 2.4" / "Ticket 07" without the name. Those reports stay write-once. Not a product must-fix.
2. Feature Envy (judgement) — `ownerAncestorIds` walks `graph.ownerParentByChild` instead of GraphQuery. Same nice-to-have as the first review.
3. Duplicated Code (judgement) — `visibleClosureGraph` repeats the `installWantAnswer` merge. Same nice-to-have as the first review.

## 7. Spec

Axis report: [spec-rereview-07.md](spec-rereview-07.md).

1. Zoom-as-parent versus the reserved-plus-ancestors sentence. See section 4. Not a remaining must-fix.
2. No scope creep. Production doors stay on the old path. Load `packages` remain. `ApiVersion.current` is 13.
3. Prior dishonest checkmarks are fixed. Shared segment 6 unchecked. Story 21.2 names `installWantAnswer` only. Story 21.4 leaves SyncLogic to [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md).

## 8. Summary

Standards: 3 findings (worst: bare numbers in prior review notes). Spec: 1 finding (worst: Zoom-as-parent wording). Remaining must-fix: none. Ticket Status stays `coded`.
