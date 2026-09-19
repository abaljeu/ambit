# llm-connector

Updated: 2026-09-19

Sources locked into this spec: [[issues/06-define-command-run-agent-redesign.md|06 — Define the revised Command + Run Agent seam]], [[issues/07-lock-run-agent-architecture.md|07 — Lock the Run Agent architecture]], [[map.md]], [[reports/agent-redesign-locked-2026-09.md]]. Epic chapter: [[plan/roadmap/epics/chapters/ask-from-what-i-see.md|Ask from what I see]].

Checklist convention: `[x]` means locked by 06/07 and/or already delivered by the Core lifecycle rebuild (TestActor hello and related Core doors). `[ ]` means still to build for this Project.

## 1. Problem Statement

1. **Ask from what I see** — A person looking at an outline wants to ask an Agent about the included context and get a reply under Focus, without leaving Ambit or managing a separate chat transcript.
2. **Long-running work** — The ask may take time. The person must keep editing elsewhere, see that a job is live for a Focus, and cancel without Undo.
3. **Shared update process** — Agent replies must enter the Graph through ordinary Core Changes so merge, Poll, and History stay one model — not a second paste protocol.

## 2. Solution

1. **Command-text Run Agent** — The person marks a Command Node, grows included context under Zoom, sets Focus, and Runs. Command text dispatches the Actor (`?test hello` for TestActor; `?ai` + args (ignored for now) for the Agent Actor). Focus is the stable reply parent.
2. **Mixed-format extract** — Core builds an authoritative Zoom-rooted extract. Document serializes each Node through its owning codec into one mixed-format document with Focus marked. CloudAgents completes system prompt plus that document.
3. **Focus-child replacement** — On success, delete every current Child under Focus and create new Children from the complete response (structural parse, else plain indentation). On failure or cancel, preserve Focus Children (except earlier accepted Changes). Ordinary Core merge owns concurrent edits.
4. **One mailbox lifecycle** — Launch, Change, cancel, terminal, and drop share one Core mailbox and durable ActorStarted / ActorFinished Events. At most one live Actor per Focus NodeId.

## 3. User Stories

