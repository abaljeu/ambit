# 08 — Poll hash fallback loops `/state` and resets Selection to ROOT

**Context:** After the cold-load hang fix ([[../reports/cold-load-loading-hang.md]]), the Browser refetches `/state` on every boot Poll. Each `StateLoaded` sets `selectedNodes` to None. Session restore does not restore Selection, so the UI jumps to ROOT without stop.

**What to build:** After a fresh `/state`, boot Poll must not compare a client Graph fingerprint to the server `bootstrapHash`. Fable and .NET `graphFingerprint` disagree, so equal Revision always falls back, then paints, then Polls again.

**See also:** [[07-optional-poll-bootstrap-hash.md]], [[src/Client/Program.fs]], [[src/Shared/BootCache.fs]], [[src/Client/Update.fs]] `StateLoaded`

**Status:** ready-for-agent

- [x] Shared `cachedHashForBootPoll`: after `/state`, cached hash is None even when a fingerprint string is already set.
- [x] Boot Poll after `/state` confirms when the server hash disagrees with a leftover client fingerprint.
- [x] Browser does not assign `bootHash` from `graphFingerprint` or from a snapshot that stored a client fingerprint.
- [x] IndexedDB snapshot persist does not write a client fingerprint (empty `bootstrapHash`).
- [ ] HITL: Network shows one `/state` then Poll confirms; Selection stays where the user put it.

## Comments

Console: `poll fallback hash → /state` repeats. Network: `poll` then `state?zoom=00000000-...` then `file-status`, then the same again. `StateLoaded` clears Selection; [[src/Client/SessionState.fs]] restores Zoom and folds only.
