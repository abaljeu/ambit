# Poll vs Load conveyance

Facts from current code. Not a design. **POST and Poll are not the same path** (unification superseded — [[unified-messaging.md]], [[pipelined-post.md]]). Do not read "Poll = empty POST" as current design.

## Poll — GET `/ambit/poll?rev=N`

**Sent:** `PollResponse`: revision, deploy/page build stamps, `isReady`, and `changes: Change list` (wire `c`). No Graph, no Node packages, no files. Each `Change` is `{ id, changeId, ops }`. Empty `c` when the Browser is current.

**Browser:** `SyncLogic.applyServerTail` → `ResidentProjection.applyChange` → `Op.apply` per Op. No map-merge. Non-empty Change tail **clears History** (`applySyncResponse`). That is **software debt**. Design: Poll is its own path (queue-empty catch-up from **baseline**); it must not clear History ([[pipelined-post.md]]).

**Verdict:** Yes — Poll is Changes/Ops only.

## Load — POST `/ambit/load`

**Sent:** `LoadResponse`: the same Poll envelope (`r`/`b`/`p`/`ready`/`c`) **plus** `packages: Node list` — complete Workspace subgraph Nodes at that revision. Not files. Packages come from slicing the Server Graph (`packagesForTargets`), not from replaying Ops.

**Browser:** `SyncLogic.applyLoadResponse` applies the Change tail through `Op.apply`, then `ResidentProjection.installPackages` (`Map.add` Nodes). Mixed.

**Verdict:** Partial — Ops for the tail; Graph transfer for residency.

## `/state` (out of this question)

GET `/ambit/state` is `{ revision, graph }` only. No Change list.

## Sources

[[src/Shared/ApiResponses.fs]], [[src/Shared/ApiResponseSerialization.fs]], [[src/Server/Api.fs]] (`getPoll`, `postLoad`), [[src/Shared/SyncLogic.fs]], [[src/Client/Update.fs]] (`PollDone`, `LoadDone`), [[doc/api.md]] (Poll).
