# 09 — Cache-first boot delayed LCP to ~10 s

**Context:** After cache-first boot, LCP of `div.amb-text` was **10.81 s** on the minified `Program.bundle.js` path (not `?debug=1`). Before this attempt, the same bundle path was ~**1 s** (`/state` ~1.19 s). See [[../reports/production-hitl-after-deploy.md]].

**What to build:** First paint must start `GET /state` immediately, as before cache-first. Do not wait on IndexedDB read or decode. Equal-Revision Poll must not `getState` plus `graphFingerprint` the live Graph (Fable and .NET hashes already disagree; the walk blocks the agent mailbox).

**See also:** [[08-poll-hash-fallback-loop.md]], [[../reports/cold-load-loading-hang.md]], [[src/Client/Program.fs]], [[src/Server/Api.fs]]

**Status:** ready-for-agent

- [x] Browser boot always fetches `/state` without waiting for IndexedDB.
- [x] Poll omits `bootstrapHash` (no live Graph fingerprint on the agent).
- [ ] HITL on `/ambit` (bundle): LCP of `div.amb-text` back near ~1 s; Network shows `Program.bundle.js` then one `/state`.

## Comments

Cache-first read decoded the snapshot on the main thread before `/state` started. A large IndexedDB record plus JSON decode delayed `StateLoaded`. Server Poll at matching Revision also loaded the full Graph to hash it, which queued later `/state` on the same agent.
