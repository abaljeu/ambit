# Core creation

Stage: build
Summary: Establish Core and Core API as the sole Server Graph writer, persistent-state coordinator, and Actor pool.
Updated: 2026-09-21
Started: 2026-09-05
Actual: 83h50m

## Notes
- 2026-09-21 — Implemented [53 — Cancel HTTP conveys ActorStop](issues/53-cancel-http-conveys-actorstop.md): Cancel HTTP success carries Cancelled `ActorStop` Events; Client applies them so `amb-actor-live` clears without waiting on Poll. Status `coded`.
- 2026-09-21 — Filed [53 — Cancel HTTP conveys ActorStop](issues/53-cancel-http-conveys-actorstop.md): Cancel HTTP success carries Cancelled `ActorStop` Events; Client applies them so `amb-actor-live` clears without waiting on Poll. Status `defined`.
- 2026-09-20 — Landed [52 — Run must not launch when edit commit fails](issues/52-run-abort-when-commit-fails.md) on staging (Alan accept). Status `done`.

- 2026-09-20 — Alan Standards finding Divergent Change (edit commit belongs in execRunOp) for [52 — Run must not launch when edit commit fails](issues/52-run-abort-when-commit-fails.md): Editing commit in Client RunLaunch; CommandRequest ActorStart factory. Status stays `coded`.
- 2026-09-20 — Re-review Spec for [52 — Run must not launch when edit commit fails](issues/52-run-abort-when-commit-fails.md): lastCmdResult identity abort; Shared RunEditCommit gate; same-Error proof. Status stays `coded`.
- 2026-09-20 — Review findings for [52 — Run must not launch when edit commit fails](issues/52-run-abort-when-commit-fails.md): Shared `execRunOp` proof; UpdateHelpers and CoreMailboxBackend length restored. Status stays `coded`.
- 2026-09-20 — Independent review of [52 — Run must not launch when edit commit fails](issues/52-run-abort-when-commit-fails.md): Standards Needs changes; Spec Needs changes. Status stays `coded`. Report: [code-review-52-run-abort-when-commit-fails](reports/code-review-52-run-abort-when-commit-fails.md).
- 2026-09-20 — Implemented [52 — Run must not launch when edit commit fails](issues/52-run-abort-when-commit-fails.md): no SubmitCommand/Amble after failed edit commit; drop live after ActorStart persist Error. Status `coded`.
- 2026-09-20 — Filed [52 — Run must not launch when edit commit fails](issues/52-run-abort-when-commit-fails.md): Run must not launch after failed edit commit; no orphan live without chrome. Status `defined`.
- 2026-09-20 — Landed [[issues/51-browser-run-focus-vs-command.md|51]] and [[issues/21-client-shows-lock-present.md|21]] live-label rework on staging (Alan accept). Status `done`.
- [52 — Run must not launch when edit commit fails](plan/core-creation/issues/52-run-abort-when-commit-fails.md) — No SubmitCommand after failed commit; no orphan live. Status `done`.
- 2026-09-20 — [51 — Browser Run: Focus reply parent, Command is runnable ancestor](issues/51-browser-run-focus-vs-command.md) Status `coded`. `?` ActorStart; `=` Amble; scan-stop unchanged.
- 2026-09-20 — [[issues/21-client-shows-lock-present.md|21]] failed review (hardcoded AI on `?test`); Status `coded`. [[issues/50-actor-live-labels-from-command.md|50]] cancelled (folded into 21).
- 2026-09-20 — Filed [[issues/50-actor-live-labels-from-command.md|50 Actor live result labels from Command node]]: done as Wayfinder task; coding on 21 / PR #80.
- 2026-09-20 — Filed [[issues/51-browser-run-focus-vs-command.md|51 Browser Run Focus vs Command]]: Focus = reply parent; Command = runnable ancestor (`?` or contains `=`); one-Node stays hello-only. Status `defined`.
- 2026-09-19 — Landed [[issues/22-client-cancels-a-job.md|22 Client cancels a job]] on staging (Good). Status `done`.
- 2026-09-19 — Landed [[issues/21-client-shows-lock-present.md|21 Client shows live Actor]] on staging (Good). Status `done`.
- 2026-09-19 — Chrome tickets expanded: [[plan/core-creation/issues/21-client-shows-lock-present.md|21 Client shows live Actor]] owns start result (“Run: AI started.”), Command/Poll Event apply, chrome, boot live set, Poll stop/error conveyance; [[plan/core-creation/issues/22-client-cancels-a-job.md|22 Client cancels a job]] owns cancel control on live chrome. DLL provider error naming is a separate ticket.
- 2026-09-19 — Status catch-up: [[issues/16-track-running-job.md|16]] and [[issues/17-cancel-a-job.md|17]] → `done` (delivered under Event lifecycle / llm-connector 10 + Client 22). [[issues/18-finish-and-drop.md|18]] stays `defined` (Interrupted restart unchecked).

