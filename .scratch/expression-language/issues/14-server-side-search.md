# Server-side search

Type: grilling
Status: resolved
Blocked by: none

## Question

[[13-fog-of-the-first-spec.md]] locks Unloaded walks as fail-to-answer on the client, and plans server-side Search over the complete Graph. Which consumers eval on the server (Find, Move, Run, all), and how does that sit with resident-only Find on the client?

Recommended answer (HITL confirm): the language matcher (`=` in Find / Move) evals on the server; Run stays on the client Graph.

## Answer

HITL 2026-08-27. The language matcher (`=` in Find and Move) evals on the server Graph. Run stays on the client Graph. Unloaded walks on the client remain fail-to-answer. Word search without `=` stays today’s client word search.

## Amendment

HITL 2026-08-27, later. All eval is local (client Graph): Run, Find `=`, and Move `=`. Server-side Search is postponed. Unloaded walks stay fail-to-answer.
