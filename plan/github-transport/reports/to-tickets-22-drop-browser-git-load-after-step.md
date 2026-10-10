# to-tickets 22 — Drop the Browser git Load after-step

## 1. Ticket

[22 — Drop the Browser git Load after-step](../issues/22-drop-browser-git-load-after-step.md)

Sequence on [github-transport architecture](../arch.md) is module-build. This run ticketed only that one existing ticket. The capability is the App module item that drops the Browser git Load after-step.

## 2. What was wrong

1. **Code inventory** — The old writeup named client files, line numbers, and the functions that delete the after-step.
2. **Binding arch list** — The old writeup carried a Binding arch list. Publish puts each source link in See also once.
3. **Wrong shape** — The old writeup had a trial step and a current-state section. It had no Trackers line, no Finish-up, and no See also.
4. **Wrong kind** — The old writeup was a deletion procedure. Sequence module-build writes one capability: after git Load returns, the Browser does not run a second reconcile or parse, and the App stays thin.

## 3. Blocking edge

**Blocked by:** 21 — Git Load posts the Workspace

That edge is unchanged. Status stays `defined`. Type stays `coding`.

## 4. Obligations

The obligations piece found no test, doc, glossary entry, or Committed Decision this ticket must update. The checklist under What to build is empty. Finish-up stays.

## 5. Trackers

**Trackers:** architecture, project spec

## 6. Frontier

No defined ticket in this Project is unblocked.

1. **22 — Drop the Browser git Load after-step** — Status `defined`. Blocked by 21 — Git Load posts the Workspace. That blocker is `coded`, not `done`. This ticket is not the frontier.
2. **24 — Load and Save on the events list** — Status `defined`. Blocked by 22 — Drop the Browser git Load after-step.
3. **25 — Pace server functions** — Status `defined`. Blocked by 24 — Load and Save on the events list.
4. **27 — Field test the Load trial sequence** — Status `defined`. Blocked by 25 — Pace server functions.

The next defined ticket in the chain is 22 — Drop the Browser git Load after-step. It waits until 21 — Git Load posts the Workspace is `done`.

## 7. Stage

[github-transport](../project.md) Stage was already `build`. This correction did not change Stage and did not change Updated. Stage was not set back to `slice`.
