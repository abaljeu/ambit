# Top-level context: Node versus text

Type: grilling
Status: open
Blocked by: none

## Question

Which contexts generate Nodes, display text, or do other work? Cover Run, Find, assignment, and display.

Recommended answer (HITL confirm): Run on a Normal Node materialises Node Answers as Children (current Amble Run shape). A display or search context shows text Answers as text. A Node-valued Expression in a text context uses a defined coercion (name or text of the Node).

[[05-how-multiple-answers-surface.md]] locked the Answer sequence and left consumers here. The eval engine (lazy, eager, or backtracking) is not this ticket.
