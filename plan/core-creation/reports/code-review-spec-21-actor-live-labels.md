# Spec review — Actor live labels

Range: `git diff origin/staging...HEAD` (HEAD `d280520aeea34870442613e05606851b11e349d4`). Specs: [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) Sequence + Start/Stop; [core-creation architecture](plan/core-creation/arch.md) **Actor live labels**; [Plan or doc change — Actor live labels](plan-or-doc-change-21-actor-live-labels.md). [50 — Actor live result labels from Command node](plan/core-creation/issues/50-actor-live-labels-from-command.md) is historical; architecture and 21 — Client shows live Actor win. This axis does not score Standards.

## 1. Missing or partial

None on this rework. Architecture **Runnable node**: “from Focus, scan owner-parents toward zoom root (inclusive of both); first node whose text starts with literal `?` or contains `=`. Do not skip an `=` node to reach a `?` above.” [ActorLive.fs](src/Shared/ActorLive.fs) `runnableTextOnPath` walks the owner chain with `GraphQuery.enclosing`, stops at the first `?` or `=` or at zoom, and does not skip an `=` node. **Display label**: “`?` form → TitleCase of the actor name token (same token rule as CommandRequest actor select). `=` form → TitleCase of that line’s name token if present, else generic. No runnable on the path → generic (not a product name).” The code uses `CommandRequest.actorNameFromText` then first-letter TitleCase; `count=1+2` → Count; `=1+2` → generic. Uses: “ActorStart / ActorStop Focus (and ActorStart zoomId when present); Client zoom root on stop.” Start uses `start.focusId` and `start.zoomId`; stop uses the stop Focus and [UpdateActorLive.fs](src/Client/UpdateActorLive.fs) `model.zoomRoot`. 21 — Client shows live Actor Start: “Client `lastCmdResult` … **“Run: <Label> started.”**” lands as `Detail (Some "Run", $"{label} started.")`. Stop: “same Label rule … success detail if useful; on `ActorFailed` / `ActorCancelled`, an Error … Conveyance only.” Succeeded/Failed/Cancelled still set Detail/Error with that Label as chip; provider text is unchanged. Projection, chrome, and boot are not in this diff.

## 2. Scope creep

None in product code. The `=` runnable bound and TitleCase helpers are architecture **Runnable node** and **Display label**. Filing [50 — Actor live result labels from Command node](plan/core-creation/issues/50-actor-live-labels-from-command.md) then cancelling it is not product behaviour.

## 3. Looks implemented but wrong

None against architecture and 21 — Client shows live Actor. `?test` → Test. `?ai` → Ai, which is TitleCase of the CommandRequest token `ai`, not a fixed product chip (**Command chip**: “not a fixed product name”). Tests cover “equals ancestor stops scan and does not use `?` above.”

## 4. Summary

(a) 0, (b) 0, (c) 0. Worst in Spec: none.
