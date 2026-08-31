# How multiple answers surface

Type: grilling
Status: resolved
Blocked by: none

## Question

When an Expression has many Answers, does evaluation backtrack one-at-a-time, collect a list, or generate sibling Nodes?

Recommended answer (HITL confirm): an Expression yields a stream of Answers. Collection versus generation is a context rule (see [[06-top-level-context-node-versus-text.md]]), not a different Expression meaning.

## Answer

HITL 2026-08-27. An Expression yields 0, 1, or many Answers as a sequence. Lazy, eager, or Prolog-style backtracking is implementation: pick whatever is fastest, as long as the Answers and their order match this spec. Materialising Answers as Children is a consumer rule on [[06-top-level-context-node-versus-text.md]], not an eval strategy.

Order: juxtaposition is left-to-right. `OR`/comma is all Answers of the left predicate, then all Answers of the right. `AND` keeps the left predicate's Answers that also appear on the right, in left-predicate order.

The sequence is not a unique set. `OR`/comma may repeat a Node. `AND` yields that Node at most once.
