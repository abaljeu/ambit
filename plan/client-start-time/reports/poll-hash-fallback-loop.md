# Poll hash fallback loop

Date: 2026-08-27
Branch: current `w/*`
Parent: [[../issues/08-poll-hash-fallback-loop.md]], [[cold-load-loading-hang.md]]

## Symptom

Boot Poll logged `poll fallback hash → /state` in a tight loop. Each `/state` dispatched `StateLoaded`, which sets Selection to None. Session restore puts back Zoom and folds, not Selection, so the UI jumped to ROOT again and again.

## Cause

After `/state`, a deferred `graphFingerprint` ran on the Fable Graph and was compared to the server Poll `bootstrapHash` (computed in .NET). The two hashes do not match. `decideBootPoll` then fell back to `/state`, which painted and Polls again.

The cold-load hang fix made this worse: persist and fingerprint moved to `setTimeout 0`, so the hash was often set while the confirmation Poll was still in flight.

## Fix

Shared `cachedHashForBootPoll`: after `/state`, the cached hash is None. Browser boot Poll uses that helper, does not store a client fingerprint, and persist writes an empty `bootstrapHash`. Cache hit also ignores a snapshot hash that may be a leftover Fable fingerprint.

Focused `BootCachePollTests`: 17 passed.

## HITL

Hard-reload. Console must not repeat `poll fallback hash → /state`. Selection must stay where the user left it. One `/state` on miss is still correct.
