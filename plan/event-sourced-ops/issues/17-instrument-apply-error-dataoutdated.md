# 17 — Instrument apply-error → DataOutdated with op type and mismatch reason

**Status:** defined
**Type:** bug-fixing
**Blocked by:** None — can start immediately

## Context

When Poll apply fails (`foldProjectedEvents`, `Op.apply`, or `installWantAnswer` returns Error), the Browser sets DataOutdated and shows the reload overlay. Those Error arms discard the message (`| Error _ ->`) and do not `consoleLog`. Success paths already log `PollDone autoSync applied=` and `PollDone catchUp applied=`. Apply failures stay silent, so a person cannot tell a CAS mismatch from a dangling edge or another gate.

This ticket is diagnostics only. The consume fix path is [16 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload](16-fix-pending-merged-events-undo-then-apply.md) / GitHub [146 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload](https://github.com/abaljeu/ambit/issues/146). Do not implement that fix here. Do not change when DataOutdated is set, except to store a reason for log and optional overlay.

## What to build

When Poll apply fails and the Browser enters DataOutdated, the console (and preferably a short overlay reason) names the failed Op type, the mismatch reason, and the Node id when those are known.

### 1. Log PollDone Error arms

1. [ ] Catch-up Error log — [Update.fs](src/Client/Update.fs) PollDone catch-up Error arms (~341–350) bind the Error string and `consoleLog` a stable marker such as `[Gambol sync] PollDone apply failed …`.
2. [ ] None-outcome Error log — same for the none-outcome apply Error arm (~391–393).
3. [ ] DataOutdated apply Error log — same for the DataOutdated apply Error arm (~413–415).
4. [ ] Shared silence (optional) — LoadDone / Submit Error arms that share the same discard may log the same way if that is easy.

### 2. Name op, reason, and Node

1. [ ] Op type — include the failed Op type when known (`SetText`, `SetClasses`, `SetName`, `Replace`, `NewNode`, `NewSpecialNode`, `SetDocumentState`, `SetUpdateTime`) or `installWantAnswer` / Want for a dangling edge.
2. [ ] Mismatch reason — include the mismatch reason string (for example `old text does not match`, `old span does not match`, `node not found`, `dangling edge`).
3. [ ] Node id — include the Node id when available, and the parent id for `Replace`.
4. [ ] Richer Error if needed — `applySyncResponse` today returns `Result<_, string>`. Thread a richer Error from `foldProjectedEvents` / `ResidentProjection.applyOps` / `Op.apply`, or log at the first Invalid site before collapse, so op type and Node id can be named. Do not invent op identity when only a bare string exists.

### 3. Overlay reason (preferred)

1. [ ] Short overlay reason — [Overlays.fs](src/Client/Overlays.fs) DataOutdated copy is generic (“View is out of date” / “Reload before continuing.”). Prefer a short reason when one was captured on the model. Do not dump full internals.

### 4. Non-goals

1. No consume fix — do not implement undo-then-apply from [16 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload](16-fix-pending-merged-events-undo-then-apply.md).
2. No retry policy — do not add State re-fetch retry, and do not change when DataOutdated is set except to store a reason.

## See also

[148 — Instrument apply-error → DataOutdated with op type and mismatch reason](https://github.com/abaljeu/ambit/issues/148), [146 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload](https://github.com/abaljeu/ambit/issues/146), [16 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload](16-fix-pending-merged-events-undo-then-apply.md), [PollDone Error arms](src/Client/Update.fs), [applySyncResponse](src/Shared/SyncLogic.fs), [Overlays.fs](src/Client/Overlays.fs)

## Comments

- 2026-09-27 — Filed to mirror GitHub [148 — Instrument apply-error → DataOutdated with op type and mismatch reason](https://github.com/abaljeu/ambit/issues/148). Status `defined`. Diagnostics only; related fix path is [16 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload](16-fix-pending-merged-events-undo-then-apply.md).
