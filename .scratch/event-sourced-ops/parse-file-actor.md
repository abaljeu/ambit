# Parse File as the first Server-side Actor

Observation. Not a refactor plan. Not locked. Vocab already names Parse as this Actor.

## Today

1. Client `parseFileOp` → `ContinueParseFile` → `runParseFile` POST `/ambit/file/parse`.
2. `Api.postParseFile` (HTTP Task): `getState` (mailbox) → `DocumentPersistence.planParseFile` **off** mailbox → one `Change` (`encodeGraphOnlyChange`, `id` = snapshot revision) → `postGraphOnlyChange` (`PostAndAsyncReply`).
3. `handlePostChange` `applyBatch`: revision **CAS**. Graph-only (no disk persist). ACK JSON **ignored**; HTTP `{"ok":true}`.
4. Client `completeParseFilePost` waits that HTTP, then `tryStartLoadFetch` (Poll or Load). Not rewind+replay of a Change list.

No job id. No cancel. Request holds plan **and** apply.

## Already fits

- Actor that produces a Change (not a graph dump).
- Plans off the file mailbox; apply is a **message** into `FileAgent.mailbox`.
- One Change of Ops; agent applies one-at-a-time.
- Requester consumes via Poll/Load, not a completion push.

## Must realign (Merge / consume)

- **CAS → amend.** Browser Changes between `getState` and apply: today `Revision mismatch`. Framework: newest Actor, amend after other accepted Changes, 200 + Change list.
- **ACK.** `{"ok":true}` is not the unified envelope. Inner apply should return the produced sequence ([[in-process-apply.md]]).
- **Client.** Requester did not apply parse locally — Poll/Load of the tail is enough. Other optimistic Browsers: rewind+replay. Not `SetText`-style local rewrite.

## Not required to invent now

Job id, launch-return-before-apply, cancel — intended for N long jobs ([[job-launch-apply-cancel.md]]). Parse is still one request-scoped Task. Soft-lock **meaning** (advisory checkout of the File subtree): [[soft-lock.md]]. Fill-in, JSON dirt: known, not extra Parse work. A later instance that **does** need ids/cancel: [[shell-command-actor.md]].

## WORK.md

Add this file to the Active [[project.md]] related list. Stage `charting`.
