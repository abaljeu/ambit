# Research: Prolog control mapped to this language

Type: research
Status: resolved
Blocked by: none

## Question

From established Prolog facts (no websearch): which of conjunction, disjunction, negation-as-failure, `findall`/`bagof`, and backtracking map cleanly onto a Graph Node/text/number Answer language, and which should stay fog?

## Answer

Conjunction, disjunction, and negation-as-failure map as control over an Answer stream. Backtracking is that stream. `findall`-shaped collection is a context rule, not a first primitive. Cut, if-then, unification, and `bagof` grouping stay fog. Findings: [[plan/expression-language/reports/prolog-control-mapping.md]].
