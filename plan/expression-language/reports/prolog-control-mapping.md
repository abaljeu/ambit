# Prolog control mapped to this language

Research note for [[plan/expression-language/issues/09-research-prolog-control-mapped.md]]. Sources are established Prolog facts (ISO core: conjunction, disjunction, negation-as-failure, `findall`/`bagof`, backtracking). No websearch. Destination vocabulary: Expression, Answer, Graph, Node.

## What maps cleanly

**Conjunction.** In Prolog, `,` sequences goals left to right. The conjunction succeeds only if every goal succeeds. Failure backtracks to the last choice point. That maps to default pipeline composition: each word consumes Answers from the left and emits Answers to the right. A filter that matches nothing fails that branch. Do not reuse the character `,` for this; Amble already uses `,` to concatenate values ([[doc/roadmap/language-syntax-and-semantics.md]]).

**Disjunction.** Prolog `;` (and separate clauses) offers an alternative if the left goal fails, or yields further Answers on backtracking. That maps to an explicit disjunction operator over Answer streams (`or` or `;`). It is control: it does not produce a Boolean Answer.

**Negation-as-failure.** Prolog `\+ Goal` succeeds when `Goal` has no solution. It is not classical negation. Unbound variables inside `\+` can flounder. That maps to `not` in front of a filter or sub-expression: succeed when the inner Expression yields no Answer. Floundering (negation of an under-bound goal) stays fog until variable binding is specified.

**Backtracking versus a stream.** Prolog presents one solution at a time. The destination already says an Expression is non-deterministic and has many possible Answers. A stream of Answers is the same idea. One-at-a-time versus collected list is a consumer choice, not a change of Expression meaning.

**Succeed and fail as control.** Predicates do not return `true`/`false` values. They succeed with bindings or they fail. The destination already rejects Boolean Answers. Keep fail = no Answer on that branch; succeed = one or more Answers.

## What should stay fog

**`findall` / `bagof` / `setof`.** `findall(Template, Goal, List)` always succeeds and collects every instantiation (empty list if none). `bagof` fails if there are no solutions and backtracks over free variables that are not existentially quantified. `setof` is sorted unique `bagof`. Collecting a list is a context rule (Run materialises Children; display shows text), not a first-catalog primitive. `bagof` grouping by free variables needs variable binding, which is still fog.

**Cut and if-then.** `!` commits and prunes choice points. `->` / `;` if-then-else is cut in disguise. The map already parks cut and if-then in Not yet specified.

**Unification and logical variables.** Prolog Answers are bindings. This language's first Answers are Node, text, or number values, not a substitution. Binding and unification stay fog.

**A full Prolog system.** Clause databases, ISO built-ins, DCGs, and modules are out of this destination.

## Implication for the spec

Write boolean operators as control over an Answer stream. Keep collection (`findall`-shaped) as a context rule. Leave cut, if-then, unification, and `bagof` grouping unspecified.
