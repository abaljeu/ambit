# Fog of the first spec

Type: grilling
Status: resolved
Blocked by: none

## Question

Which Not yet specified patches belong in this hand-off spec, stay later, or are not planned: logical variables / unification, cut / if-then, `findall`/`bagof` collection, Unloaded walks, number-returning functions, shell `> …`, quoted path segments versus quoted filter strings?

## Answer

HITL 2026-08-27.

- Logical variables / unification: not planned.
- Cut / if-then: not planned.
- `findall`/`bagof` collection primitive: not planned. Collection stays the Run / Search consumer.
- Unloaded Node in a walk: fail-to-answer. No load side-effect. Server-side Search was planned, then postponed: [[14-server-side-search.md]].
- Quotes: filter strings (`containing "the"`) are in this spec. Quoted path segments stay later.
- Number-returning functions and shell `>`: out of this spec for now.
