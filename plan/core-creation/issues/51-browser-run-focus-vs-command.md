# 51 — Browser Run: Focus reply parent, Command is runnable ancestor

**Status:** coded
**Blocked by:** None — [35b — Browser Run hello](35b-browser-run-hello.md) one-Node proof is `done`. AI Actor replace-under-Focus is delivered (llm-connector 08).
**Type:** coding
Estimate: 2h
Actual: 2h

## Context

llm-connector locks **Focus** as the reply parent (Children replace/stream boundary). Hello/TestActor proof used **one-Node** Run: current Node is Command, Zoom, and Focus together. That is wrong for product AI: if Focus is the `?ai …` Command and the question is a Child, complete replace wipes the question.

**Intent (locked):** Focus = question (e.g. `What time is it?`); Command = nearest runnable owner-ancestor (text starts with `?` **or** contains `=`, e.g. `?ai cursor`); reply becomes a **Child of Focus**. Zoom root remains the Included extract root (may equal Command or an ancestor). Scan stops at the first runnable node — do not skip `=` lines looking for a `?` above.

Architecture: [core-creation arch](../arch.md) **Browser Run** product item; [llm-connector arch](../../llm-connector/arch.md) **Focus vs Command on Run**. Runnable = text starts with `?` or contains `=`.

## What to build

### 1. Client encode

1. [x] On Run of a runnable selection path, set `focusId` from selection Focus (reply parent), not forced equal to Command.
2. [x] Set `commandId` from the first runnable node on owner-scan Focus → zoom root (`?` prefix **or** contains `=`; do not skip `=`).
3. [x] Set `zoomId` to the Included extract zoom root (existing Included descendant list at that root).
4. [x] Refuse or surface a clear Error when no runnable Command exists on that path.

### 2. Proof

1. [x] Shared or Server fact: Graph with Command `?ai …` (or `?test hello`), Focus = child question node; start carries distinct `commandId` / `focusId`; after success, reply Owned child(ren) sit under the question Focus, and the question text remains.
2. [x] One-Node hello path still works when Focus is the Command node itself (TestActor / 35b shape).

### 3. Non-goals

1. Changing Amb replace vs stream write algorithm (llm-connector 18).
2. Actor live label TitleCase (21 rework / Actor live labels).
3. Widening Core doors beyond existing `zoomId` / `focusId` / `commandId`.

## See also

[35b — Browser Run hello](35b-browser-run-hello.md), [llm-connector 06](../../llm-connector/issues/06-define-command-run-agent-redesign.md), [llm-connector 08](../../llm-connector/issues/08-agent-ask-from-what-i-see.md), [../arch.md](../arch.md), [../../llm-connector/arch.md](../../llm-connector/arch.md)

## Comments

- 2026-09-20 — Coded: Client Run uses [CommandRequest](../../../src/Shared/CommandRequest.fs) `tryStart`; Focus is selection, Command is first runnable owner (`?` or `=`), Zoom is Included extract root. No Command and not Amble → Run Error. One-Node hello remains when Focus is the Command. Status `coded`.
- 2026-09-20 — Type `coding` (implementation), not Wayfinder task.
- 2026-09-20 — Amendment: lines containing `=` are also runnable; owner-scan must not skip past them.
- 2026-09-20 — Filed from chat: Run with Focus on `?ai cursor` and child `What time is it?` replaced the question; intent is Focus on the question so the time is a Child. plan-or-doc-change placed Focus vs Command in architecture. Status `defined`.

## Time

- 2026-09-20 — Ticket (from chat)
- 2026-09-20 2h — Product Run encode and Focus vs Command proof (from chat)
