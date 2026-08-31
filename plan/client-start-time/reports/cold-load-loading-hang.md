# Cold load stuck on Loading...

Date: 2026-08-27
Branch: `w/broken`
Parent: [[implement-cache-first-boot-01-07.md]], [[cache-first-boot-via-poll.md]]

## Root cause

No rewind is required. Cold boot gated `GET /state` on the IndexedDB read callback. A miss that never called back left the HTML `Loading...` text forever.

The read opened one readonly transaction on both `snapshots` and `changes`, then used an index cursor on `changes`. On a first visit the Change store is empty. If that cursor path never completed, `/state` never started.

A second stall: after `/state` decode, the boot path ran snapshot persist and `graphFingerprint` on the Graph **before** first paint. That is off the miss path, but it can delay the first render on a successful `/state`.

## Fix (in tree, Fable rebuilt)

1. Read the snapshot store first. Only if a snapshot exists, open a second transaction for the Change log. `onblocked` and a settle flag treat errors as miss.
2. Shared `decideBootReadWait`: if IndexedDB has not returned by 2500 ms, fetch `/state` (`timeout`).
3. Paint `StateLoaded` first. Persist the snapshot and fingerprint on a `setTimeout 0` after paint.
4. Poll fallback to `/state` no longer waits on `deleteCache` before the fetch.

## Tests

Focused `BootCacheTests` including `decideBootReadWait` (miss, timeout, keep waiting). Worker ran these green. Full suite not run.

## HITL still required

Hard-reload a cold document (empty `gambol-boot-cache-v1`). Confirm the Graph paints, not `Loading...`. Console should show `cache miss → /state` or `cache timeout → /state`, then `decodeStateResponse`.

## WORK.md mutations (parent)

- Keep HITL pending; point it at this report as well as [[implement-cache-first-boot-01-07.md]].
