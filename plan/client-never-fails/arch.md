# Client never fails on server data architecture

Spec: [[spec.md]]

## 1. Story paths

A failure on these paths is `Error` of a string.

Alan step 1, Notices.

1. **Accept Notices**
   1. **Notice apply** — With an empty pending queue, each Notice is applied and its event id is stored.
   2. **Notice apply** — `NoticeGraph.writeGraph` writes the Notice. An `Error` leaves the Graph as it is and the event id is still stored.
   3. **Notice apply** — A post reply applies its Notices the same way, then matches confirmations.
   4. **Notice apply** — `Update` PollDone leaves Sync `Idle` when the pending queue is empty, including when the edit box holds new text.

Alan step 2, Event then Graph.

2. **Survive a Server crash**
   1. **Event then Graph** — The in-memory apply runs first, so the stored Event carries its confirmed ops.
   2. **Event then Graph** — `CoreEventDispatch.persistNew` appends that Event, then the filler writes the durable Graph.
   3. **Event then Graph** — `DbAgent` writes the projection after the append. `FileAgent` writes its durable Graph after the append.
   4. **Event then Graph** — A Graph write that returns `Error` of a string leaves the stored Event in the log.
   5. **Event then Graph** — Each reported event id has Events in the log. The Browser applies those Events.

Alan step 3, the placeholder. Loaded and Unloaded stay in place. The Graph accepts a child id with no Header so the row can draw.

3. **See a moved Node**
   1. **Resident projection** — `installWantAnswer` returns `Ok` when a child id has no Header.
   2. **Graph mutate** — `replace` returns `Ok` for a new child id with no Header.
   3. **Row plan** — `planPatchDOM` reads a Header with `Map.tryFind`. A missing Header is a value.
   4. **Site map** — A child id with no Header becomes a row.
   5. **Row view** — That row's Bullet is drawn in color #444444.

4. **Use a placeholder**
   1. **Row state** — The row selects.
   2. **Edit start** — Expand leaves the row expanded. The id has no child list, so the current Want includes it on the next Poll.
   3. **Edit start** — Every Command runs except edit. `startEditOp` leaves the mode as it is when the Header is absent.

5. **Fill a placeholder**
   1. **Resident projection** — `installWantAnswer` adds the Node when its Header arrives.
   2. **Row plan** — The next plan draws that row as a normal row.

Alan step 4, each Node with its child list. This removes Loaded and Unloaded. `ChildrenStatus` stays until the contract steps, so each earlier step still builds.

6. **Remove Loaded and Unloaded**
   1. **Resident projection** — `wantAnswer` and `visibleClosureWantAnswer` send each returned Node's Header and its child list. The list is ids only. A child id with no Header stays in the list.
   2. **Command response** — `Api.nodesForRequest` sends each Node's Header and its child list. `UniversalResponse` carries that list. Encode and decode carry it.
   3. **Resident projection** — `applyOpAllowing` applies `Op.Replace` when the parent Header is present.
   4. **History** — A parent change checks the parent Header.
   5. **Upload structure** — A workspace change checks the workspace Header.
   6. **Children indicator** — A present child list draws a chevron when it has ids, and a solid circle when it is empty. Unparsed stays slashed.
   7. **Row state** — `residencyText` keeps the document-state line. The children line is deleted.
   8. **Node wire** — The field `childrenStatus` and the values `loaded` and `unloaded` are deleted.
   9. **Want** — The `isLoaded` call is deleted. Until the Absent fetch path, the filter is an id with no child list.
   10. **Browser sync helpers** — The `isLoaded` call in `loadTargetIntent` is deleted. `includeWorkspace` stays false on every target until the delete path.
   11. **Graph model** — After the callers above are gone, delete `ChildrenStatus`, `GraphChildren.isLoaded`, `GraphChildren.status`, `GraphBuild.isLoaded`, `GraphBuild.childrenStatus`, `Graph.isLoaded`, and `Graph.childrenStatus`.
   12. **Graph model** — Test updates for that delete are batches of at most four tests, in the test list on the Graph model module. The type delete is the last batch of this path.

Alan step 5, Absent fetch. This includes the whole-Workspace request and the raced-Load guard.

7. **Poll for nearby Absent Nodes**
   1. **Want** — `compose` keeps ids whose Header is absent.
   2. **Want** — The included walk and the two child ranks already list those ids.
   3. **Want** — Expand puts a Node's child ids on that walk. The next Poll brings the missing Nodes.

8. **Delete the whole-Workspace request**
   1. **Command response** — `LoadTarget` loses `includeWorkspace`. Encode and decode lose the field.
   2. **Browser sync helpers** — `loadTargetIntent` builds a target id only.
   3. **Resident projection** — `wantAnswerForTargets` answers the target ids. The `includeWorkspace` filter is deleted.
   4. **Resident projection** — `LoadRefuse.MultiWorkspace` stays.

