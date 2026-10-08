# API

Category: Contract

See Also: [Server](server.md), [Mailbox](mailbox.md), [Browser](browser.md)

The API index links each published contract.

## API expansion

Contract rule for a route change and for an Event body change. The sequence is expand, then migrate, then contract. The same sequence is [core-refinement architecture](../../plan/core-refinement/arch.md) §3 and the Sequence on [Single event source architecture](../../plan/single-event-source/arch.md).

Expand means the new door or Event body ships beside the old one. A new capability expands the events door. That expansion is a new EventBody case in the ordered list. It is not a new POST route.

Migrate means the caller moves to the new form while the old form still exists. Connected Events stay in one ordered stream. No new side-channel route is added.

Contract means the old route is removed only after no caller uses it.

One Event source means one ordered stream. Connected Events are queued in order, posted in that order, and processed in that order. A later Event does not pass an earlier connected Event. A posted list is applied in order, as one unit; lists from different clients do not interleave. The only rejection is the credential check. That check refuses the whole list before anything applies. Otherwise every posted list applies whole.

1. [x] Events door — `POST /ambit/changes` and `POST /ambit/events` call `Api.postEvents`. Detail: [HTTP contract](http-contract.md).
2. [x] Command and Cancel routes are removed. Run and Cancel are events on the events door.
3. [x] Mailbox order — The one mailbox queue handles one message, then the next. A later message on that queue does not pass an earlier message. Detail: [Mailbox](mailbox.md).
4. [x] Posted list — `postEvents` pushes each event onto the mailbox queue back to back. Another client's list cannot enter inside that push.
5. [x] Credential only — The credential check refuses the whole list before anything applies. Client ActorStart and Cancel apply. On ActorStart, a refusal and a fail are the same stored pair (`unknown actor`, or the `Rejected` message). A client ActorStop is not a client event type.

## Parties

[x] Browser and Server: [HTTP contract](http-contract.md)
[x] Sync clients and Server: [Multi-client sync](sync-mvp.md)
[x] Core callers and the mailbox: [Mailbox](mailbox.md)
[x] Desktop host and local proxy: [Desktop local files](desktop-local-files.md)
[x] Workspace labels and local paths: [Workspace local mapping](workspace-local-mapping.md)
[x] Workspace Upload and Download: [Workspace file sync](workspace-file-sync.md)
[ ] Find and Move: [Search Actor](search-actor.md)
[ ] Query: [Query Actor](query-actor.md)
[x] Ambit and the external agent: [AI agent protocol](ai-agent-protocol.md)

## Operations

[x] HTTP routes and JSON: [HTTP contract](http-contract.md)
[x] Sync protocol: [Protocol](sync-mvp.md#protocol)
[x] Sync endpoints: [Endpoints](sync-mvp.md#endpoints)
[x] Mailbox door: [Interface](mailbox.md#interface)
[x] Mailbox messages: [Messages](mailbox.md#messages)
[x] Op cases: [Shape](op.md#shape)
[x] Change action: [Change](operations.md#change)
[x] Desktop capability discovery: [Capabilities](desktop-local-files.md#capabilities)
[x] Desktop file routes: [Endpoints](desktop-local-files.md#endpoints)
[x] WebDAV mount and sync HTTP: [Transport](workspace-file-sync.md#transport)
[x] Mapping document: [JSON](workspace-local-mapping.md#json)
[x] Mapping routes: [API](workspace-local-mapping.md#api)
[x] Workspace node JSON: [Serialization](graph.md#serialization)
[x] Agent wake and deliver: [AI agent protocol](ai-agent-protocol.md)
[ ] Search messages: [Messages](search-actor.md#messages)
[ ] Query messages: [Messages](query-actor.md#messages)
