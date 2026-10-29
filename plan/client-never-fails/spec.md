# Client never fails on server data

Map: [[map.md]]

## 1. User stories

1. Accept Notices — As an outliner with no pending changes, I want the Browser to apply each Notice and take its event id, so that parse and persist status never stops Sync while I type or expand.
2. Never reload for server data — As an outliner, I want the Browser to apply every server reply, so that I never see "Data changed on server — click to reload".
3. See a moved Node — As an outliner with a section open, I want a Node that another tab moves into that section to show as a placeholder Bullet, so that my view stays live.
4. Use a placeholder — As an outliner, I want to expand, select, and run every Command except edit on a placeholder Bullet, so that it acts like an Unloaded Node.
5. Fill a placeholder — As an outliner, I want the Browser to request a Node that is in a child list but not Resident, and draw it as a normal row when it arrives, so that the placeholder fills in by itself.
6. Poll with pending changes — As an outliner with changes not yet posted, I want a Poll to apply, and my changes to post after it, so that Sync continues.
7. Merge in progress — As an outliner with more changes pending, I want a post reply with different changes to count as a merge in progress, so that the Browser continues to post and reaches a clean merge.
8. Type while others change — As an outliner in edit mode, I want my edit to post after a pause in typing, with edit mode on, so that server changes apply while I type.
9. Edit during a Load — As an outliner, I want to edit while a Load is in flight, so that an expand never blocks my edit.
10. Load without a race — As an outliner, I want a Load reply to apply like any server reply, so that an expand after a parse or persist Notice works.
11. Survive a Server crash — As an outliner, I want the Server to write each Event before or with its Graph change, so that a restart never reports an event id with no Events behind it.
12. Know about a new version — As an outliner, I want "New version available" to stay, so that I reload when the protocol changes.