## Map

- [[plan/core-creation/map.md]] — chart the initial Graph-agent package and later Core decisions.
- [[plan/core-creation/arch.md]] — hello / one-mailbox Actor program module map, plus Event destination. Stories **Event, EventLog, and ClientHistory** and **Caller, persist, and Poll** are expand-migrate-contract.

## Committed Decisions

- [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]] — provisional framing of the Core structure.
- [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]] — cancel/finish are fast queue messages; slow work is an Actor.

## Agent instruction

This increment: Core owns the authoritative Graph, Authority validation, and the Actor pool. The Adapter owns HTTP JSON and Browser transport. Persist algorithms and Parse algorithms stay outside Core. Parse retains its typed Graph-only operation; Actor output uses normal Core Change. Actor definitions stay outside Core.

- Callers hold the Core object ([[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]). Browser HTTP is an adapter. Callers do not unpack [[src/Server/Core/CoreRuntime.fs]] into a flattened HTTP context.
- Dispatch is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]. Boundaries, launch membership, Event sequence, universal response, and mailbox lifecycle are [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]].
- Files stay [[plan/core-creation/issues/07-define-core-files-contract.md]]. General Query stays [[plan/core-creation/issues/08-define-core-query-contract.md]]. Live Actor query stays [[plan/core-creation/issues/16-track-running-job.md]].
- Active Actor chrome is [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) then [22 — Client cancels a job](plan/core-creation/issues/22-client-cancels-a-job.md) (Browser lifecycle UI). Earlier note that parked 21 on event-sourced-ops is superseded.
- Locked code plan: [[plan/core-creation/mitigations.md]].

## Implementation plan

- [[plan/core-creation/issues/Implementation Planning and Record.md]]

## Issues

