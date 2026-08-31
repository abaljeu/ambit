# Server Change augmentation audit

Audit status: complete against current source on project branch w/selective-client-loading-undo.

## Conclusion

There is one current request-time augmentation of a Browser-submitted Change: live Document persistence can append server-produced SetUpdateTime Ops. No current code uses the fully resident Server Graph to append ownership, Ref-promotion, placement, delete, move, or other corrective Ops. The full Graph validates the submitted Ops and can reject them, but it does not expand them.

Current ChangeRequest.Undo and ChangeRequest.Redo are different. They contain no Ops. The Server uses process-local canonical History to materialize each request as a Change, then the same persistence-stamp path can append SetUpdateTime Ops to the last materialized Change in the batch.

Parse and directory reconciliation also use canonical Graph and disk facts to plan Changes. Those Changes are Server-generated results of separate requests. They do not enrich a Browser-submitted Change and are not ACK enrichment.

## The one actual augmentation: persistence stamps

### Trigger

The trigger is an accepted, graph-changing, non-graph-only batch when live Document persistence is configured. FileAgent always has a data directory. DbAgent produces these Ops only when liveSaveDataDir is Some; a database-only agent has no live file-write stamp augmentation. FileAgent gates persistence on changed and not graphOnly at [[src/Server/FileAgent.fs]] lines 203–239. DbAgent gates it on not graphOnly, a live data directory, and at least one ChangeLog entry at [[src/Server/DbAgent.fs]] lines 207–248.

DocumentOpImpact treats NewNode, SetText, SetClasses, NewSpecialNode, SetName, SetDocumentState, and Replace as possible Document impacts; it deliberately excludes SetUpdateTime at [[src/Shared/DocumentOpImpact.fs]] lines 14–40. It resolves the affected current writable Document roots from both pre- and post-Graphs and includes path moves at [[src/Shared/DocumentPartition.fs]] lines 80–124. Therefore normal Browser commands that create, edit, rename, move, paste, delete, or change Document state can trigger stamps when their Ops dirty a writable Document. A stamp-only explicit Download alignment does not recursively trigger a write because SetUpdateTime has no Document impact.

### Extra Ops and required Server facts

Document persistence writes affected Documents, reads each resulting file mtime, and stamps the canonical Graph at [[src/Server/DocumentPersistence.fs]] lines 627–656 and 667–693. PersistStamp.opsBetween compares the accepted Graph with that stamped Graph and emits only Op.SetUpdateTime(nodeId, oldTime, newTime) at [[src/Shared/History.fs]] lines 741–761.

The actual disk mtime is a Server-only fact. The Server also needs its fully resident Graph to resolve touched Nodes and path moves to their canonical writable Document roots. No other Op kind is produced by this post-apply comparison.

### Absent and Unloaded Nodes

The stamped Node is a Document root. It can be Resident with Unloaded Children; ResidentProjection applies Header Ops, including SetUpdateTime, whenever the Header is Resident, without loading Children at [[src/Shared/ResidentProjection.fs]] lines 7–25. It can also be Absent when a command changes a Resident Ref target whose canonical Owned chain and containing Document root are outside the Browser's resident Workspaces. The Server can still resolve and stamp that root from the full Graph. The Browser then consumes that appended Op as a projected no-op. The residency model explicitly permits reachable Ref Headers without their Children and preserves canonical owner identity even when the owner edge is not resident at [[.scratch/selective-client-loading/spec.md]] lines 73–81.

### Batch assignment

Both agents flatten the Ops from all newly logged Changes and persist the batch Graph once: [[src/Server/FileAgent.fs]] lines 227–236 and [[src/Server/DbAgent.fs]] lines 233–247. PersistStamp.appendToLast appends every stamp Op from that aggregate persistence pass to the last ChangeLog entry only at [[src/Shared/History.fs]] lines 763–776. FileAgent uses it at [[src/Server/FileAgent.fs]] lines 243–258; DbAgent uses it at [[src/Server/DbAgent.fs]] lines 252–267.

This assignment is by last logged Change, not by the command that dirtied each Document. A batch can therefore assign stamps caused by earlier Changes to the final logged Change. A trailing duplicate or unchanged request has no new ChangeLog entry, so the stamps attach to the last request in the batch that did produce an entry. If there is no entry, there is no persistence pass and no stamp augmentation.

For an ordinary final Change, its submitted Ops remain an exact prefix and the stamps are appended. For a final Undo or Redo request, the materialized Ops are the prefix; the request itself submitted no Ops. Earlier logged Changes in the batch are unchanged.

