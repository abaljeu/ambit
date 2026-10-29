# Client never fails on server data

Map: [[map.md]]

## 1. User stories

1. Accept Notices — As an outliner, I want the Browser to apply each Notice and take its event id when it has no pending changes, and a post reply to apply its Notices too, so that parse and persist status never stops Sync while I type or expand.
2. Never reload for server data — As an outliner, I want the Browser to apply every server reply, so that I never see "Data changed on server — click to reload".
3. See a moved Node — As an outliner with a section open, I want a Node that another tab moves into that section to show as a placeholder Bullet drawn in color #444444, so that my view stays live.
4. Use a placeholder — As an outliner, I want to expand, select, and run every Command except edit on a placeholder Bullet, so that it acts like an Absent Node.
5. Fill a placeholder — As an outliner, I want the Browser to request a Node that is in a child list but not in nodes, and draw it as a normal row when it arrives, so that the placeholder fills in by itself.
6. Poll with pending changes — As an outliner with changes not yet posted, I want a Poll to apply, and my changes to post after it, so that Sync continues.
7. Merge in progress — As an outliner with more changes pending, I want the Browser to ignore a post reply with different changes, post the rest, and wait for a clean merge, so that Sync continues without a reload.
8. Type while others change — As an outliner in edit mode, I want my edit to post after a one-second pause in typing, with edit mode on, so that server changes apply while I type.
9. Edited Node removed — As an outliner in edit mode, I want the Browser to leave edit mode when a Poll removes the Node I edit, and then render the view as usual, so that the view stays true to the Server.
10. Edit during a Load — As an outliner, I want to edit while a Load is in flight, so that an expand never blocks my edit.
11. Load without a race — As an outliner, I want a Load reply to apply like any server reply, so that an expand after a parse or persist Notice works.
12. Survive a Server crash — As an outliner, I want the Server to write each Event first and then its Graph change, so that a restart never reports an event id with no Events behind it.
13. Rename Loaded to Resident — As a developer, I want a Node with its Header and child list on the Browser named Resident, and a Node without its child list named Absent, so that the state is not confused with the Load process. The rename covers:
    - type `ChildrenStatus` in `src/Shared/Model.fs`: cases `Loaded` and `Unloaded` become `Resident` and `Absent`
    - `GraphChildren.isLoaded` and `GraphChildren.status` in `src/Shared/Model.fs`
    - `GraphBuild.isLoaded` and `GraphBuild.childrenStatus` in `src/Shared/GraphBuild.fs`
    - `Graph.isLoaded` and `Graph.childrenStatus` in `src/Shared/GraphOps.fs`
    - the callers of `isLoaded` in `Want.fs`, `History.fs`, `GraphMutate.fs`, `ResidentProjection.fs`, `WorkspaceUploadStructure.fs`, and `UpdateHelpers.fs`
    - the wire values `"loaded"` and `"unloaded"` of field `childrenStatus` in `src/Shared/Serialization.fs`
    - the Bullet tip text in `ViewModelRowState.residencyText`
    - the glossary entries Loaded, Unloaded, Resident, and Absent in [[GLOSSARY.md]]
14. Know about a new version — As an outliner, I want "New version available" to stay, so that I reload when the protocol changes.
