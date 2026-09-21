# Plan or doc change — Focus vs Command on Run

Requirement named in chat: after Run on `0c716ef3` with Focus on `?ai cursor` and child `What time is it?`, the question was replaced by the time. Intent: Focus on `What time is it?` so the time becomes a Child of Focus; Command remains the `?ai …` ancestor.

## 1. Owning layer

**Architecture.** Highest layer: [llm-connector architecture](../../llm-connector/arch.md) Locked **Focus vs Command on Run**, and [core-creation architecture](../arch.md) **Browser Run** product item (distinct `focusId` / `commandId` / `zoomId`). Cross-ticket: hello one-Node proof vs product AI Run.

User named a live repro and intent; that differed from the owning layer (Browser one-Node overspec in core-creation arch).

## 2. Check upward

1. **Spec — no change.** [llm-connector spec](../../llm-connector/spec.md) already treats Focus as reply parent / Command-text dispatch. No strip needed.
2. **Map — no change.** Wayfinders do not force one-Node equality for product Run.

Forcing clause was core-creation **Browser Run** Interface “current Node is Command, Zoom root, and Focus” — load-bearing for hello proof, overspecified for product AI. Split: keep one-Node as hello; add product Run with distinct ids.

## 3. Scope

1. **map** — no-change.
2. **spec** — no-change.
3. **architecture** — change (llm-connector Locked + core-creation Browser Run).
4. **ticket** — change (new [51](../issues/51-browser-run-focus-vs-command.md)).
5. **code** — no-change this pass.

## 4. Apply

1. llm-connector Locked **Focus vs Command on Run**; renumber Live Actor chrome to 9.
2. core-creation Browser Run: hello one-Node kept; product Run item open.
3. Ticket 51 defined.

## 5. Ratchet

Architecture gained one Locked clause and one Browser Run product item. Hello path unchanged. Not more conditional than splitting hello vs product.

## 6. Flagged lower artifacts

1. **Code** — `CommandRequest.oneNodeStart` and Client Run still force equal ids until 51 is coded.
2. **35b** — done; do not reopen. Remains the one-Node proof.
3. **Actor live labels / 21** — same Command scan rule; separate conveyance ticket.

## 7. Outcome

Edited architecture (both Projects) and filed ticket 51. Code untouched.

## 8. Amendment 2026-09-20

Alan: lines containing `=` are also runnable; do not skip past those on the Focus → zoom owner-scan. Architecture and ticket 51 updated: runnable = starts with `?` **or** contains `=`; scan stops at the first.
