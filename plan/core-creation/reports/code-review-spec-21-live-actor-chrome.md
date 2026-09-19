# Spec review — 21 Client shows live Actor

Range: `git diff 2f34407ebc14a3377d6fb7d7f555d062b26aaa6b...HEAD` (commit `df9e281f`). Spec: [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) (What to build + Context + Non-goals). This axis does not score Standards.

## 1. Missing or partial

None on the Poll / Load Event-apply path the spec names. `foldProjectedEvents` runs `applyActorLive` for `EventBody.ActorStart` / `EventBody.ActorStop`. Poll, Load, boot, and catch-up copy `liveFocusIds` onto the VM. Row class `actor-live` plus CSS left bar is the Focus-row indicator. Non-goals hold: no Graph `lockPresent` field, no span lock, no History UI, no Cancel UI, no extra live-registry Poll.

Command POST 200 still does not apply Events ([runSubmitCommand](src/Client/App.fs) only logs). Context says Poll carries them, so that wait is in spec.

## 2. Scope creep

1. **`aria-busy` on the live row** — Spec: “While a Focus is live, the Browser shows a clear active-Actor indicator on that Focus (row / outline).” [RowView.fs](src/Client/RowView.fs) also sets `aria-busy="true"` when the class is `actor-live`, and removes it when the class leaves. The spec asks for visible row / outline chrome, not this attribute.

Ticket Status, Time, and [project.md](plan/core-creation/project.md) Notes are not product behaviour.

## 3. Looks implemented but wrong

None on the Poll / Load path. `applyServerTail` adds the Focus on ActorStart and removes it on ActorStop. `planPatchDOM` adds and drops `actor-live`. The selection fast path compares `liveFocusIds`, so the class can clear when the Focus leaves the live set. Change POST `applySubmitResponse` does not copy `liveFocusIds`; `reconcileAck` does not run `applyActorLive`. That matches Graph Change confirmation (suffix ops), not a broken Poll projection.

## 4. Summary

(a) 0, (b) 1, (c) 0. Worst in Spec: `aria-busy` is extra vs the asked row / outline chrome.
