# Pipeline examples (prototype)

Cheap reaction page for [[.scratch/expression-language/issues/08-prototype-pipeline-examples.md]]. Juxtaposition and fixity are locked ([[.scratch/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md|Pipeline versus Amble juxtaposition]]). Other rows are still a HITL reaction surface. Answer kinds: Node, text, or number. Failure is no Answer.

Old Amble prefix `text #todo` is a type error. The valid form is `#todo text`.

| Expression | Answer kind | Meaning |
| --- | --- | --- |
| `root descendant containing "the" named "blue"` | Node stream | Anchor ROOT. Postfix `descendant`. Infix `containing "the"`. Infix `named "blue"`. Left-associative composition. |
| `root descendant containing "the" #blue` | Node stream | Same intent with `#` plus `blue` meaning `named "blue"`. |
| `containing "the" AND named "blue"` | Node stream | `AND` on the same left (current Node). Intersection of Header-text filter and name filter. |
| `#x , #y` | Node stream | Comma is `OR` / concatenation: subnodes named `x` plus subnodes named `y`. Same as `#x OR #y`. |
| `// descendant containing "the"` | Node stream | Path generator ROOT, then postfix and infix. Same start as `root` if `//` is that anchor. |
| `//ws/x` | Node stream | From ROOT, path-search Workspace Node `ws`, then path-search File Node `x` (or Directory Node `x` with `x/`). Not tag search. |
| `#blue` | Node stream | Search down from the current Node for Normal Nodes named `blue`. If Focus is under named `todo`, this finds `blue` under `todo`. |
| `^#blue` | Node stream | From the structural container, search down. Hits named `todo` as a wall and yields no Answer when `blue` sits under `todo`. Use `^#todo#blue` to find `blue`. |
| `:*` | Node stream | Every Child of the current Node. Bare `:` is not defined. |
| `!-249053534` | no Answer | Out-of-range sibling index. Fail-to-answer, not an error. Same for a name / `#` / `/` miss. |
| `root descendant` | Node stream | Every descendant of ROOT (walk rule still fog: Owned versus Ref). |
| `root descendant NOT containing "draft"` | Node stream | Descendants of ROOT that fail the `containing "draft"` filter. |
| `// OR /` | Node stream | ROOT, or postfix `/` on the current Node (keep only if it is a Directory Node or Workspace Node). `/` is not “nearest Workspace”. |
| `#todo text` | text stream | Find Nodes tagged `todo`, then postfix `text` yields each Node’s text. |
| `text #todo` | type error | Old Amble prefix. `text` is postfix, not a prefix function. |
| `root descendant containing root` | type error | `containing` does not take a Node on the right. |
| `3` | number | Number literal. One Answer. |
| `todo = // descendant named "blue"` | statement | Name the current Node `todo`. Consume Node Answers as Children of the current Node (Run-shaped context). |

Path-plus-pipeline mix: a path term yields Nodes; postfix and infix words continue from the left. `**` and `descendant` are the same idea ([[.scratch/expression-language/issues/02-path-references-as-pipeline-terms.md|Path references as pipeline terms]]). Owned versus Ref for that walk is [[.scratch/expression-language/issues/12-owned-versus-ref-walk-for-descendant.md|Owned versus Ref walk for descendant]].
