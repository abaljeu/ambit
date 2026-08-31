# Define synchronization and revision correctness

Type: grilling
Status: resolved
Blocked by: 03

## Question

How must polling, catch-up, and load snapshots interact when server changes touch unloaded or partially present regions: which facts should the client install, ignore, or record, how should it advance revision and invalidate loadedness, and how should it prevent stale resurrection while coordinating in-flight loads and local edits?

## Answer

- The client revision labels its resident projection: after advancing to revision `R`, every resident header and every `Loaded` child list reflects `R`; `Unknown` lists and absent headers make no claim. Catch-up applies changes to represented facts and ignores changes only to unrepresented facts. Synchronization never demotes `Loaded` to `Unknown`.
- Poll and post-bootstrap load responses use the same catch-up transaction from client base revision `B` to one atomically read server revision `R`. It contains the ordered history actions (`Change`, `Undo`, or `Redo`) from `B` through `R` plus authoritative headers or tombstones at `R` for every node touched or named by that tail. A load response also contains its requested `Direct | ArtifactClosure | Workspace` snapshot at `R`; normally `B = R` and the tail is empty.
- Tail actions replay against projected State. Change effects apply only to represented facts, structural spans apply only to `Loaded` lists, and Undo or Redo performs the normal projected History transition. Every accepted revision retains its projected history entry even when its graph effect is empty. Supplemental headers install only when already resident or required by a resident or newly loaded list; other facts remain absent. Requested snapshots authoritatively overwrite their returned headers and lists at `R`.
- The client applies tail actions, supplemental facts, requested snapshots, derived indexes, and revision advancement atomically. A server tombstone removes a represented deleted node; deletion is not residency eviction. Missing history needed after `B` requires a full bootstrap.
- A load target deleted before `R` yields valid catch-up without requested snapshot facts. Its command-specific continuation observes the missing target and produces no invalid mutation. Malformed requests remain protocol failures.
- Poll and load responses compare-and-swap against their captured base revision and mutation epoch. Concurrent loads remain allowed; the first valid response commits, while responses based on superseded state are discarded and any still-required request is retried through generic load handling, preventing duplicate tail application and stale resurrection.
- Polls and loads dispatch only while no graph-changing local submission is pending. A graph-changing edit after dispatch changes the mutation epoch and invalidates the whole response; no remote catch-up is rebased over optimistic local changes.
- If projection, supplemental closure, decoding, or final invariants fail, the transaction changes neither graph nor revision and requires a full bootstrap. Such failure is a coding or protocol invariant violation, not ordinary revision drift, and never invalidates loadedness in place.
