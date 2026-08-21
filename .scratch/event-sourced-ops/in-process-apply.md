# In-process apply vs `/changes`

Grill Q2: **don't-care** — inner apply (objects) vs Parse-style JSON into `postGraphOnlyChange` is not a pin. Either is fine. Stage still `charting`.

## Pipeline today

1. [[src/Server/RouteRegistration.fs]] `MapPost("/ambit/changes")` — auth, read body string.
2. [[src/Server/Api.fs]] `postChange` — `handle.postChange body` → 200 JSON or error.
3. FileAgent / DbAgent mailbox `PostChange` → `handlePostChange` ([[src/Server/FileAgent.fs]], same shape in [[src/Server/DbAgent.fs]]).
4. `Serialization.decodeChangeBatch` then `applyBatch`.
5. `applyBatch` — `changeId` dedup, revision gate, `History.applyChange` ([[src/Shared/History.fs]]), bump revision.
6. Disk validate / persist (unless `graphOnly`), `PersistStamp` `SetUpdateTime`, log, `encodeChangeAckJson` (confirmation echo).

The user's assumption holds: HTTP parses JSON, builds F# Changes, then apply.

Parse already skips HTTP: `Api.postParseFile` encodes Ops to a JSON string and calls `handle.postGraphOnlyChange`. Same `handlePostChange` (`graphOnly = true`). It ignores the ACK body (`Ok _` → `{"ok":true}`). Still pays JSON encode/decode.

## Is the handler separable?

HTTP (`Api.postChange`) is already thin. The glue is `handlePostChange`: JSON in, ACK echo out, stamps in the middle. `applyBatch` is nested in the mailbox — not a public function. `History.applyChange` is Shared apply only (no log, revision, stamps, mailbox).

`reconcileAck` is Browser-only ([[src/Shared/SyncLogic.fs]]). Not on this Server path.

## Two consumers

- **Browser POST:** needs HTTP 200 + Change list (unified success envelope; rewind+replay). Today's ACK is a stamp-only echo — dirt relative to that envelope.
- **In-process Server Actor:** needs apply + log so other Browsers **Poll**. Does not need an HTTP ACK. Parse already discards the ACK.

Do not POST-to-self. That re-enters auth/JSON/ACK for a caller that already has Change objects.

## Recommendation

**Same inner apply, not the HTTP handler, not POST-to-self, not a new public HTTP API.**

Cleaner seam (not built): take already-built `Change list`, run mailbox apply + persist + log as **newest Actor**, return the **produced sequence** (amended + fill-in + stamps). HTTP decodes JSON then calls that and wraps 200. The job calls that with objects and ignores or uses the sequence only for its own bookkeeping. Other Browsers Poll `getChangesSince`.

Tradeoff vs calling today's `handlePostChange` with encoded JSON (Parse style): works now, but keeps ACK-echo dirt and double encode. Fine as a temporary fact; not the clean seam.

Revision gate and confirmation ACK are apply-path facts to beat when Merge lands — same as Browser POST.

How the mailbox and request Tasks actually run: [[server-concurrency.md]].

## WORK.md mutations

Add this file to the Active [[project.md]] related list. Do not lock. No `add` / `move` / `block` / `remove`.
