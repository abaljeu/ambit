# 07 — Optional Poll `bootstrapHash`

**Context:** Poll at equal Revision cannot see Graph corruption in a cached snapshot. An optional `bootstrapHash` on the Poll response detects equal-Revision Graph drift. Old Servers omit the field; the Browser then skips the compare.

**What to build:** Optional `bootstrapHash` on `ChangeSuccessResponse`. Shared fingerprint of ROOT-closure Graph encoding. Server fills it on Poll (POST ack may omit). Client compares to the cached snapshot hash when both are present and Revisions are equal; mismatch deletes cache and fetches `/state`.

**Blocked by:** [[05-novel-tail-and-state-fallback-matrix.md]]

**See also:** [[.scratch/client-start-time/reports/cache-first-boot-via-poll.md]], [[src/Shared/ApiResponses.fs]], [[src/Shared/ApiResponseSerialization.fs]], [[src/Server/Api.fs]]

**Status:** ready-for-agent

- [x] `ChangeSuccessResponse.bootstrapHash` is `string option`; encoder omits None; decoder treats missing as None so old Servers still decode.
- [x] Shared fingerprint of `rootBootstrapGraph` is stable for the same Graph and differs when ROOT-closure content differs.
- [x] Equal Revision plus both hashes present and unequal: fallback `/state` and delete cache.
- [x] Missing Poll hash: no hash fallback (confirm or novel-tail path as before).
- [x] Focused Shared tests cover omit/round-trip, mismatch fallback, and missing-hash skip.
