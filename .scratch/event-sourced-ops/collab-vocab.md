# Parent-facing — fill-in on the stack

Speak this. Not a quiz.

Acknowledged: as-practice, Server fill-in Ops are **relayed onto the Browser undo stack**.

Implication: if delete + promote-Ref share **one** History entry, Undo inverts both. If they are **two** entries, Undo last inverts whichever landed last (fill-in first, or the delete).

Lookup tension: ACK suffixes today are `SetUpdateTime` only and **do not** enter History. Poll/Load with a Change tail still **clears** History. So a later Poll fill-in would wipe the stack, not append. The path that already puts fill-in on the stack is the Browser packing promote-then-remove in the **same** Change before submit.

Linear History stays this Browser process. Fill-in is more Ops on that stack — only if it is not the Poll-clear path. Cancel ≠ Undo still holds.

Fill-in timing, extra-Owned → Ref, same-text HITL/reject still open.
