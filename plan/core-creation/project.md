# Core creation

Stage: build
Summary: Establish Core and Core API as the sole Server Graph writer, persistent-state coordinator, and Actor pool.
Updated: 2026-09-17
Started: 2026-09-05
Actual: 51h50m

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
- This increment does not add Browser lifecycle UI. [[plan/core-creation/issues/21-client-shows-lock-present.md]] belongs with [[plan/event-sourced-ops/project.md]].
- Locked code plan: [[plan/core-creation/mitigations.md]].

## Implementation plan

- [[plan/core-creation/issues/Implementation Planning and Record.md]]

## Issues

- [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] — establish the shared Core Changes path.
- [[plan/core-creation/issues/02-core-actor-pool.md]] — establish Core-owned Actor pool machinery. Status `ready-for-agent` after rewind named the mailbox/TaskPool shape.
- [[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]] — use Database persistence when available and reject Changes when unavailable.
- [[plan/core-creation/issues/14-server-tracks-credentials.md]] — public/secret Authority admission and durable readable Authority; Status `blocked` by the Actor pool baseline.
- [[plan/core-creation/issues/15-launch-actor-and-hold-span.md]] — historical span delivery superseded by typed launch and ActorStarted; Status `blocked`.
- [[plan/core-creation/issues/16-track-running-job.md]] — live public-identity query plus durable lifecycle Events; Status `blocked`.
- [[plan/core-creation/issues/17-cancel-a-job.md]] — terminal Cancelled by Focus NodeId without Undo; Status `blocked`.
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
- [[plan/core-creation/issues/33-credentialed-browser-change-posts.md]] — Story path Browser Change posts: cookie-as-credential, boot seed, CoreMailbox admit only; Status `coded`. `auth.Disabled` skip removed; development cookie is auto-issued and still required.
- [[plan/core-creation/issues/34b-outside-core-lifecycle-proof.md|34b — Outside Core lifecycle proof]] — Story path Outside Core lifecycle proof: TestActor hello from Pool/Actor seam without HTTP. Status `done` (independent review approve; report [[plan/core-creation/reports/code-review-34b-outside-core-lifecycle-proof.md]]).
- [[plan/core-creation/issues/35b-browser-run-hello.md|35b — Browser Run hello]] — Story path Browser Run hello: `?` one-Node Command through HTTP to Owned child `hello`; Status `ready-for-agent` (34b `done`).
- [[plan/core-creation/issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]] — Collapse extra Core entrances onto CoreMailbox; Status `coded`. Report: [[plan/core-creation/reports/mailbox-single-door.md]].
- [[plan/core-creation/issues/37-expand-shared-event-eventlog-and-history.md|37 — Expand Shared Event, EventLog, and History]] — Story **Event, EventLog, and ClientHistory** Shared expand beside HistoryEvent; Event-shaped ClientHistory beside the Change-shaped API. No new History module. Status `coded`.
- [[plan/core-creation/issues/40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]] — Story **Caller, persist, and Poll** expand: `postEvent`, EventLog store, Event JSON beside ChangeLog. Status `coded`.
- [[plan/core-creation/issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]] — Story **Caller, persist, and Poll** Core migrate batch. Status `coded`.
- [[plan/core-creation/issues/42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]] — Story **Caller, persist, and Poll** persist migrate batch. Status `coded`.
- [[plan/core-creation/issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]] — Story **Caller, persist, and Poll** HTTP Adapter migrate batch. Status `done` (SES Event-only repair completed this path).
- [[plan/core-creation/issues/44-migrate-browser-poll-history-pending-and-eventid.md|44 — Migrate Browser Poll, History, pending, and EventId cursor]] — Story **Caller, persist, and Poll** Browser migrate batch. Status `done` (SES Event-only repair completed this path).
- [[plan/core-creation/issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md|45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog]] — Story **Caller, persist, and Poll** contract. Status `done` (SES Event-only repair completed this path).

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
- [[plan/core-creation/reports/35b-slice1-graphids.md]] — [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) slice 1 Shared `graphIds` Fold walk.

## Comments

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
- 2026-09-14 — Removed `auth.Disabled` skip on [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|Credentialed Browser Change posts]]; Status `coded`; Stage `build`. Report: [[plan/core-creation/reports/remove-auth-disabled-bypass.md]].
- 2026-09-14 — [[plan/core-creation/issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]] Status `coded`. Report: [[plan/core-creation/reports/mailbox-single-door.md]].
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
- 2026-09-15 — [[plan/core-creation/issues/37-expand-shared-event-eventlog-and-history.md|37 — Expand Shared Event, EventLog, and History]] Status `coded`. Event types landed under Shared (later `Ev` in `Gambol.Shared`; see 2026-09-16). Stage `build`. Report: [[plan/core-creation/reports/implement-issue-37.md]].
- 2026-09-15 — [[plan/core-creation/issues/40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]] Status `coded`. Did not edit EventLog / Event / ClientHistory (newest-head redesign lock). Report: [[plan/core-creation/reports/implement-issue-40.md]].
- 2026-09-15 — [[plan/core-creation/issues/40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]] Event JSON encode/read on EventLog. Deleted EventJson and ChangeLog Event codec. Status stays `coded`.
- 2026-09-16 — Shared Event record and helpers are `Ev` in `Gambol.Shared`. Namespace `Gambol.Shared.Events` is gone. Related types (`EventId`, `EventBody`, `EventLog`, `EventJson`, `Authority`, `ActorStart`, `ActorResult`) stay in `Gambol.Shared`. Arch and dependents: [[plan/core-creation/reports/ev-rename-arch-docs.md]].
- 2026-09-16 — Suspended until [[plan/single-event-source/map.md]] creates the Event-only architecture. Then [[plan/core-creation/arch.md]] is updated to match. Do not add implementation issues here for that cleanup.
- 2026-09-17 — Marked [[plan/core-creation/issues/34b-outside-core-lifecycle-proof.md|34b — Outside Core lifecycle proof]] `done` after independent review approve. Unblocked [[plan/core-creation/issues/35b-browser-run-hello.md|35b — Browser Run hello]] → `ready-for-agent`.
- 2026-09-17 — Resume after SES Event-only repair on staging. Story **Caller, persist, and Poll** migrate/contract (43–45) is `done`. Next open Story path: **Browser Run hello** ([[plan/core-creation/issues/35b-browser-run-hello.md|35b — Browser Run hello]]), blocked only by review of [[plan/core-creation/issues/34b-outside-core-lifecycle-proof.md|34b — Outside Core lifecycle proof]] (`coded`).
