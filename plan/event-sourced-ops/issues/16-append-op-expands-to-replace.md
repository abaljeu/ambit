# 16 — Append Change op expands to Replace in History

**Context:** Producers that only mean “add these children” — streaming Focus `addChild`, mailbox Actors — should not hand-build a full-list Replace against the parent’s current children. Mailbox Change already has `Replace(parentId, oldChildren, newChildren)` ([[../../../src/Shared/History.fs]]). [13 — Migrate producers to full-list Replace wire shape](13-migrate-producers-full-list-replace-wire.md) and [14 — Drop Replace index (§10 wire migration)](14-drop-replace-index-wire-migration.md) are `done`, so that contract is the expansion target. This ticket adds an in-process **Append** face that execute expands to Replace before History records. It is not [11 — Completing-ops pattern beyond timing](11-completing-ops-pattern-beyond-timing.md) (Server fill-in of missing Ops when an Actor’s view is too small).

**What to build:** Mailbox Change accepts Append. Placement is the **end** of the parent’s children only. On execute, expand to `Replace(parentId, oldList, newList)` where `oldList` is the parent’s children at the apply anchor and `newList` is `oldList @ addedChildren`. History and `EventBody` store Replace only. After expansion, existing Replace acceptBoth / amend rules apply ([[../details/replace-amendment.md]]). The stored Event’s initiating command is **Append** via existing `Ev.commandName` (see Initiating command).

**Blocked by:** None — Append can land on current `Op.Replace(parentId, oldChildren, newChildren)`.

**See also:** [[../details/replace-amendment.md]], [[../../../src/Shared/History.fs]], [[../../../src/Shared/ClientHistory.fs]] (`mintChange`), [[../../../src/Server/GraphOnlyChangePost.fs]]

**Status:** defined
**Type:** coding

- [ ] Wire / in-process face: `Append(parentId, addedChildren: ChildNode list)` (field names may match this shape). Placement is end of the parent’s children only.
- [ ] Execute expands Append to `Replace(parentId, oldList, newList)` with `oldList` = current children at the apply anchor and `newList` = `oldList @ addedChildren`, before or as History records.
- [ ] History / `EventBody` never stores Append. Permanent log and poll tails show Replace only.
- [ ] After expansion, existing Replace acceptBoth and Server amend rules apply ([[../details/replace-amendment.md]] §2–§5).
- [ ] Initiating command on the stored Replace Event is `Append` via `Ev.commandName` (no new Op or envelope field).
- [ ] Proofs: unit and Server tests that posting Append yields History Replace, `commandName = "Append"`, and Graph children grow at the end.

## Initiating command

**General rule:** a Change states the command that initiated it.

**Home:** existing `Ev.commandName` ([[../../../src/Shared/History.fs]]). `Ev` already carries `commandName: string` next to `body`. Client `mintChange` and `GraphOnlyChangePost.mint` already take that string. This is Event / Change metadata, not an Op field: after expansion the stored Ops are Replace, and consumers still need to see that the Change came from Append. Do not add a field on `Op`. Do not add a second initiating-command field on the Change envelope.

**This ticket:** Append expansion sets `commandName` to `"Append"` on the stored Replace Event.

**Follow-up (not this ticket):** several mailbox / Server producers still mint `commandName = ""` (lifecycle Events in [[../../../src/Server/Core/CoreEventDispatch.fs]], [[../../../src/Server/TestActor.fs]], [[../../../src/Server/RunAgentActor.fs]], [[../../../src/Shared/ImportText.fs]]). This ticket defines the rule and implements it for Append. Do not rename every historical producer overnight.

## Non-goals

- Insert-before, mid-list splice, or an index on Append.
- Keeping Append in History, `EventBody`, or the permanent event log.
- Changing Replace wire JSON field names (`oldChildren` / `newChildren` stay; rename closed in [14 — Drop Replace index (§10 wire migration)](14-drop-replace-index-wire-migration.md)).
- Stamping `commandName` on every existing producer (follow-up above).