- [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] — establish the shared Core Changes path.
- [[plan/core-creation/issues/02-core-actor-pool.md]] — establish Core-owned Actor pool machinery. Status `ready-for-agent` after rewind named the mailbox/TaskPool shape.
- [[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]] — use Database persistence when available and reject Changes when unavailable. Status `done`.
- [[plan/core-creation/issues/14-server-tracks-credentials.md]] — public/secret Authority admission and durable readable Authority; Status `blocked` by the Actor pool baseline.
- [[plan/core-creation/issues/15-launch-actor-and-hold-span.md]] — historical span delivery superseded by typed launch and ActorStarted; Status `blocked`.
- [[plan/core-creation/issues/16-track-running-job.md]] — live public-identity query plus durable lifecycle Events; Status `done`.
- [[plan/core-creation/issues/17-cancel-a-job.md]] — terminal Cancelled by Focus NodeId without Undo; Status `done`.
- [[plan/core-creation/issues/18-finish-and-drop.md]] — durable ActorFinished, synchronous registry removal, non-blocking termination, and Interrupted restart reconciliation; Status `blocked`.
- [[plan/core-creation/issues/19-database-down-and-host-stop.md]] — Database-down state and mutating Post/launch probe; blocked by [[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]].
- [[plan/core-creation/issues/20-client-presents-credential.md]] — live Browser presents a credential on every message.
- [[plan/core-creation/issues/21-client-shows-lock-present.md]] — historical name; any Browser running indicator projects lifecycle Events.
- [[plan/core-creation/issues/22-client-cancels-a-job.md]] — user cancels a job from the UI.
- [[plan/core-creation/issues/23-close-core-object-seam.md]] — production posts present Credential; callers use the typed Core object.
- [[plan/core-creation/issues/24-clarify-core-increment-boundary.md]] — agent instruction: Core vs Adapter vs Client; no lock UI this increment.
- [[plan/core-creation/issues/25-bind-changes-at-core-seam.md]] — leftover after 23: HTTP posts through bound Changes, not unpacked CoreAuth. Status `done`.
- [[plan/core-creation/issues/26-failed-actor-stop-still-drops.md]] — failed Actor stop must still enqueue delete-actor. Status `cancelled` (rewind/redo, not a wrap patch).
- [[plan/core-creation/issues/27-prove-core-actor-lifecycle-with-testactor.md]] — prove the public Core lifecycle and universal response without an Agent transport.
- [[plan/core-creation/issues/28-drain-actor-lifecycle-on-host-stop.md]] — later host-stop terminal drain and restart reconciliation, separate from Database availability.
- [[plan/core-creation/issues/30-reshape-coreactorpool-synchronized-table.md]] — Point 0 preamble (done): strip CoreActorPool mailbox-queue design; synchronized table + thread pool.
- [[plan/core-creation/issues/31-one-coremsg-loop-parameterized-persist.md]] — Point 0 (done): one CoreMsg loop, parameterized persist.
- [[plan/core-creation/issues/32-move-persist-agents-under-coremailbox.md]] — Point 0 (done): persist agents under Core; generic CoreMailbox door.
- [[plan/core-creation/issues/29-prove-testactor-hello.md]] — current cut: section 1 mailbox foundation on `dev`; remaining hello sections open. Redo: land [[plan/core-creation/arch.md]] before further hello implement — [[plan/core-creation/reports/redo-29-architecture-before-proceed.md]].
- [[plan/core-creation/issues/33-credentialed-browser-change-posts.md]] — Story path Browser Change posts: cookie-as-credential, boot seed, CoreMailbox admit only; Status `done`. `auth.Disabled` skip removed; development cookie is auto-issued and still required.
- [[plan/core-creation/issues/34b-outside-core-lifecycle-proof.md|34b — Outside Core lifecycle proof]] — Story path Outside Core lifecycle proof: TestActor hello from Pool/Actor seam without HTTP. Status `done` (independent review approve; report [[plan/core-creation/reports/code-review-34b-outside-core-lifecycle-proof.md]]).
- [[plan/core-creation/issues/35b-browser-run-hello.md|35b — Browser Run hello]] — Story path Browser Run hello: `?` one-Node Command through HTTP to Owned child `hello`; Status `done`. §6 durability → [[plan/core-creation/issues/49-mailbox-history-durability.md|49]]; §7 proof does not need 49.
- [[plan/core-creation/issues/49-mailbox-history-durability.md|49 — Mailbox History durability]] — persist/load the audit sequence; load reconcile Graph id vs EventLog tip (drop / noop / apply until concurrent). Status `done`. Blocked by [[plan/core-creation/issues/35b-browser-run-hello.md|35b — Browser Run hello]]. Not required for 35b §7 Browser proof. Plan: [[plan/core-creation/reports/49-mailbox-history-durability-explore.md|49 mailbox History durability explore]]. Reconcile: [[plan/core-creation/reports/49-mailbox-history-durability-reconcile.md|49 mailbox History durability reconcile]].
- [50 — Actor live result labels from Command node](plan/core-creation/issues/50-actor-live-labels-from-command.md) — Cancelled — folded into 21. Status `done` (Wayfinder task).
- [51 — Browser Run: Focus reply parent, Command is runnable ancestor](plan/core-creation/issues/51-browser-run-focus-vs-command.md) — Product Run distinct focusId/commandId; `?` ActorStart, `=` Amble; Status `coded`.
- [53 — Cancel HTTP conveys ActorStop](plan/core-creation/issues/53-cancel-http-conveys-actorstop.md) — Cancel HTTP success carries Cancelled `ActorStop` Events; Client applies them; no wrong-Focus stop after finish. Status `coded`.
- [[plan/core-creation/issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]] — Collapse extra Core entrances onto CoreMailbox; Status `done`. Report: [[plan/core-creation/reports/mailbox-single-door.md]].
- [[plan/core-creation/issues/37-expand-shared-event-eventlog-and-history.md|37 — Expand Shared Event, EventLog, and History]] — Story **Event, EventLog, and ClientHistory** Shared expand beside HistoryEvent; Event-shaped ClientHistory beside the Change-shaped API. No new History module. Status `done`.
- [[plan/core-creation/issues/40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]] — Story **Caller, persist, and Poll** expand: `postEvent`, EventLog store, Event JSON beside ChangeLog. Status `done`.
- [[plan/core-creation/issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]] — Story **Caller, persist, and Poll** Core migrate batch. Status `done`.
- [[plan/core-creation/issues/42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]] — Story **Caller, persist, and Poll** persist migrate batch. Status `done`.
- [[plan/core-creation/issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]] — Story **Caller, persist, and Poll** HTTP Adapter migrate batch. Status `done` (SES Event-only repair completed this path).
- [[plan/core-creation/issues/44-migrate-browser-poll-history-pending-and-eventid.md|44 — Migrate Browser Poll, History, pending, and EventId basis]] — Story **Caller, persist, and Poll** Browser migrate batch. Status `done` (SES Event-only repair completed this path).
- [[plan/core-creation/issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md|45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog]] — Story **Caller, persist, and Poll** contract. Deletes HistoryEvent, ActorLifecycleEvent, mailbox History name (replaced by EventLog), PendingKind, StartActorRequest, and the ChangeLog name. ClientHistory remains. Status `done` (SES Event-only repair completed this path).
- [46 — Workspace Load prepare-push 401](plan/core-creation/issues/46-workspace-run-prepare-push-401.md) — Desktop Load on a mapped Workspace Node fails with prepare-push HTTP 401. Status `done`.
- [47 — Server rejected Change: duplicate event id](plan/core-creation/issues/47-server-rejected-change-duplicate-event-id.md) — After Load, a Browser Change is rejected: Event persist duplicate `events_pkey`. Status `done`.
- [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md) — Retool EventId to Zero or positive Int; next of Zero is Zero; other tests do not lock stored serials. Status `done`.

