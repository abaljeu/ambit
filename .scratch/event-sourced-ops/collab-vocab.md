# Parent-facing — fill-in on the stack

Speak this. Not a quiz.

Acknowledged: as-practice, Server fill-in Ops are **relayed onto the Browser undo stack**. Timing **accepted**: delete + promote-Ref share **one** History entry; Undo inverts both. Later Poll fill-in is rejected.

Lookup tension: ACK suffixes today are `SetUpdateTime` only and **do not** enter History. Poll/Load with a Change tail still **clears** History. So a later Poll fill-in would wipe the stack, not append. The path that already puts fill-in on the stack is the Browser packing promote-then-remove in the **same** Change before submit.

Linear History stays this Browser process. Fill-in is more Ops on that stack — only if it is not the Poll-clear path. Cancel ≠ Undo still holds.

Owner count: a well-formed Change does not raise owner 1→2. Extra-Owned → Ref is a bug net. Classes: merge is set delta, not implemented replace. Same-text **accepted**: Server arrival is first; `B` on the Node; `amb-conflict` child `C`; optimistic Client rewind+replays (not `SetText C→B`). Name **accepted**: first name stays on the Node; `amb-conflict` child with the new name (a **Normal Node**; Merge success, not Reject). DocumentState: field deleted; NoServerFile / Unparsed inferred. Children **accepted**: default positional Replace (posted Op). Conflict: bag Accept Both (edges); Server amends the newest Replace after other accepted Changes — not a node-local rewrite. Implemented span-CAS Replace is behavior to beat. No `amb-conflict` for edges.

Soft-lock **meaning accepted:** a long-running Actor (Parse File, Shell, same kind) checks out its subtree — recommended to work elsewhere, not illegal. Concurrent edits there Merge (job amended as newest). Not a hard lock. [[soft-lock.md]].

Amendment order: common prior, then other Actors' accepted Changes, then the newest Actor's Change amended against that combined Local Graph. Server **produces** the sequence. Client **consumes** it by rewind+replay (accepted): rewind to the common prior, replay that list. Not genesis replay. Not a node-local patch. Leftover pending stays **unamended**; next POST sends it; Server amends. POST/Poll carry last-received Server Revision only. Fill-in still completes one Change. Load packages stay Graph transfer.

POST and Poll are **not** the same ("Poll = empty POST" **superseded**). POST ACK **informs** that external Changes exist; Client notes **baseline**. Queue-empty **Poll** applies the Change list (undo to baseline + replay). Neither clears History (today's Poll clear is debt). Recoverable kick-back is 200 Merge. [[pipelined-post.md]]

Unrestricted Undo: **possible** now (global order of Changes). **Desirability open** — can Actors see and understand those edits to choose Undo? Not answered. [[undo.md#Unrestricted Undo desirability]].

This framework is a **more general relaxed concurrency** than [[.scratch/relaxed-concurrency/map.md]] (CAS-or-reject; later Reject+replan). Sibling, not a replacement. Genesis replay stays rejected. [[more-general-relaxed-concurrency.md]].
