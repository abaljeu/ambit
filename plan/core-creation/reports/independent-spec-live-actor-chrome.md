# Independent spec review — 21 live Actor chrome

Range: `origin/staging...HEAD` at `65bdbc40`. Spec: [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md). Implementer review files in the range are ignored.

## (a) Missing or partial

1. Command response Events — Spec: "Client tracks live Focus ids from EventBody.ActorStart / EventBody.ActorStop applied through normal Poll / response Event apply (same path as Graph Changes)." Poll, Load, boot novel tails, and catch-up use [SyncLogic.fs](src/Shared/SyncLogic.fs) `foldProjectedEvents` (same fold as Change Events). [App.fs](src/Client/App.fs) `runSubmitCommand` logs the command POST and does not apply the Event list that [Api.fs](src/Server/Api.fs) `postCommand` already returns. Chrome waits for a later Poll.
2. `/state` boot — Spec: "A person needs to see that an Actor is live for a Focus." and "After ActorStart for a Focus, that Focus is live." [Update.fs](src/Client/Update.fs) `StateLoaded` sets `actorLiveFocusIds` to empty and does not apply EventLog. After `/state` at the tip, Poll does not resend a prior ActorStart. A live Actor has no chrome until a new ActorStart. Spec non-goals forbid a Graph lock-present field and a live registry Poll, so this path does not meet the person-facing need.

## (b) Scope creep

None in product behavior. Projection is a field on `ClientSyncState` beside Graph apply. Chrome is the `actor-live` row class plus CSS. Tests cover ActorStart / ActorStop and the class patch. No Cancel UI. No new Graph lock-present field. No span lock. No History UI. No extra Poll registry.

## (c) Implemented but wrong

None. `applyActorLive` matches `EventBody.ActorStop(focusId, _)`, so ActorFailed and ActorCancelled also remove the Focus. Chrome does not read `lockPresent`. The indicator is a row inset `box-shadow` (spec: "row / outline"). [ViewModelDomPlan.fs](src/Shared/ViewModelDomPlan.fs) treats a live-set change as a class patch, not a selection-only fast path.