## Decision tickets

- [[plan/core-creation/issues/03-define-typed-core-changes-contract.md]]
- [[plan/core-creation/issues/04-separate-http-adapter-from-core-changes.md]]
- [[plan/core-creation/issues/05-place-core-changes-in-existing-projects.md]]
- [[plan/core-creation/issues/06-ready-the-initial-core-changes-increment.md]]
- [[plan/core-creation/issues/07-define-core-files-contract.md]]
- [[plan/core-creation/issues/08-define-core-query-contract.md]]
- [[plan/core-creation/issues/09-define-core-command-launch-contract.md]]
- [[plan/core-creation/issues/10-define-actor-cancellation-and-output-admission.md]]
- [[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]]
- [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]]

## Reports

- [13 — Delete runtime mirror Persistence:Mode](reports/13-delete-runtime-mirror-persistence-mode.md) — production persist choice is DbStatus; leftover Persistence:Mode is ignored.
- [[plan/core-creation/reports/kernel-fsproj.md]] — Core boundary and module shape.
- [[plan/core-creation/reports/solid-core-module-fit.md]] — fit with the existing modules.
- [[plan/core-creation/reports/current-edit-core-reconciliation.md]] — current edit path and planned authority sequence.
- [[plan/core-creation/reports/create-project-reorganization.md]] — Project creation and ownership reorganization.
- [[plan/core-creation/reports/core-wayfinder-fact-inventory.md]] — evidence and open-choice inventory used to chart the map.
- [[plan/core-creation/reports/chart-core-wayfinder-map.md]] — map, ticket topology, and verification report.
- [[plan/core-creation/reports/plan-initial-core-changes-implementation.md]] — implementation-plan changes and verification.
- [[plan/core-creation/reports/implement-issue-01-actor-produce-path.md]] — test Actor produce path on Normal Core Changes.
- [[plan/core-creation/reports/implement-issue-14-credentials.md]] — Core credential set and one auth-refuse family.
- [[plan/core-creation/reports/commit-14-implement-15.md]] — commit [[plan/core-creation/issues/14-server-tracks-credentials.md]]; implement historical [[plan/core-creation/issues/15-launch-actor-and-hold-span.md]].
- [[plan/core-creation/reports/commit-15-what-is-16.md]] — commit historical [[plan/core-creation/issues/15-launch-actor-and-hold-span.md]]; describe [[plan/core-creation/issues/16-track-running-job.md]].
- [[plan/core-creation/reports/implement-issue-16.md]] — query a registered job by public number.
- [[plan/core-creation/reports/implement-issue-20.md]] — Browser presents the session cookie; missing cookie is auth-refuse.
- [[plan/core-creation/reports/core-api-boundary-review.md]] — Core API seam vs the map destination.
- [[plan/core-creation/reports/commit-16-mitigation-tickets.md]] — commit [[plan/core-creation/issues/16-track-running-job.md]] and [[plan/core-creation/issues/20-client-presents-credential.md]]; file [[plan/core-creation/issues/23-close-core-object-seam.md]] and [[plan/core-creation/issues/24-clarify-core-increment-boundary.md]] from the boundary review.
- [[plan/core-creation/reports/plan-23-24-mitigations.md]] — pointer at the locked 24-then-23 plan.
- [[plan/core-creation/reports/implement-24-then-23.md]] — 24 instruction then 23 Core object seam.
- [[plan/core-creation/reports/actor-core-and-mailbox-check.md]] — Actor `CoreChanges` handle vs mailbox; writes are the two Posts.
- [[plan/core-creation/reports/actor-pool-rewind-review.md]] — sets 1–3 review: one mailbox, pool is TaskPool; product rewound; rebuild.
- [[plan/core-creation/reports/set3-review-handoff.md]] — set 3 review closed.
- [[plan/core-creation/reports/commit-24-and-23.md]] — commit 24 then 23 on `dev`.
- [[plan/core-creation/reports/improve-codebase-architecture.md]] — Core hot-spot deepening candidates; top recommendation is 25.
- [[plan/core-creation/reports/implement-issue-25.md]] — bind Browser Changes on Core; Adapter decode and status only.
- [[plan/core-creation/reports/grill-issue-09-launch-contract.md]] — start grill of the Core Command launch contract.
- [[plan/core-creation/reports/grill-issue-10-cancellation.md]] — start grill of Actor cancellation and output admission.
- [[plan/core-creation/reports/grill-issue-11-finish.md]] — grill of Actor finish and failure behavior.
- [[plan/core-creation/reports/grill-issue-12-shutdown.md]] — start grill of Actor-pool shutdown behavior.
- [[plan/core-creation/reports/implement-issue-29-testactor-hello.md]] — public Core TestActor hello on the FileAgent apply mailbox.
- [[plan/core-creation/reports/align-29-referenced-completed-details.md]] — completed-detail `[x]` pass on files referenced by 29.
- [[plan/core-creation/reports/file-db-agent-mailbox-twins.md]] — FileAgent and DbAgent persist twins plus CoreActorMailbox module.
- [[plan/core-creation/reports/redo-29-architecture-before-proceed.md]] — ticket 29 redo: keep map/tickets/partial implement; `/to-arch` before remaining hello sections.
- [[plan/core-creation/reports/remove-auth-disabled-bypass.md]] — remove `auth.Disabled` skip; development cookie is real and required.
- [[plan/core-creation/reports/implement-34-section-1-coremsg.md]] — issue 34 section 1 CoreMsg / CoreMailboxBackend Actor cases.
- [[plan/core-creation/reports/mailbox-start-type-corrections.md]] — StartActorRequest replaces LaunchRequest; Caller; no ActorMailboxHandlers; async StartActor handoff.
- [[plan/core-creation/reports/sync-startactor-one-mailbox.md]] — Sync startActor, one CoreMsg host, mailbox-owned live table, File or Db persist, mirror deleted.
- [[plan/core-creation/reports/corecredentials-caller-set.md]] — CoreCredentials is a mailbox-owned Set of Caller; login maps name+secret.
- [[plan/core-creation/reports/mailbox-single-door.md]] — Mailbox is the only Core door: one admission, thinned CoreRuntime, credentialed Graph-only, hidden CoreMsg.
- [[plan/core-creation/reports/36-review-corrections.md]] — Review corrections for [[plan/core-creation/issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]].
- [[plan/core-creation/reports/ambitapp-record.md]] — AmbitApp record for the six-arg route clump leftover from [[plan/core-creation/issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]].
- [[plan/core-creation/reports/gitgateway-routes-type.md]] — GitGateway.Routes for the shell/flush/reconcile clump leftover from [[plan/core-creation/issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]].
- [[plan/core-creation/reports/actor-corechanges-mailbox-door.md]] — Actors use mailbox `coreChanges`; no second `makeCoreChanges` swallow.
- [[plan/core-creation/reports/actorstop-single-admit.md]] — ActorStop admits once; `pool.finish` drops without a third admit.
- [[plan/core-creation/reports/cancel-poll-eventhistory-undo.md]] — Cancelled poll-carried eventHistory / ClientHistory replacement; increment reverted.
- [[plan/core-creation/reports/event-abstraction.md]] — Locked Event / EventLog / ClientHistory / `postEvent` destination.
- [[plan/core-creation/reports/implement-issue-40.md]] — [[plan/core-creation/issues/40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]].
- [implement-46-workspace-load-prepare-push-401](plan/core-creation/reports/implement-46-workspace-load-prepare-push-401.md) — [46 — Workspace Load prepare-push 401](plan/core-creation/issues/46-workspace-run-prepare-push-401.md).
- [qa-47-event-persist-duplicate-key](plan/core-creation/reports/qa-47-event-persist-duplicate-key.md) — QA context for [47 — Server rejected Change: duplicate event id](plan/core-creation/issues/47-server-rejected-change-duplicate-event-id.md).
- [[plan/core-creation/reports/35b-slice1-graphids.md]] — [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) slice 1 Shared `graphIds` Fold walk.
- [[plan/core-creation/reports/35b-slice2-3-http-browser-run.md]] — [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) slices 2–3 HTTP Adapter and Browser Run.
- [[plan/core-creation/reports/35b-slice4-5-7-core-testactor-browser-proof.md]] — [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) slices 4+5+7 Core TestActor and Browser proof.
- [35b slices 4+5+7 standards and Spec corrections](plan/core-creation/reports/35b-slice4-5-7-standards-spec-corrections.md) — [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) Origin Spec review: unregistered Actor name and non-hello command fail.
- [Independent review — 52 Run abort when commit fails](reports/code-review-52-run-abort-when-commit-fails.md) — [52 — Run must not launch when edit commit fails](issues/52-run-abort-when-commit-fails.md). Status stays `coded`.
- [Re-review — 52 Client RunLaunch](reports/code-review-52-rereview-client-runlaunch.md) — [52 — Run must not launch when edit commit fails](issues/52-run-abort-when-commit-fails.md). Status stays `coded`.
- [Independent review — 51 Focus vs Command](reports/independent-review-51-focus-vs-command.md) — [51 — Browser Run: Focus reply parent, Command is runnable ancestor](issues/51-browser-run-focus-vs-command.md). Status stays `coded`.
- [Independent re-review — 51 Focus vs Command](reports/independent-review-51-focus-vs-command-rereview.md) — after Amble lock. Standards Approve with nits; Spec Approve. Status stays `coded`.
- [49 mailbox History durability](plan/core-creation/reports/49-mailbox-history-durability.md) — [49 — Mailbox History durability](issues/49-mailbox-history-durability.md) implement: seed order, File+Db recover, one serial.
- [Arch reconcile 35b and 49 landed](reports/arch-reconcile-35b-46-landed.md) — [Core creation architecture](arch.md) checkboxes after [35b — Browser Run hello](issues/35b-browser-run-hello.md) and [49 — Mailbox History durability](issues/49-mailbox-history-durability.md). Remaining unchecked → covering ticket.