1. [x] **Command-text dispatch** — As a person, I want Command Node text to select which Actor runs (`?test hello`, later `?ai` + args (ignored for now)), so that role, Kind, and CSS class do not invent a second dispatch channel. Locked: [[issues/06-define-command-run-agent-redesign.md|06]]. Delivered for TestActor hello via Core.
2. [x] **One Zoom, one Focus** — As a person, I want one Run to carry one Zoom-rooted subgraph with exactly one Focus inside it, so that the Actor packs a clear working set. Locked: [[issues/06-define-command-run-agent-redesign.md|06]].
3. [x] **TestActor hello proof** — As a builder, I want `?test hello` through Browser Run and Outside Core to produce one Owned child `hello`, so that the Core lifecycle is proven before the Agent Actor. Delivered: core-creation 34b / 35b.
4. [x] **Agent ask from Focus** — As a person, I want Command text `?ai` with optional args (args ignored for now) to launch the Run Agent Actor with mixed-format context and return Owned children under Focus, so that Ask from what I see works end-to-end. Locked behavior: [[issues/06-define-command-run-agent-redesign.md|06]]. Delivered: [[issues/08-agent-ask-from-what-i-see.md|08]] with Amb extract-walk pack from [[issues/11-simple-extract-format.md|11]].
5. [ ] **Mixed-format pack** — As the Run Agent Actor, I want Document to serialize the extract with each Node's owning codec and Focus marked, so that the model sees one coherent document. Locked: [[issues/06-define-command-run-agent-redesign.md|06]], [[issues/07-lock-run-agent-architecture.md|07]].
6. [x] **Vendor-neutral complete** — As composition, I want CloudAgents to take system prompt plus document plus cancellation and return Completed text, Failed safe error, or Cancelled, so that Cursor stays an ordinary adapter. Locked: [[issues/07-lock-run-agent-architecture.md|07]]. Face kept; `setFake` on the DLL. Delivered: [[issues/08-agent-ask-from-what-i-see.md|08]].
7. [x] **Focus-child replace on success** — As a person, I want a successful reply to replace every current Focus Child from the complete response (structural then Plain fallback; empty success clears children), so that Focus stays the stable boundary. Locked: [[issues/06-define-command-run-agent-redesign.md|06]], [[issues/07-lock-run-agent-architecture.md|07]]. Delivered: [[issues/08-agent-ask-from-what-i-see.md|08]].
8. [x] **Preserve children on failure** — As a person, I want provider or Agent failure to keep Focus Children with no response Change, so that a bad run does not wipe my outline. Rule: Actor framework does not cause Changes on failure; AI Actor does not erase data (future agentic extensions out of scope). Locked: [06 — Define the revised Command + Run Agent seam](issues/06-define-command-run-agent-redesign.md), [09 — Agent failure preserves children](issues/09-agent-failure-preserves-children.md). Framework: TestActor / `?test`. AI: `?ai` + `setFake` Failed.
9. [x] **Cancel by Focus** — As a person, I want to cancel the live job by Focus NodeId, preserving children and clearing running state through ActorFinished, so that cancel is not Undo. Locked: [[issues/06-define-command-run-agent-redesign.md|06]], [[issues/07-lock-run-agent-architecture.md|07]]. Core / harness proof: [[issues/10-cancel-by-focus.md|10]]. Browser chrome may still be open on core-creation 21/22.
10. [x] **Focus exclusivity** — As Core, I want at most one live Actor per Focus NodeId while different Focus values may run concurrently even when extracts overlap, so that two asks on the same Focus do not race. Locked: [[issues/06-define-command-run-agent-redesign.md|06]]. Core admission path exists with hello lifecycle.
11. [x] **Ordinary Core Changes** — As Core, I want Agent output submitted as normal Changes (not Graph-only Parse), so that merge and amendment stay one path. Locked: [[issues/06-define-command-run-agent-redesign.md|06]], [[issues/07-lock-run-agent-architecture.md|07]]. Proven with TestActor hello Changes.
12. [x] **Durable lifecycle Events** — As Core, I want ActorStarted and ActorFinished on the one EventLog with Authority names, so that Poll and recovery see the same serial. Locked: [[issues/07-lock-run-agent-architecture.md|07]]. Delivered with mailbox History durability.
13. [x] **Typed launch membership** — As Core, I want the Browser to send exact included NodeIds, Zoom, Focus, Command, and current event id, with Core building the authoritative extract, so that the client cannot smuggle a second graph. Locked: [[issues/07-lock-run-agent-architecture.md|07]]. Browser Run hello exercises the path for TestActor.
14. [ ] **Live Actor projection** — As a person, I want the Browser to show a live Actor for a Focus from ActorStarted / ActorFinished via Poll, without a Graph lock-present field. Owned by core-creation 21/22; this Project’s first Agent vertical proves Graph + Poll only. Architecture: [[issues/07-lock-run-agent-architecture.md|07]].
15. [x] **Credentials stay out of Graph** — As Core, I want secret credentials never to persist and public Actor identity to remain durable after drop, so that Authority is safe. Locked: [[issues/07-lock-run-agent-architecture.md|07]]. Delivered on credentialed posts / hello.

## 4. Out of Scope

1. **Cancelled Create slice** — Restore [[issues/05-create-cloud-agent-posts-reply-under-focus.md|05 — Create cloud-agent posts a reply under Focus]] Create payload, Focus-only lock, Md paste-replace, or PR #4 vertical.
2. **Follow-up turns** — Multi-turn chat, Talk again, LLM-authored free Changes — later Epic Chapters.
3. **Graph/file query Actors** — Query the Graph or files as Agent tools — later Chapters.
4. **CLI / MCP act** — Act through CLI or MCP — later Chapters.
5. **Provider domain behavior** — Cursor-specific domain rules in Core or Browser; provider selection stays inside CloudAgents composition.
6. **Exact Focus sentinel spelling** — Escaping and sentinel strings are implementation details deferred by [[issues/07-lock-run-agent-architecture.md|07]].
7. **New merge policy** — Stale checks, span-overlap refusal, last-writer, or special undo of accepted Agent output.
8. **Same-transaction event+Graph** — Persistence hardening beyond existing EventLog authority.
9. **Expand / Edit dual-locus patterns** — Redesign brainstorm only; not this Project's first vertical.

## 5. Further Notes

1. **Info hub** — Ambit pulls in, transforms, and brings back; Actor formats vary; the adaptive update process is common ([[reports/agent-redesign-locked-2026-09.md]]).
2. **Spoken name** — Run Agent. Do not say Agent for the Actor ([[CONTEXT.md]]).
3. **Recognition only** — [[plan/expression-language/issues/33-recognize-ask-run-statement.md]] owns `?` as a Run statement; this Project owns pack, call, and write-back.
4. **Next build** — Smallest vendor-neutral CloudAgents / Run Agent Actor implementation issues after this arch; not a revival of 05.
5. **First pack** — [[issues/11-simple-extract-format.md|11 — Pack extract with Amb (supplied-fragment walk)]] writes the supplied extract with Amb extract-walk. Mixed-format owning-codec stays tabled. Mixed-format stories above remain the product intent.
