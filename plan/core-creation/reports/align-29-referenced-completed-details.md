# Align completed details on files referenced by 29

Date: 2026-09-11. `[x]` means a locked spec or grilling fact is complete. It is not an implementation-progress mark. `[ ]` means an undelivered proof or rebuild box. No commit.

Dispatch home is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]. Command text is `?test hello`. This report does not add a parse of `test` versus `hello`.

## [[../issues/29-prove-testactor-hello.md]]

Kept the Context rewind sentence, the Phase 2 exclusion, the draws-from sentence, and the out-of-scope list.

`[x]` locked spec: increment purpose; public Core request members; universal `{ nodes; events; latestId }` response; normal composition; Command Node text is the dispatch; command text `?test hello`; no role/Kind/CSS/Focus-Header selection; Authority plus secret on launch and post; register against Focus, ActorStarted, schedule, mailbox free; ActorStarted and registry before output; one hello Change then Succeeded; Succeeded is Core-only; one ActorFinished, drop registry and secret, terminate only if still running; one-mailbox rebuild and no wrap-patch; out-of-scope list; observe Graph and registry from outside.

`[ ]` undelivered proof: tests launch; outer fact sees ActorStarted before output; hello Graph; one ActorFinished seen; secret revoked after drop; no live service.

## [[../issues/01-generalized-server-actor-produce-path.md]]

Status `done`. All six produce-path proof boxes stay `[x]`. No box was opened. This delivery is not 29's hello proof.

## [[../issues/12-define-actor-pool-shutdown-behavior.md]]

Status `done`. Answer has no checkboxes. Locked Database-down versus host-stop prose stays unmarked. Did not invent a list.

## [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]

Status `done`. `[x]` every existing Locked architecture bullet and every Named test seam bullet. Sequences stay numbered prose. Dropped the “command text lives on 29” deferral. Dispatch stays on 06.

## [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]

Status `done`. All sixteen Answer boxes were already `[x]`. Left them `[x]`. Dropped the 29 command-text deferral. This ticket owns `?test hello` and `?ai ...`.

## [[../issues/02-core-actor-pool.md]]

`[x]` locked design-before-implementation and the scope exclusion (no Actor definitions, Browser chrome, or soft-lock UI here).

`[ ]` launch-does-not-hold-mailbox and one-mailbox rebuild. Those are Phase 2 launch-and-query rebuild boxes. 29 did not deliver them.

## [[../issues/14-server-tracks-credentials.md]]

`[x]` all five existing admission and Event-attribution boxes. They are locked 07 facts. Status stays `blocked`. This mark is not the one-mailbox credential rebuild.

## [[../issues/15-launch-actor-and-hold-span.md]]

`[x]` the three existing locked Focus-registration sentences (registry fields, ActorStarted then schedule, refuse only same Focus). Did not add launch-and-query proof boxes. Status stays `blocked`.

## [[../issues/18-finish-and-drop.md]]

`[x]` the five existing finish-and-drop contract boxes. They are locked 07 facts. Status stays `blocked`. This mark is not the Phase 2 rebuild and not the 27 catalog.

## [[../issues/27-prove-core-actor-lifecycle-with-testactor.md]]

`[x]` observe Graph and registry from outside; TestActor does not assert.

`[ ]` tests launch through the normal path; full catalog; no live service. This ticket stays the catalog. 29 is the hello slice, not the eight-item program.

## [[../issues/Implementation Planning and Record.md]]

No checkboxes. Left unmarked. Phase 2 still names the eight program pieces. 29 remains the current implement cut.

## [[actor-pool-rewind-review.md]]

No checkboxes added. Replaced the Focus-Header case-id restatement with 06 Command-text dispatch and command text `?test hello`.

## [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]]

`[x]` the two existing FIFO bullets. Cancel and finish prose stays unmarked.

## [[implement-issue-29-testactor-hello.md]]

No checkboxes. Left unmarked.

## Not marked as 29-delivered

Did not edit [[../issues/16-track-running-job.md]], [[../issues/17-cancel-a-job.md]], or [[../issues/28-drain-actor-lifecycle-on-host-stop.md]]. Those Phase 2 pieces are not this increment.

## Related 06 homes

Dropped the 29 command-text deferral from [[plan/llm-connector/project.md]], [[plan/llm-connector/map.md]], [[plan/llm-connector/reports/agent-redesign-locked-2026-09.md]], and [[plan/llm-connector/reports/lock-run-agent-architecture-fact-inventory.md]].
