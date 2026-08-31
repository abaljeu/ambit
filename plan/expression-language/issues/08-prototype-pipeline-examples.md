# Prototype: pipeline examples

Type: prototype
Status: resolved
Blocked by: none

## Question

Do the example Expressions, Answer kinds, and one-line meanings match the intended pipeline, including path-plus-pipeline mixes and one statement? HITL reacts to the artifact; this ticket stays open until that reaction.

Recommended answer (HITL confirm): treat the page as a cheap reaction surface for the recommended answers on the grilling tickets, not as locked syntax.

[[.scratch/expression-language/reports/pipeline-examples.md]]

[[07-statements-in-this-spec.md]] locked `= Expression` and `Name=Expression`. Bare Expression is not a Run statement. The reaction page now has those statement rows.

## Answer

HITL 2026-08-27. The examples match the intended pipeline, with three corrections:

- `//ws/x` can match a Workspace Node or a Directory Node named `ws`.
- `#blue` is correct given the `#` search rules and Node tree locked elsewhere; this page does not restate those details.
- `descendant` walks Owned Nodes only; same as `**`.

The page stays a cheap reaction surface, not locked syntax.

## Amendment

HITL 2026-08-27, later the same day.

- `// OR /` is undefined: `/` is not a prefix.
- `3` is a type error. A number is only valid as the right operand of `:` or `!`.
- Walk words: `child` finds Children (Owned and Ref). `descendant` is the closure of `child`. `tree` / `**` is acyclic and does not follow Ref; `// tree` is transitively Owned Nodes. No Directory/Workspace stop. This revises the earlier Owned-only / same-as-`**` line for `descendant`. Detail: [[12-owned-versus-ref-walk-for-descendant.md]].

[[.scratch/expression-language/reports/pipeline-examples.md]]
