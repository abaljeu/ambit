# First primitive catalog

Type: grilling
Status: resolved
Blocked by: none

## Question

Which short words and symbols are in the first catalog? The catalog must include generators and filters analogue to ROOT, descendant, containing (text), and tagged, plus boolean control operators. Extra functions stay fog.

Recommended answer (HITL confirm): a small closed catalog:

- `root` — generator; short form `//` (ROOT)
- `descendant` — generator from left-hand Nodes
- `containing` — filter; argument is a quoted string matched against Node text (and name, if HITL agrees)
- `tagged` — filter; argument is a name; short form `#name` when used as a path term
- conjunction — default pipeline composition (space); do not reuse Amble `,` (that comma concatenates)
- `or` — disjunction; optional short form `;`
- `not` — negation-as-failure

Keep `text`, `name`, `children`, `of`, and `sort` out of this closed set until later tickets graduate them.

Fixity is locked on [[plan/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md]]: anchors, postfix, and infix; `text` is postfix (`Ref text`); space only lexes.

Path operators are locked on [[plan/expression-language/issues/02-path-references-as-pipeline-terms.md|Path references as pipeline terms]]: each is one function of its left-hand Nodes; `#x` searches down; `/` keeps Directory Node and Workspace Node; `**` is the same idea as `descendant`.

## Answer

HITL 2026-08-27. Closed first catalog of words:

- `root` — generator; short form `//` (ROOT).
- `child` — generator from left-hand Nodes; finds Children (Owned and Ref). Same set as `:*`.
- `descendant` — closure of `child` (follows Ref). Not `**`.
- `tree` — transitively Owned Nodes; acyclic; does not follow Ref. Short form `**`. `// tree` is that walk from ROOT.
- `containing` — infix or postfix filter; argument is a quoted string; matches Header text only (not name).
- `named` — not `tagged`; finds Normal Nodes with that name; short form `#x`. Same glob: `named "re*ed"` and `#re*ed`.
- `NOT` — negation-as-failure. `(left) NOT #x` and `(left) NOT named x` have the natural meaning. Letter-case is locked on [[04-boolean-operators-as-control.md]].
- `AND` — same-left intersection of two predicates.
- `OR` — disjunction. Comma `,` has the same meaning. Both are reserved.

Combinators:

- `x f g` is composition (left-associative juxtaposition). It is not boolean.
- `x (f, g)` is concatenation, which is `OR`: all Answers of `x f` plus all Answers of `x g`. Example: `#x , #y` (or `#x OR #y`) yields subnodes named `x` or named `y`.
- `(left) containing "the" AND named "blue"` is `AND` on the same left.
- Composition of two pure filters happens to equal `AND` (`left containing "the" named "blue"` versus `… AND …`). If an operand is not a pure filter (for example the generator `descendant`), composition and `AND` differ. They are not interchangeable.
- Answer sequence (order, duplicates) is locked on [[05-how-multiple-answers-surface.md]]: `OR` concatenates and may repeat; `AND` is left-predicate order, at most once.

Path symbols already locked on Path references as pipeline terms stay in the language: `//`, `^`, `.`, `/`, `#`, `*`, `**`, `:n`, `!n`, `:*`, `!*`. This ticket does not re-open them. `/` is not a prefix. `**` is `tree`, not `descendant`. Walk words `child` / `descendant` / `tree`: [[12-owned-versus-ref-walk-for-descendant.md|Owned versus Ref walk for descendant]].

Out of this closed word set until later tickets: postfix `text` (exists from [[plan/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md|Pipeline versus Amble juxtaposition]] but not this catalog slice), `name`, `sort`. `of` is dropped ([[plan/expression-language/issues/11-keep-or-drop-amble-of-and-comma.md|Keep or drop Amble of and comma]]).

No Boolean Answer type. A miss is 0 Answers (Prolog-style), already locked.

HITL 2026-08-28 amendment. `#` is not the short form of `named`. `#` is subsection search (spoken spelling `subsection`; `subsection "todo"` equals `#todo`). `named` remains the name-glob pure filter. `section` is the zero-argument pure filter “is a named Normal Node”.