### ACK and ChangeLog

The complete enriched Change is persisted to ChangeLog. Poll and Load read that payload through getChangesSince at [[src/Server/FileAgent.fs]] lines 307–316, [[src/Server/DbAgent.fs]] lines 340–352, and [[src/Server/Api.fs]] lines 108–127 and 153–190.

The current ACK does not return complete Changes. ChangeBatchAck contains revision, ackedChangeIds, aggregate stampOps, and an optional message at [[src/Shared/Serialization.fs]] lines 8–17 and 470–487. The Browser applies stampOps directly to its Graph at [[src/Client/Update.fs]] lines 73–105; it does not amend the matching History Change. Thus the complete Change exists only in ChangeLog, while its appended Ops are also returned separately and without per-Change association in the ACK.

A duplicate submission is acknowledged by id without loading its prior ChangeLog payload: [[src/Server/FileAgent.fs]] lines 111–150 and [[src/Server/DbAgent.fs]] lines 82–95. After a lost first response, the retry ACK can therefore omit the original stamps. This confirms the retry gap described in [[undo-wayfinder.md]] lines 97–108.

The process-local Server History is also not enriched. The agents apply or materialize Actions before persistence, then replace only the Graph with the stamped Graph at [[src/Server/FileAgent.fs]] lines 145–175 and 243–268 and [[src/Server/DbAgent.fs]] lines 89–120 and 252–275. ChangeLog has the stamps; Server History keeps the pre-stamp Change.

## Current materialization that is not augmentation of submitted Ops

ChangeRequest.Change carries a Change, but Undo and Redo carry only Revision and changeId at [[src/Shared/History.fs]] lines 23–57. History.applyAction materializes Undo by inverting the head past Change and materializes Redo from the head future Change, replacing identity with the request values at [[src/Shared/History.fs]] lines 702–739. FileAgent and DbAgent persist that materialized Change at [[src/Server/FileAgent.fs]] lines 155–175 and [[src/Server/DbAgent.fs]] lines 100–120.

This requires canonical process-local History, not merely the full Graph. It is proven downstream behavior: [[tests/Server.Tests/StateEndpointTests.fs]] lines 210–245 shows an ACK with only ids and Poll with the three materialized Changes. It is not a case where submitted Ops are preserved as a prefix because Undo and Redo submit no Ops.

## Full-Graph and persistence validation: rejection, not augmentation

The Server applies the exact submitted Change to its canonical Graph through History.applyChange at [[src/Server/FileAgent.fs]] lines 145–166 and [[src/Server/DbAgent.fs]] lines 89–111. Shared Op application checks node existence, old values and Children spans, document accessibility, placement, reserved names, and name conflicts. After shape Ops, History validates ownership semantics and rejects missing or multiple Owned occurrences, broken owner chains, illegal File or Directory placement, and duplicate artifact names at [[src/Shared/History.fs]] lines 374–562 and 582–670. None of these checks creates corrective Ops.

The Server also validates file path moves, destination existence, and ignored destinations before persistence at [[src/Server/DocumentPersistence.fs]] lines 378–401 and [[src/Server/IgnoredDestination.fs]] lines 52–114. These Server-only filesystem facts can reject a Change. They do not enrich it.

This distinction applies to a permanent Delete that removes an Owned occurrence while an unseen Ref remains. Current source has only Browser-side promotion planning: ViewModelDeleteOps classifies from the Browser Graph and emits an explicit Replace that promotes a known Ref at [[src/Shared/ViewModelDeleteOps.fs]] lines 55–78 and 126–143. If no Ref is resident, it can plan hard delete; the fully resident Server can reject the resulting missing-Owner Graph. There is no Server path that discovers the unseen Ref and appends a promotion Op.

## Server-generated Changes from separate requests

POST /ambit/file/parse reads the canonical Graph and server file text, plans a fresh Op list, creates a fresh Change identity, and submits it through postGraphOnlyChange at [[src/Server/Api.fs]] lines 282–355. DocumentPersistence.planParseFile can include a planned SetUpdateTime from the already-existing file mtime at [[src/Server/DocumentPersistence.fs]] lines 228–280. That SetUpdateTime is part of the new Server-generated Change from its creation; it is not appended to a Browser Change.

