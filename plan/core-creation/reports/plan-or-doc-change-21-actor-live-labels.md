# Plan or doc change — Actor live labels

Requirement named in chat: Run on `?test hello` shows “AI: …” / “AI: Actor succeeded.”; then “new ticket or find failed ticket” with Command id = scan up from Focus to zoom root for text first char `?`; then set 21 to `coded` (failed review); then TitleCase the command name. Ticket 50 was filed then cancelled into 21.

Workplace: branch `cursor/actor-live-cmd-result-label` / draft PR #79. Code fix in flight on a cloud agent.

## 1. Owning layer

**Architecture.** Highest layer whose invariant must change: [Core creation architecture](../arch.md) module **Actor live labels**. Cross-ticket coupling (21 conveyance + 14’s hardcoded AI chip on `lastCmdResult`) belongs in architecture. Tickets consume that shape.

The user named the problem as a Browser bug and ticket 21 / a new ticket 50. Named location (ticket/code) differed from the owning layer.

## 2. Check upward

1. **Spec — no change needed.** [llm-connector spec](../../llm-connector/spec.md) item **Live Actor projection** only requires showing live Actor from lifecycle Events without Graph lock-present; it does not name result chips or TitleCase. No forcing clause to strip.
2. **Map — no change needed.** [Core creation Wayfinder](../map.md) does not state live result labeling. llm-connector map parks chrome on core-creation 21/22.

No clause above architecture forced the hardcoded-AI ugliness. The overspecified fixed **AI** chip lived in code and in ticket 14 / 21 example wording.

## 3. Scope

Declared before edits in this run:

1. **map** — no-change.
2. **spec** — no-change.
3. **architecture** — change. Place **Actor live labels** (Command via Focus→zoom owner scan; TitleCase actor token; no fixed product name).
4. **ticket** — change. [21](../issues/21-client-shows-lock-present.md) consumes architecture; Start/Stop result items reopen unchecked. [50](../issues/50-actor-live-labels-from-command.md) stays `cancelled` (wrong owning layer for a new ticket).
5. **code** — no-change this pass. Flag mismatch until the in-flight cloud agent matches architecture.

## 4. Apply

Top-down.

1. **Architecture** — [arch.md](../arch.md) `Updated: 2026-09-20`. New Module map **Actor live labels** and Seams **6a**. Load-bearing: resolve Command on owner path Focus→zoom for `?`; TitleCase actor token; generic if missing. Specific F# signatures and test fixtures stay down.
2. **Ticket 21** — Sequence and What to build Start/Stop results point at architecture; strip duplicated scan/TitleCase prose from Comments into one plan-or-doc-change note. Status remains `coded`.
3. **Ticket 50** — Comment: plan-or-doc-change confirms cancel (architecture + 21).

## 5. Ratchet

Architecture gained one thin module and one seam pointer. Not more conditional; does not admit only one F# helper name. Ticket thinned relative to inlined algorithm. Upper layers (map/spec) unchanged length.

## 6. Flagged lower artifacts

1. **Code** — [ActorLive.fs](../../../src/Shared/ActorLive.fs) still hardcodes AI in `lastCmdResult` until the cloud agent lands. Mismatch with architecture.
2. **llm-connector 14** — [14 — Provider-named AI errors](../../llm-connector/issues/14-provider-named-ai-errors.md) checklist “command label is **AI**” overspecified the chip for the `?ai` path; do not retrofit the done ticket. Architecture now forbids a fixed product chip for all Actors.
3. **project.md / PR #79** — housekeeping notes; not stack layers.
4. **mitigations.md** — still parks 21 on event-sourced-ops; stale vs delivered chrome; out of this requirement.

## 7. Outcome

Edited layers: architecture, tickets 21 and 50 comments. Did not edit map, spec, or code in this pass.
