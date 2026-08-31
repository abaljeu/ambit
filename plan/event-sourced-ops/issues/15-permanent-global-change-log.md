# 15 — Permanent global Change log

**Context:** Today startup loads graph + revision from the DB projection but does not treat the persisted Change log as the recovery source ([[../details/permanent-history-and-genesis.md]]). File-mode bootstrap/migration can truncate `changes`; ordinary restart must load **DB projection + log** so open Browsers catch up through merge instead of stale-client rejection (`DataOutdated`, `ServerRejected`). Post-protocol change (`CodeOutdated`) remains a forced reload; this ticket addresses restart with unchanged protocol only.

**What to build:** Stop discarding the append-only global Change log on server restart except at an explicit genesis boundary. **Store (proposed):** the existing PostgreSQL `changes` table ([[src/Server/Database.fs]]) — evolve retention policy; do not introduce a file log, Redis, or second store. Recovery loads **DB projection + permanent Change log** — not re-parse from document files. Load current graph + revision from the DB projection as today. Poll and Load tails read from the permanent log. Document or record the genesis revision when the permanent log is instituted. Do not require Clients to replay from genesis on ordinary catch-up.

**Blocked by:** none (charting/spec slice; may follow issue 04 polish)

**See also:** [[../details/permanent-history-and-genesis.md]], [[../details/messaging.md]], [[../details/client-consume.md]], [[src/Server/Database.fs]], [[src/Server/DatabaseSetup.fs]], [[src/Shared/SyncLogic.fs]]

**Status:** ready-for-agent

- [ ] Permanent log store is the PostgreSQL `changes` table (existing or evolved schema in [[src/Server/Database.fs]]); no new file/Redis/secondary log store.
- [ ] `changes` is not truncated on ordinary server restart.
- [ ] Recovery/rebuild loads from DB projection + permanent log; document-file re-parse is not the recovery path — written policy in code or ops doc.
- [ ] Explicit new-genesis boundary (migration only) is documented if log truncation is ever allowed again.
- [ ] Open Browser with pending work can catch up (poll tail or amended post) without stale-client wipe when protocol stamps are unchanged.
- [ ] `CodeOutdated` behavior unchanged when build stamps differ.
- [ ] No routine Client genesis replay; short-tail rewind+replay unchanged.
- [ ] Server restart with unchanged protocol: no forced Browser reload; state (graph + revision) consistent with pre-reset.
- [ ] Old Clients accepted by default; stale rejection only at explicit fail points (`CodeOutdated`, malformed, auth).
- [ ] Short-term transition policy documented: Server-generated Browser code; compatibility = keep state + protocol consistent for prior build.
