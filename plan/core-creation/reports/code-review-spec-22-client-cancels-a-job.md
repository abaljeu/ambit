# Spec review — 22 Client cancels a job

Range: `git diff origin/staging...HEAD` (tip `de0345fe`, base `c4cbfd46`). Spec: [22 — Client cancels a job](plan/core-creation/issues/22-client-cancels-a-job.md) (What to build + Context Sequence + Non-goals). Context only: [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) Poll conveyance; [plan/llm-connector/arch.md](plan/llm-connector/arch.md) Story path Cancel by Focus. This axis does not score Standards. Ticket Status stays `coded`.

## 1. Missing or partial

None. Live-row Cancel is shown only with [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) `amb-actor-live` chrome. Browser POST sends Focus NodeId through the existing `CoreMailbox.cancelByFocus` door. Cancel POST acknowledges without Events; chrome off and Cancelled (or Error) result stay on 21 Poll. Non-goals hold: no Undo, no CoreMailbox cancel change, no DLL catalog, no `applyEvent` projection work.

## 2. Scope creep

1. **Palette and key Cancel** — Spec: “While a Focus is live (chrome from [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md)), the Browser offers cancel for that Focus.” [RowView.fs](src/Client/RowView.fs) Cancel on the live row meets that. The diff also adds `CommandId.Cancel`, palette filter `cancelAvailable`, and `Ctrl+Shift+Enter` in [CommandEntry.fs](src/Shared/CommandEntry.fs) and [Commands.fs](src/Client/Commands.fs). The spec does not name a palette item or a key.

Ticket Status, Time, [project.md](plan/core-creation/project.md) Notes, and arch checkbox edits are not product behaviour. Unchecked arch seam **Browser cancel ↔ Core Cancelled** is not a must-fix when What to build is met.

## 3. Looks implemented but wrong

None. Row click and palette/key both go through `ActorLive.cancelEffect` and POST `{ focusId }`. Success callback is empty, so `ActorStop` Cancelled, chrome off, and the result wait for 21 Poll. HTTP Adapter `/ambit/cancel` only exposes the existing Core door; it does not add Core cancel semantics.

## 4. Summary

(a) 0, (b) 1, (c) 0. Worst in Spec: palette and `Ctrl+Shift+Enter` Cancel are extra surfaces beyond live-chrome cancel control.
