# Pipeline examples (prototype)

Cheap reaction page for [[.scratch/expression-language/issues/08-prototype-pipeline-examples.md]]. Juxtaposition and fixity are locked ([[.scratch/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md|Pipeline versus Amble juxtaposition]]). HITL 2026-08-27 confirmed the rows, then amended `/`, numbers, and walk words. This page is not locked syntax. Answer kinds: Node, text, or number. Failure is no Answer.

Old Amble prefix `text #todo` is a type error. The valid form is `#todo text`.

| Expression | Answer kind | Meaning |
| --- | --- | --- |
| `root descendant containing "the" named "blue"` | Node stream | Anchor ROOT. Postfix `descendant`. Infix `containing "the"`. Infix `named "blue"`. Left-associative composition. |
| `root descendant containing "the" #blue` | Node stream | Same intent with `#` plus `blue` meaning `named "blue"`. |
| `containing "the" AND named "blue"` | Node stream | `AND` on the same left (current Node). Intersection of Header-text filter and name filter. |
| `#x , #y` | Node stream | Comma is `OR` / concatenation: subnodes named `x` plus subnodes named `y`. Same as `#x OR #y`. |
| `// descendant containing "the"` | Node stream | Path generator ROOT, then postfix and infix. Same start as `root` if `//` is that anchor. |
| `//ws/x` | Node stream | From ROOT, path-search a Workspace Node or Directory Node named `ws`, then path-search File Node `x` (or Directory Node `x` with `x/`). Not tag search. |
| `#blue` | Node stream | Search down from the current Node for Normal Nodes named `blue`. If Focus is under named `todo`, this finds `blue` under `todo`. Correct given the `#` search rules already locked; this page does not restate those details. |
| `^#blue` | Node stream | From the structural container, search down. Hits named `todo` as a wall and yields no Answer when `blue` sits under `todo`. Use `^#todo#blue` to find `blue`. |
| `child` | Node stream | Children of the current Node: Owned and Ref. Same set as the Children model element. Same set as `:*`. |
| `:*` | Node stream | Every Child of the current Node. Bare `:` is not defined. |
| `!-249053534` | no Answer | Out-of-range sibling index. Fail-to-answer, not an error. Same for a name / `#` / `/` miss. |
| `root descendant` | Node stream | Closure of `child` from ROOT (Owned and Ref). |
| `// tree` | Node stream | Transitively Owned Nodes from ROOT. Acyclic; does not follow Ref. Same as `**`. |
| `root descendant NOT containing "draft"` | Node stream | Descendants of ROOT that fail the `containing "draft"` filter. |
| `// OR /` | undefined | `/` is not a prefix. The right side of `OR` has no left Node. |
| `#todo text` | text stream | Find Nodes tagged `todo`, then postfix `text` yields each Node’s text. |
| `text #todo` | type error | Old Amble prefix. `text` is postfix, not a prefix function. |
| `root descendant containing root` | type error | `containing` does not take a Node on the right. |
| `3` | type error | A number is only valid as the right operand of `:` or `!`. |
| `= // descendant named "blue"` | statement | Run: materialise Node Answers as Children. Unfold if Children are written. |
| `todo=// descendant named "blue"` | statement | Same as `= …`, plus rename the current Node `todo`. |

Path-plus-pipeline mix: a path term yields Nodes; postfix and infix words continue from the left. `child` finds Children (Owned and Ref). `descendant` is the closure of `child`. `tree` / `**` is transitively Owned only. `/` is not a prefix. A number is only valid on the right of `:` or `!`. Search-algorithm and Node-tree details for `#` live on [[.scratch/expression-language/issues/02-path-references-as-pipeline-terms.md|Path references as pipeline terms]], not on this page.
