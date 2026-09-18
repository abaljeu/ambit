# Record locked Run Agent architecture

Date: 2026-09-11. Documentation and planning only. No product code or Committed Decision was added, and no git commit was made.

## Changed files

- Architecture and Project: [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]], [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]], [[plan/llm-connector/project.md]], [[plan/llm-connector/map.md]].
- Controlling Core plan: [[plan/core-creation/issues/Implementation Planning and Record.md]], [[plan/core-creation/project.md]], [[plan/core-creation/map.md]].
- Core lifecycle issues: [[plan/core-creation/issues/02-core-actor-pool.md]], [[plan/core-creation/issues/09-define-core-command-launch-contract.md]], [[plan/core-creation/issues/10-define-actor-cancellation-and-output-admission.md]], [[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]], [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]], [[plan/core-creation/issues/14-server-tracks-credentials.md]], [[plan/core-creation/issues/15-launch-actor-and-hold-span.md]], [[plan/core-creation/issues/16-track-running-job.md]], [[plan/core-creation/issues/17-cancel-a-job.md]], [[plan/core-creation/issues/18-finish-and-drop.md]], [[plan/core-creation/issues/19-database-down-and-host-stop.md]], [[plan/core-creation/issues/21-client-shows-lock-present.md]], [[plan/core-creation/issues/26-failed-actor-stop-still-drops.md]], [[plan/core-creation/issues/27-prove-core-actor-lifecycle-with-testactor.md]], and [[plan/core-creation/issues/28-drain-actor-lifecycle-on-host-stop.md]].
- Glossary: [[CONTEXT.md]].

## Key reconciliations

- Added the completed architecture/spec issue with 2h10m Actual and chat Time. The llm-connector Project remains `spec`, Updated stays 2026-09-11, and Actual increased from 4h45m to 6h55m.
- Locked exact Browser launch membership, Core-owned extraction and ActorName interpretation, Authority identity, one global Event sequence, one Core API with a universal response, and one-mailbox lifecycle ordering.
- Replaced span locks, Graph lock-present outside History, delete-only completion, and no-terminal-result assumptions with ActorStarted and ActorFinished lifecycle Events.
- Kept normal Change, Undo, Redo, deduplication, merge, amendment, and Reference-Paste-style replacement. Run Agent response write-back uses normal Core Change.
- Reopened the rewound Authority, launch, query, finish, and cancel issues where replacement contracts remain undelivered. Their blockers now follow the serial Phase 2 order.
- Made Phase 2 point-to-issue mapping bijective: eight numbered points, one unaliased full-path issue link per point, and no issue linked by more than one point.
- Added implementation-free Authority, Actor, Event, event id, retired Revision, History, and Poll glossary language.

## Deferred details

Parser and helper mechanics, Focus sentinel spelling and escaping, exact persistence implementation, provider-selection specifics, file-state and import-current operations, lower-level type shapes, and implementation bodies remain deferred. The full vertical proof remains Phase 4.

## Verification

- Ran [[scripts/gitstatus.sh]] before edits.
- Checked the eight numbered Phase 2 lines. Each contains exactly one issue path.
- Counted Phase 2 issue paths. Every path occurs once.
- Searched every edited file, including this report, for aliased wikilinks; none remain.
- Checked edited wikilink targets and corrected the stale Project-work skill path in the Core map.
- Searched the changed controlling plans for the superseded span, Graph lock-present, no-terminal-result, and separate Revision assumptions. Current sections point to [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]; historical grill comments are explicitly labeled non-controlling.
- No tests or build were run because the changes are Markdown only.

## Unresolved contradictions

None found in the controlling plans. Historical comments still record the superseded choices as history, but each affected issue now states that those comments do not direct implementation.