## Comments

- 2026-09-19 — Squash-landed [[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md|13 — Delete runtime mirror / Persistence:Mode]]. Status `done`.
- 2026-09-19 — Implemented [13 — Delete runtime mirror and remove production Persistence:Mode](issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md). Status `done`. Report: [13 — Delete runtime mirror Persistence:Mode](reports/13-delete-runtime-mirror-persistence-mode.md).
- 2026-09-19 — Reconciled [Core creation architecture](arch.md) to landed [35b — Browser Run hello](issues/35b-browser-run-hello.md) (`done`) and [46 — Mailbox History durability](issues/46-mailbox-history-durability.md) (`done`). Hello-cut checkboxes are `[x]`. Remaining unchecked: none. Report: [Arch reconcile 35b and 46 landed](reports/arch-reconcile-35b-46-landed.md).
- 2026-09-11 — Added [[plan/core-creation/issues/29-prove-testactor-hello.md]].
- 2026-09-11 — One-home DRY of Phase 2 issues and locked 06/07. Command text `?test hello` is owned by [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]].
- 2026-09-12 — Reviews of the TestActor hello increment failed. Code was stashed. Yesterday's issue checkboxes and implementation logs for that increment were cleared.
- 2026-09-13 — Added [[plan/core-creation/issues/32-move-persist-agents-under-coremailbox.md]]. Point 0 current cut is 32.
- 2026-09-13 — Section 1 of [[plan/core-creation/issues/29-prove-testactor-hello.md]] implemented on `dev`.
- 2026-09-13 — Redo gate: architecture before remaining hello sections — [[plan/core-creation/reports/redo-29-architecture-before-proceed.md]].
- 2026-09-13 — [[plan/core-creation/arch.md]] written via to-arch; Stage `arch`. Critique Story paths / Module map / Seams / Sequence / Alternative / Unsettled before reconciling tickets or implementing sections 2–6.
- 2026-09-13 — `/to-tickets` for Story path Browser Change posts only; published [[plan/core-creation/issues/33-credentialed-browser-change-posts.md]]; Stage `slice`.
- 2026-09-13 — First implement: [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|Credentialed Browser Change posts]] done; Stage `build`.
- 2026-09-13 — Redesign on [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|Credentialed Browser Change posts]]: cookie/seed target; undo File/Db/GUID deltas; Status `ready-for-agent` again. Arch Story path checkboxes still `[x]` — align when reconciling.
- 2026-09-13 — Re-implement [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|Credentialed Browser Change posts]] against redesign; Status `done`.
- 2026-09-14 — Spec-gap fix on [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|Credentialed Browser Change posts]]: request-carried Browser API creds, client DeployEpochSec reseed, no closed-over cookie fallback.
- 2026-09-14 — `/to-tickets` for Story paths Outside Core lifecycle proof and Browser Run hello; published 34 — Outside Core lifecycle proof and 35 — Browser Run hello (later deleted). Stage `slice`. Parent [[plan/core-creation/issues/29-prove-testactor-hello.md|Prove TestActor hello]] unchanged.
- 2026-09-14 — Deleted 34 — Outside Core lifecycle proof and 35 — Browser Run hello. Living story tickets remain [[plan/core-creation/issues/34b-outside-core-lifecycle-proof.md|34b — Outside Core lifecycle proof]] and [[plan/core-creation/issues/35b-browser-run-hello.md|35b — Browser Run hello]].
- 2026-09-14 — [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|Credentialed Browser Change posts]] Status back to `ready-for-agent`: coded is not `done`; `done` is review approval only.
- 2026-09-14 — Spec of [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|Credentialed Browser Change posts]] aligned to review findings; auth-disabled app-serve SetCookie rejected and remaining.
- 2026-09-14 — Removed `auth.Disabled` skip on [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|Credentialed Browser Change posts]]; Status `done`; Stage `build`. Report: [[plan/core-creation/reports/remove-auth-disabled-bypass.md]].
- 2026-09-14 — [[plan/core-creation/issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]] Status `done`. Report: [[plan/core-creation/reports/mailbox-single-door.md]].
- 2026-09-15 — Review corrections for [[plan/core-creation/issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]]. Status stays `coded`. Report: [[plan/core-creation/reports/36-review-corrections.md]].
- 2026-09-15 — AmbitApp record for the route parameter clump. Status of [[plan/core-creation/issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]] stays `coded`. Report: [[plan/core-creation/reports/ambitapp-record.md]].
- 2026-09-15 — GitGateway.Routes for the git registration clump. Status of [[plan/core-creation/issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]] stays `coded`. Report: [[plan/core-creation/reports/gitgateway-routes-type.md]].
- 2026-09-15 — Actors use mailbox `coreChanges`. Status of [[plan/core-creation/issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]] stays `coded`. Report: [[plan/core-creation/reports/actor-corechanges-mailbox-door.md]].
- 2026-09-15 — ActorStop admits once. Status of [[plan/core-creation/issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]] stays `coded`. Report: [[plan/core-creation/reports/actorstop-single-admit.md]].
- 2026-09-15 — Removed State.history. Mailbox `eventHistory` is the one History (undo stack); EventLog is durability and restore, not a post-time copy. ClientHistory is leftover and still does Browser undo.
- 2026-09-15 — Cancelled poll-carried eventHistory / mailbox undo door. Reverted that increment only. Report: [[plan/core-creation/reports/cancel-poll-eventhistory-undo.md]].
- 2026-09-15 — `/to-arch` Event destination on existing [[plan/core-creation/arch.md]]. Completed hello stories unchanged. Module map is destination-only. New stories **Event, EventLog, and History** and **Caller, persist, and Poll** are expand-migrate-contract. Stage `arch`. Report: [[plan/core-creation/reports/event-abstraction.md]].
- 2026-09-15 — `/to-tickets` for Story **Event, EventLog, and History** only; published [[plan/core-creation/issues/37-expand-shared-event-eventlog-and-history.md|37 — Expand Shared Event, EventLog, and History]]. Stage `slice`. Story **Caller, persist, and Poll** not ticketed.
- 2026-09-15 — `/to-tickets` for Story **Caller, persist, and Poll** only; published [[plan/core-creation/issues/40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]] through [[plan/core-creation/issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md|45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog]]. Numbers start at 40. Story 4 tickets unchanged. Stage `slice`.
- 2026-09-15 — EventLog is the Event sequence and its persist. ChangeLog is a lagging code name only (`src/Server/ChangeLog.fs`).
- 2026-09-15 — Destination module 6 and report §3.3 are ClientHistory, not History. EventLog is the sequence (today’s mailbox `type History` / `module History` is the lagging name). ClientHistory stays at [[src/Shared/ClientHistory.fs]]. No destination module named History.
- 2026-09-15 — [[plan/core-creation/issues/37-expand-shared-event-eventlog-and-history.md|37 — Expand Shared Event, EventLog, and History]] Status `done`. Event types landed under Shared (later `Ev` in `Gambol.Shared`; see 2026-09-16). Stage `build`. Report: [[plan/core-creation/reports/implement-issue-37.md]].
- 2026-09-15 — [[plan/core-creation/issues/40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]] Status `done`. Did not edit EventLog / Event / ClientHistory (newest-head redesign lock). Report: [[plan/core-creation/reports/implement-issue-40.md]].
- 2026-09-15 — [[plan/core-creation/issues/40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]] Event JSON encode/read on EventLog. Deleted EventJson and ChangeLog Event codec. Status stays `coded`.
- 2026-09-16 — Shared Event record and helpers are `Ev` in `Gambol.Shared`. Namespace `Gambol.Shared.Events` is gone. Related types (`EventId`, `EventBody`, `EventLog`, `EventJson`, `Authority`, `ActorStart`, `ActorResult`) stay in `Gambol.Shared`. Arch and dependents: [[plan/core-creation/reports/ev-rename-arch-docs.md]].
- 2026-09-16 — Suspended until [[plan/single-event-source/map.md]] creates the Event-only architecture. Then [[plan/core-creation/arch.md]] is updated to match. Do not add implementation issues here for that cleanup.
- 2026-09-19 — Rebuild: Spec-review approve → `done` for 33, 36, 37, 40, 41, 42; 35b `done`; arch reconciled after 35b/49 (report [[plan/core-creation/reports/arch-reconcile-35b-46-landed.md]]).
- 2026-09-18 — [46 — Workspace Load prepare-push 401](plan/core-creation/issues/46-workspace-run-prepare-push-401.md) Status `coded`. Report: [implement-46-workspace-load-prepare-push-401](plan/core-creation/reports/implement-46-workspace-load-prepare-push-401.md).
- 2026-09-18 — [47 — Server rejected Change: duplicate event id](plan/core-creation/issues/47-server-rejected-change-duplicate-event-id.md) filed from QA. Status `defined`.
- 2026-09-18 — [47 — Server rejected Change: duplicate event id](plan/core-creation/issues/47-server-rejected-change-duplicate-event-id.md) Status `coded`. EventLog nextId catch-up from persisted event_id; persist messages say Event.
- 2026-09-18 — [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md) filed. Status `defined`. Follow-on to [47 — Server rejected Change: duplicate event id](plan/core-creation/issues/47-server-rejected-change-duplicate-event-id.md): Zero or positive Int; next of Zero is Zero; tests check event id numbers only in EventId builder tests.
- 2026-09-18 — [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md) Status `done`. Alan approved the 4.2 review ([code-review-48-other-tests-do-not-lock-serials](plan/core-creation/reports/code-review-48-other-tests-do-not-lock-serials.md)); SerializationTests wire 3/4 and DbAgentTests growth accepted. Stage stays `build`.
- 2026-09-19 — Rebuild onto ready: marked [[plan/core-creation/issues/34b-outside-core-lifecycle-proof.md|34b — Outside Core lifecycle proof]] `done` after independent review; unblocked [[plan/core-creation/issues/35b-browser-run-hello.md|35b — Browser Run hello]] (`defined`, no Blocked-by).
- 2026-09-19 — Rebuild: split 35b History durability to [49 — Mailbox History durability](issues/49-mailbox-history-durability.md) (ready already used 46 for workspace prepare-push).
- 2026-09-19 — Rebuild: marked [[plan/core-creation/issues/49-mailbox-history-durability.md|49 — Mailbox History durability]] `done` (EventLog tip reconcile + recover) after replaying former staging land `fce22cf7`.
