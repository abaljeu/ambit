# Issue 41 — Singular postEvent (client batch vs mailbox queue)

Date: 2026-09-15. Ticket: [[../issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]]. Follows [[issue-41-postchange-postevent-fix.md]] and Alan’s design lock (same day).

## Design lock

The client may accumulate a **batch of Changes for transport efficiency**. The mailbox must remain a **simple queue**.

At the adapter / Core door boundary: **loop** the received Changes and enqueue **one Event per Change** (one `postEvent` / one `CoreMsg.PostEvent` each). Do **not** batch multiple Events inside `CoreEventDispatch` or inside a single mailbox message.

| Layer | Shape | Rule |
| --- | --- | --- |
| Wire / HTTP ChangeBatch | `Change list` OK | Transport convenience until [[../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md\|43]] |
| `CoreMailbox.postChange` / `CoreChanges.postChange` | May take `Change list` | Loops; each item → one `PostEvent` |
| `CoreMsg.PostEvent` | Singular `event: Event` | One message, one Event |
| `CoreEventDispatch.postEvent` | Singular `event: Event` | No `postMany` / Event list |
| PersistHandlers (until [[../issues/42-migrate-persisthandlers-restore-and-geteventssince.md\|42]]) | `Change list` | Wrap one Event’s ops as a **one-element** list |

## What changed

1. **Removed** `CoreEventDispatch.postMany`. Only `postEvent` remains; prepare / persist / store take one `Event`.
2. **Removed** `CoreMsg.PostChange`. Credentialed Graph Changes go through `PostEvent` only (no multi-Event CoreMsg).
3. **`CoreMailbox.postChange`** accepts a transport `Change list`, builds one Event per Change, and posts each via `PostEvent`. Empty list returns `Error "changes must not be empty"` at the door (does not invent a batch message). Accepted replies merge across the loop.
4. **`postGraphOnlyChange`** stays **singular `Change`** (chunk posts already one Change; skips EventLog). Persist still receives `[ change ]`.
5. **Authority stamp**, name-only Undo/Redo fill, ActorStart/ActorStop append, unchanged-submission via empty Ops → persist, EventLog append — preserved on the singular `postEvent` path.
6. **Api.postChange** still decodes ChangeBatch and calls `handle.postChange batch.changes`; the loop into the mailbox is on CoreMailbox.

## Out of scope (unchanged)

HTTP Event Poll / wire Event (43). PersistHandlers Event restore (42). Contract deletes (45). Multi-Change **persist** batch semantics stay on File/Db `postChange: Change list` until 42; Core no longer drives multi-Change through one Event dispatch.

## Tests

Focused Server.Tests: Issue41CoreMailboxTests, CoreMailboxDoorTests, CoreMsgActorCasesTests, ActorCoreChangesDoorTests, CredentialedChangePostsTests (and related compile fixes for list-at-door call sites).
