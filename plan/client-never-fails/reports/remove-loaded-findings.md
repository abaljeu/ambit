# Remove Loaded and Unloaded: findings

Read against origin/staging `ed8acaca`. Static reading only. Nothing was run.

## Server replies that send a Node without its child list

- `ResidentProjection.wantAnswer`: for each wanted parent it sends the parent child list and the child Headers. It sends no child list for a child. Each child arrives Unloaded. Poll and post replies use it through `Api.wantAnswerFromHandle`. Load uses it through `ResidentProjection.wantAnswerForTargets` (`Api.loadWantAnswer`, `ResidentProjection.captureLoadResponse`).
- `ResidentProjection.visibleClosureWantAnswer` and `visibleClosureGraph` (boot state, `BootstrapScope.RootClosure` in `Api` GetState): child lists only for reserved parents, Zoom ancestors, and Zoom. Each child of those arrives Unloaded.
- `Api.nodesForRequest` (Load and save command reply, `universalResponseFromHandle`): Headers only, no child lists.
- The Server never sends a child list without its parent Header in these paths. `wantAnswer` drops a parent edge when a child Header is missing on the Server.

## Browser places that rely on Loaded and Unloaded

- `ChildrenStatus` with `Loaded` and `Unloaded` (`Model.fs`), `GraphChildren.isLoaded`, `GraphChildren.status`, `GraphBuild.isLoaded`, `GraphBuild.childrenStatus`, `Graph.isLoaded`, `Graph.childrenStatus`.
- Wire field `childrenStatus` with values `"loaded"` and `"unloaded"` (`Serialization.fs`), and the edge comments in `ApiResponses.fs`.
- `Want.compose`: wants each walked id that is not Loaded.
- `ResidentProjection.applyOpAllowing`: applies `Op.Replace` only when the parent is Loaded.
- `ResidentProjection.installWantAnswer`: refuses an edge with a child that has no Header ("dangling edge").
- `UpdateHelpers.loadTargetIntent`: sets `includeWorkspace` only for an Unloaded target.
- `ViewModelChildrenIndicator.rowChildrenIndicator`: hollow circle for a missing child list. `ViewModelRowState.residencyText`: tip text "Loaded" or "Unloaded".
- `ExprWalk`: an Unloaded Node yields no Answers.
- `History.fs` and `GraphMutate.fs` Replace checks, `WorkspaceUploadStructure.fs`.
- `GraphBuild.addDetachedNode` already gives a new Node an empty child list, so `Op.NewNode` already makes a Node with both.

## What changes

- Each Resident Node has its child list. The hollow circle for Unloaded goes. The tip line for children goes.
- The ids in a child list that have no Header are Absent. They show as placeholders. Want asks for Absent ids, not for Nodes without a child list.
- `installWantAnswer` accepts an edge with Absent children. The "dangling edge" refusal goes.
- The Replace check on a Loaded parent becomes a check on a Resident parent.
- `loadTargetIntent` and `includeWorkspace` need a new trigger, because no Resident target is Unloaded.
- `ExprWalk` sees every Resident Node's Children. An Absent child is a miss.

## Cost

A child list is ids only. Each Node in a reply adds one id per child, and no grandchild data. A collapsed row for a Node with many children (for example a large File Node Body) sends all of those ids. Boot sends child lists for every child of the Zoom path, one level more than today.