9. **Load without a race**
   1. **Notice apply** — `applyLoadResponse` applies the reply as any other server reply.
   2. **Notice apply** — The raced Load answer guard is deleted.
   3. **Notice apply** — `Update` LoadDone uses that apply.

Alan step 6, protocol, then the one-second typing pause.

10. **Poll with pending changes**
    1. **Notice apply** — A post reply undoes the posted changes and replays the server Events. A Poll has an empty undo list, so the server Events apply.
    2. **Sync planner** — `tryStartPoll` emits `PollServer` while pending changes wait.
    3. **Sync planner** — Those pending changes post after the Poll applies.

11. **Merge in progress**
    1. **Notice apply** — A reply that carries our own changes is accepted.
    2. **Notice apply** — A reply with different changes, while more changes are pending, is `AckReconcile.Ignored`.
    3. **Notice apply** — The Browser posts the rest and waits for a reply that matches.

12. **Type while others change**
    1. **Row view** — Each key in the edit box re-arms a one-second timer.
    2. **Edit timer** — `armOrRearm` holds that timer.
    3. **Browser sync helpers** — When the timer fires, `commitTextEdit` posts the text and the mode stays `Editing`.

13. **Edited Node removed**
    1. **Browser sync helpers** — `adjustModeAfterServerApply` sets the mode to `Selecting` when the edited NodeId has no Header.
    2. **Row plan** — The view draws from the Graph.

14. **Edit during a Load**
    1. **Edit timer** — A Load in flight leaves the timer and the edit free to run.
    2. **Notice apply** — The Load reply uses the Load reply path above.

Alan step 7, the reload prompt, last.

15. **Never reload for server data**
    1. **Reload prompt** — The status line's `DataOutdated` arm is deleted, including the sentence `Data changed on server — click to reload`.
    2. **Reload prompt** — The blocking alert's `DataOutdated` arm is deleted, including the title `View is out of date`.
    3. **Reload prompt** — `ServerRejected` stays.

16. **Know about a new version**
    1. **Reload prompt** — `CodeOutdated` keeps the title `New version available` and the status sentence `New version available — click to reload`.

## 2. Module map

1. **Notice apply**
   1. [[src/Shared/SyncLogic.fs]] — Notice apply, post-reply match, Load reply apply, undo and replay.
   2. [[src/Shared/NoticeGraph.fs]] — `writeGraph`.
   3. [[src/Client/Update.fs]] — PollDone and LoadDone.

2. **Event then Graph**
   1. [[src/Server/Core/CoreEventDispatch.fs]] — `persistNew`.
   2. [[src/Server/Core/DbAgent.fs]] — projection write after the Event append.
   3. [[src/Server/Core/FileAgent.fs]] — durable Graph write after the Event append.

3. **Resident projection**
   1. [[src/Shared/ResidentProjection.fs]] — `wantAnswer`, `visibleClosureWantAnswer`, `installWantAnswer`, `applyOpAllowing`, `wantAnswerForTargets`, `LoadRefuse.MultiWorkspace`.

4. **Command response**
   1. [[src/Server/Api.fs]] — `nodesForRequest`.
   2. [[src/Shared/ApiResponses.fs]] — `UniversalResponse`, `LoadTarget`.
   3. [[src/Shared/ApiResponseSerialization.fs]] — encode and decode for those records.

5. **Graph mutate**
   1. [[src/Shared/GraphMutate.fs]] — `replace`.

6. **Site map**
   1. [[src/Shared/ViewModelSiteMap.fs]] — child rows.

7. **Row plan**
   1. [[src/Shared/ViewModelDomPlan.fs]] — `planPatchDOM`.

8. **History**
   1. [[src/Shared/History.fs]] — parent change check.

9. **Upload structure**
   1. [[src/Shared/WorkspaceUploadStructure.fs]] — workspace change check.

10. **Children indicator**
    1. [[src/Shared/ViewModelChildrenIndicator.fs]] — `rowChildrenIndicator`.

11. **Row state**
    1. [[src/Shared/ViewModelRowState.fs]] — `residencyText`, selection.

12. **Node wire**
    1. [[src/Shared/Serialization.fs]] — node `childrenStatus`.

13. **Want**
    1. [[src/Shared/Want.fs]] — `compose`.

14. **Browser sync helpers**
    1. [[src/Client/UpdateHelpers.fs]] — `loadTargetIntent`, `commitTextEdit`, `adjustModeAfterServerApply`.

