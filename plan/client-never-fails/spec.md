# Client never fails on server data

Map: [[map.md]]

## 1. User stories

1. Accept Notices — As an outliner, I want the Browser to apply each Notice and take its event id when it has no pending changes, and a post reply to apply its Notices too, so that parse and persist status never stops Sync while I type or expand.
2. Never reload for server data — As an outliner, I want the Browser to apply every server reply, so that I never see "Data changed on server — click to reload".
3. See a moved Node — As an outliner with a section open, I want a Node that another tab moves into that section to show as a placeholder Bullet drawn in color #444444, so that my view stays live.
4. Use a placeholder — As an outliner, I want to expand, select, and run every Command except edit on a placeholder Bullet, so that it acts like any row.
5. Fill a placeholder — As an outliner, I want the Browser to draw an Absent child as a normal row when its Node arrives, so that the placeholder fills in by itself.
6. Poll with pending changes — As an outliner with changes not yet posted, I want a Poll to apply, and my changes to post after it, so that Sync continues.
7. Merge in progress — As an outliner with more changes pending, I want the Browser to ignore a post reply with different changes, post the rest, and wait for a clean merge, so that Sync continues without a reload.
8. Type while others change — As an outliner in edit mode, I want my edit to post after a one-second pause in typing, with edit mode on, so that server changes apply while I type.
9. Edited Node removed — As an outliner in edit mode, I want the Browser to leave edit mode when a Poll removes the Node I edit, and then render the view as usual, so that the view stays true to the Server.
10. Edit during a Load — As an outliner, I want to edit while a Load is in flight, so that an expand never blocks my edit.
11. Load without a race — As an outliner, I want a Load reply to apply like any server reply, so that an expand after a parse or persist Notice works.
12. Survive a Server crash — As an outliner, I want the Server to write each Event first and then its Graph change, so that a restart never reports an event id with no Events behind it.
13. Remove Loaded and Unloaded — As a developer, I want each state return for a NodeId to include both its Header and its child list, so that a Resident Node always has its child list and the Browser needs no Loaded or Unloaded state. A child list holds ids only. The removal covers:
    - `ResidentProjection.wantAnswer` (Poll, post, and Load replies) and `ResidentProjection.visibleClosureWantAnswer` (boot): send each returned Node's child list
    - `Api.nodesForRequest`: send each Node's child list
    - type `ChildrenStatus` and its cases `Loaded` and `Unloaded` in `src/Shared/Model.fs`
    - `GraphChildren.isLoaded`, `GraphChildren.status`, `GraphBuild.isLoaded`, `GraphBuild.childrenStatus`, `Graph.isLoaded`, and `Graph.childrenStatus`
    - the wire field `childrenStatus` and its values `"loaded"` and `"unloaded"` in `src/Shared/Serialization.fs`
    - `ResidentProjection.installWantAnswer`: accept an edge with Absent children, without the "dangling edge" refusal
    - the children line in `ViewModelRowState.residencyText`
    - the glossary entries Loaded and Unloaded in [[GLOSSARY.md]], removed. Resident and Absent keep their meaning.
14. Poll for nearby Absent Nodes — As an outliner, I want the active process that looks for nearby Nodes to look for Absent ids, and Poll to bring their Nodes, so that placeholders near my view fill in. Today `Want.compose` looks for nearby Unloaded Nodes. Poll is the one mechanism that gets data.
15. Delete the whole-Workspace request — As a developer, I want each hook for a whole-Workspace request unhooked and deleted, so that Poll is the one mechanism that gets data. The hooks are:
    - the field `includeWorkspace` of `LoadTarget` in `src/Shared/ApiResponses.fs`
    - its encode and decode in `src/Shared/ApiResponseSerialization.fs`
    - `UpdateHelpers.loadTargetIntent`, which sets it
    - the `includeWorkspace` filter in `ResidentProjection.wantAnswerForTargets`
16. Know about a new version — As an outliner, I want "New version available" to stay, so that I reload when the protocol changes.

Out of scope: the hollow circle is repurposed to mark Absent Nodes. A separate ticket does that.
