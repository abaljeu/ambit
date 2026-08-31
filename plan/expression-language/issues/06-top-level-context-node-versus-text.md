# Top-level context: Node versus text

Type: grilling
Status: resolved
Blocked by: none

## Question

Which contexts generate Nodes, display text, or do other work? Cover Run, Find, assignment, and display.

Recommended answer (HITL confirm): Run on a Normal Node materialises Node Answers as Children (current Amble Run shape). A display or search context shows text Answers as text. A Node-valued Expression in a text context uses a defined coercion (name or text of the Node).

[[05-how-multiple-answers-surface.md]] locked the Answer sequence and left consumers here. The eval engine (lazy, eager, or backtracking) is not this ticket.

## Answer

HITL 2026-08-27. Consumers follow what is already implemented. Every function has a defined type. Mixing kinds is only possible with `AND` / `OR` / `NOT` and is an error. No function in this catalog returns a number. A number literal is only valid as the right operand of `:` or `!`. Anywhere else is a type error.

**Run:** Node Answers become Ref Children. Text Answers become new Owned Nodes (that text). Statement syntax, invalid-format no-op, blueletter `No matches found`, and unfold are locked on [[07-statements-in-this-spec.md]]. There is no separate display consumer.

**Search and Move:** keep the existing scrolling dialog. A leading `=` evals a Node-typed Expression locally; omitted left side is zoomRoot. No leading `=`: today’s word search. The dialog fetches Answers as the user scrolls. The user picks one Node. Search zooms that Node. Move relocates to that Node. 0 Answers and a parse or type error both show no hits (as now). All eval is local; server postponed: [[14-server-side-search.md]].
