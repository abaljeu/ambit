# Notices that trip the Browser

Read against origin/staging `ed8acaca`. Static reading only. Nothing was run.

## Finding

Notices are a main cause of DataOutdated. A Notice is a logged Event, so it moves the server event id. `SyncLogic.getPollOutcome` returns DataOutdated for any server id ahead of the Browser, Notice or not.

## Paths from a Notice to a block

- Typing: `Update.fs` PollDone `Some DataOutdated when events.IsEmpty || isAutoSyncBlocked`. A Poll that carries only Notices while the edit box differs from the Graph sets DataOutdated. DataOutdated stops Poll and post, so the block is permanent.
- Load: `SyncLogic.applyLoadResponse` drops Notices to find substantive Events. With none, and with Nodes in the answer, `responseEventId <> state.eventId` refuses the answer as "raced Load answer". A Notice in the reply makes those ids differ, so an expand after a parse or persist Notice is refused.
- Load while typing: `Update.fs` LoadDone `None when isAutoSyncBlocked` and `Some DataOutdated when ... isAutoSyncBlocked` set DataOutdated for any payload, Notices included.
- Notice write error: `SyncLogic.applyNoticeGraph` skips an absent Node, but an Error from `NoticeGraph.writeGraph` (for example `writePersisted` "node not found" or `setPersistState` on a non-artifact) fails `foldProjectedEvents`, and the Poll sets DataOutdated. No server path that sends such a Notice was found.

## Paths that are already safe

- A Notice on a Node the Browser does not have is skipped (`applyNoticeGraph`).
- A post reply drops Notices before it matches confirmations (`SyncLogic.dropNotices` in `identityError` and `collectSuffixes`).
- During Uploading, PollDone applies or skips. It does not block.

## How common

Each Change inside a File Node Body makes the Server log Notices. `CoreEventDispatch.markForPersist` logs NodeUnpersisted. `PersistThread.runRequest` logs NodePersisting, then NodePersisted or PersistFailed. No delay was found before the persist run. So an outliner who commits a line in a File Node and types the next line in the next seconds gets a Notice-only Poll while the edit box is dirty. ParseThread and LazyLoadReconciliationServer add NodeParsing, NodeParsed, and NodeUnparsed when a File Node opens or is reconciled.

## What ignoring a Notice loses

Notices write only document state: parse state, persist state, persisted version, and Update Time (`NoticeGraph.writeGraph`). NodeParsing, NodePersisting, ParseFailed, and PersistFailed do not write the Graph. PersistFailed also shows its message (`SyncLogic.persistFailedMessage`). A Notice never writes text or Children, so it never conflicts with an edit. A skipped Notice leaves the Bullet and tip status stale until the next Notice for that Node. Applying the Notice without the block keeps the status and is equally safe.