15. **Graph model**
    1. [[src/Shared/Model.fs]] — `ChildrenStatus`, `GraphChildren`.
    2. [[src/Shared/GraphBuild.fs]] — `isLoaded`, `childrenStatus`.
    3. [[src/Shared/GraphOps.fs]] — `Graph.isLoaded`, `Graph.childrenStatus`.
    4. Tests, in batch order, at most four tests a ticket:
       1. [[tests/Shared.Tests/ModelTests.fs]]
       2. [[tests/Shared.Tests/SerializationTests.fs]]
       3. [[tests/Shared.Tests/GraphProjectionTests.fs]]
       4. [[tests/Shared.Tests/WantTests.fs]]
       5. [[tests/Shared.Tests/BootstrapScopeTests.fs]]
       6. [[tests/Shared.Tests/BootCachePollTests.fs]]
       7. [[tests/Shared.Tests/SyncLogicTests.fs]]
       8. [[tests/Shared.Tests/HistoryTests.fs]]
       9. [[tests/Shared.Tests/LargeChangeApplyTests.fs]]
       10. [[tests/Shared.Tests/ClientHistoryRuntimeTests.fs]]
       11. [[tests/Shared.Tests/WorkspaceUploadStructureTests.fs]]
       12. [[tests/Shared.Tests/ViewModelRowStateTests.fs]]
       13. [[tests/Shared.Tests/ExprCombinatorTests.fs]]
       14. [[tests/Shared.Tests/ExprEvalTests.fs]]
       15. [[tests/Shared.Tests/ExprPipelineTests.fs]]
       16. [[tests/Server.Tests/ApiGetStateTests.fs]]
       17. [[tests/Server.Tests/StateEndpointTests.fs]]

16. **Sync planner**
    1. [[src/Shared/SyncPlanner.fs]] — `tryStartPoll`.

17. **Edit timer**
    1. [[src/Client/AutoDownloadTimer.fs]] — `armOrRearm`.

18. **Row view**
    1. [[src/Client/RowView.fs]] — Bullet color, edit key timer.

19. **Edit start**
    1. [[src/Client/UpdateOps.fs]] — `startEditOp`.
    2. [[src/Client/Commands.fs]] — Command dispatch.

20. **Reload prompt**
    1. [[src/Client/StatusView.fs]] — status line.
    2. [[src/Client/Overlays.fs]] — blocking alert.

## 3. Slicing strategy

Direction: `tracer-cut`.

[[plan/context/slicing-work2.md]] cuts the tickets in the story-path order above. [[plan/context/size-limit.md]] bounds each ticket: 8 source files or fewer, and 4 tests added or changed or fewer.

The order is Alan's seven steps:

1. **Notices** — path **Accept Notices**. Blockers: none.
2. **Event then Graph** — path **Survive a Server crash**. Blocked by **Accept Notices**.
3. **Placeholder** — path **See a moved Node**, then **Use a placeholder**, then **Fill a placeholder**. **Use a placeholder** and **Fill a placeholder** are also blocked by **See a moved Node**.
4. **Child list on each Node** — path **Remove Loaded and Unloaded**. Blocked by **Fill a placeholder**. Each numbered step of that path is its own ticket, and each of those tickets is blocked by the step before it. Callers move while `ChildrenStatus` still builds. The **Graph model** contract is the last tickets of this step, four tests at a time, type delete last.
5. **Absent fetch** — path **Poll for nearby Absent Nodes**, then **Delete the whole-Workspace request**, then **Load without a race**. Blocked by the last **Remove Loaded and Unloaded** ticket. Each of these paths is also blocked by the path before it in this step.
6. **Protocol, then the typing pause** — path **Poll with pending changes**, then **Merge in progress**, then **Type while others change**, then **Edited Node removed**, then **Edit during a Load**. Blocked by **Load without a race**. Each of these paths is also blocked by the path before it in this step.
7. **Reload prompt** — paths **Never reload for server data** and **Know about a new version** are one ticket. Blocked by **Edit during a Load**. One view change deletes the data-changed arms and keeps the new-version sentences.

`tracer-cut` is the direction because each step is one verifiable path, and the next path starts when that path is done. Step 4 is the wide symbol change, so its numbered steps are the tickets and they stay inside the size limit. Those steps add the child lists beside `ChildrenStatus`, move callers, then delete the type. The other six steps stay behavior paths. Several paths share one file, so a cut by module would cross this order.

## 4. Glossary proposals

[[GLOSSARY.md]] stays as it is. Proposed edits:

1. **Loaded** — delete the entry.
2. **Unloaded** — delete the entry.
3. **Resident** — A Node whose Header is present in this Graph, with its child list. Antonym: **Absent**.
4. **Absent** — add an entry. A NodeId that is not Resident. An Absent child is a child id whose Header is not in this Graph. The Browser draws it as a placeholder Bullet. Antonym: **Resident**.
