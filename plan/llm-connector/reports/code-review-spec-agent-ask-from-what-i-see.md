# Spec-axis review: Agent ask from what I see

Range: `git diff origin/staging...HEAD` (HEAD `3c0e14cf`; `origin/staging` `08831db1`). Primary: [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md). Architecture: [llm-connector architecture](plan/llm-connector/arch.md) (**Run Agent Actor**, **Document**, **CloudAgents**, **CoreMailbox / CoreMsg / CoreActorPool**; Story path **Agent ask from what I see**; Locked **CloudAgents DLL interface** and **CloudAgents setFake**). Consume only: [11 — Pack extract with Amb (supplied-fragment walk)](plan/llm-connector/issues/11-simple-extract-format.md).

## (a) Missing or partial

1. **Concurrent merge proof is partial.** [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md) §5.2: "a credentialed Change under Focus while the Actor runs reconciles with ordinary merge only (Story path **Credentialed Change while Agent runs** open hop)." The diff adds no Agent-specific stale rule (`getState` then `ChildListWire.replace`; harness `postEvents`). The new test only asserts that post is `Ok` and the Actor reaches `ActorSucceeded`. It does not read Focus Children after both Changes.

## (b) Scope creep

None material. `setFake`, `?ai` → `RunAgentActor`, `AmbWriteWalk.SuppliedExtract` pack, `FocusChildrenReplace`, ordinary Core Change, and fake Ask tests match the ticket. Server composition references CloudAgents; `src/Server/Core` does not. [12 — Replace Focus Children from reply](plan/llm-connector/issues/12-replace-focus-children-from-reply.md) stays open; that is not extra code.

## (c) Implemented but looks wrong

1. **Cancel-token complete is marked delivered.** [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md) §2.2: "Complete — fit system prompt + document + cancel into existing CloudAgents `start` / `poll` / `cancel`; map outcomes to Completed | Failed | Cancelled." [llm-connector architecture](plan/llm-connector/arch.md) step 6: "Run Agent Actor → CloudAgents complete (system prompt + document + cancel token)." Module **CloudAgents** interface 3 and Locked **CloudAgents DLL interface** mark the same fit `[x]`. `RunAgentActor.complete` concatenates prompt and pack, calls `start` and `waitUntilComplete`, and maps every error to `ActorFailed`. It does not call `cancel` or emit `Cancelled`. [10 — Cancel by Focus](plan/llm-connector/issues/10-cancel-by-focus.md) still owns the Cancel story; this is 08's Complete line claimed done.

## Summary

Success path matches: DLL `setFake`, Amb extract-walk pack, Focus-child replace, fake Ask through ordinary Core Change, Core without CloudAgents. Two findings. Worst: Complete/cancel mapping marked `[x]` while `RunAgentActor.complete` never fits a cancel token.