Directory and git reconciliation read canonical Graph plus discovered server artifacts, plan report.ops, create a fresh Change, and post it graph-only at [[src/Server/LazyLoadReconciliationServer.fs]] lines 148–190. The directory routes are at lines 281–349, and git push invokes the same reconciliation seam from [[src/Server/GitGateway.fs]] lines 149–186. One such request emits at most one Change, but a Load workflow can invoke Parse and reconciliation separately and therefore produce several remote Changes.

Graph-only posts skip file persistence and the post-persist stamp augmentation. Their complete generated Changes enter ChangeLog and later reach the Browser through Poll or Load. Their request responses return only command success or reconciliation failures, not an ACK that confirms a matching optimistic Browser Change.

## Implicit Graph effects that are not appended Ops

Shared setters and Replace call NodeUpdateTime.touch while applying their stated Ops, for example [[src/Shared/GraphMutate.fs]] lines 40–82, 84–169, and 171–236. Graph reconstruction also recomputes occurrence indexes and applies owner fields from explicit Owned edges at [[src/Shared/GraphBuild.fs]] lines 253–290. These effects happen as part of applying an Op and are not additional Ops in the Change. The Browser runs the same Shared transformations on its resident projection. Only the final disk-mtime correction described above is converted into appended Ops.

DatabaseProjection similarly derives normalized database rows from the final Graph and the touched Op ids at [[src/Server/DatabaseProjection.fs]] lines 90–131. Document persistence derives file writes from the final Graph. These are persistence projections, not Change enrichment.

## Historical unseen-Ref promotion

The unseen-Ref promotion premise is historical planned behavior, not current or prior Server source behavior. [[issues/08-define-move-edit-dependencies.md]] lines 13–19 and [[issues/13-finalize-permanent-delete-undo.md]] lines 9–15 proposed that the Server would expand a bare permanent Delete by promoting an unseen Ref. Commit ec17f93 (2026-08-04, selective loading spec/tickets) retained those tickets as deliberation while making [[spec.md]] the sole current decision; the current supersession is explicit at [[spec.md]] lines 71–74.

Commit 4255c48 (2026-08-04, implementing new undo plan ready for partially downloaded graph) implemented Server Undo/Redo materialization and kept delete behavior as ordinary explicit Ops. A history search for unseen, omitted promotion, bare permanent, or Server Ref-promotion code under src found no commit. Client-side LocalDeleteWithPromotion predates selective loading and remains explicit in the submitted Change. The old ticket behavior must not be cited as current ACK enrichment.

Persistence stamps were introduced separately by commit fd6701a (2026-07-23, relay file update time to client for display). That commit added PersistStamp.opsBetween, appendToLast, ACK stampOps, and the Server agent wiring. It is the only historical implementation found that appends Server-produced Ops to accepted or materialized Changes.

## Implications for Change-only Undo

1. Complete confirmed Changes are required only for ordinary submitted Changes whose durable ChangeLog entry receives persistence SetUpdateTime Ops. This includes a normal Change and, in the destination design, Browser-built inverse Changes for Undo and Redo when any of them causes Document persistence. The confirmation must identify the specific complete Change because current aggregate stampOps loses batch ownership, and a duplicate retry must return the already-persisted complete payload.
2. The submitted-prefix invariant is supported by the one actual augmentation path: PersistStamp appends to the selected Change. It is not evidence about current ChangeRequest.Undo or Redo, which submit no Ops. After the destination removes those request cases, an inverse ordinary Change can use the same prefix rule.
3. Current batching assigns all persistence extras to the final logged Change, including stamps caused by earlier Changes. The Change-only design must either preserve that durable assignment and invert it as part of that final History record, or change persistence to assign stamps per Change. It must not silently attribute aggregate ACK stamps to each causing command.
4. Current explicit Undo/Redo materialization must not be described as ACK enrichment. It is a Server-generated Change body from canonical History and disappears when the wire accepts only ordinary Changes.
5. Parse and directory-reconciliation Changes must not be described as ACK enrichment. They remain remote ChangeLog Changes delivered by Poll or Load and clear local History under the existing synchronization rule.
6. Full-Graph validation, filesystem validation, implicit update-time touches, owner/index rebuilding, database projection, and the superseded unseen-Ref promotion plan do not require complete enriched ACK Changes. Validation rejects; implicit effects are not Ops; unseen-Ref promotion does not exist.
7. A write-free unchanged request currently has no persisted Change, so it cannot return an exact ChangeLog payload. The ordered-confirmation spec needs an explicit unchanged-request rule. A duplicate changed request is different: its durable complete Change exists and should be returned.
