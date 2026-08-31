# Statements in this spec

Type: grilling
Status: resolved
Blocked by: none

## Question

Which of `= Expression`, `#ident = Expression`, Amble `Name = Expr`, and `> shell` belong in this spec, and which wait?

Recommended answer (HITL confirm): include assignment-to-current-node and named assignment as consumers of Expression Answers. Leave shell `> …` as considered-but-later (still in destination; not specified in the first catalog). Do not move statements to Out of scope.

[[06-top-level-context-node-versus-text.md]] locked assignment as the Run materialise (plus optional rename). This ticket is which statement syntax belongs in the first spec.

## Answer

HITL 2026-08-27.

**In this spec**

- `= Expression` — Run evals the Expression and materialises Answers as in [[06-top-level-context-node-versus-text.md]].
- `Name=Expression` — the same as `= Expression`, plus rename of the current Node.

**Not in this spec:** `>` shell.

**Rejected:** `#ident = Expression`. `#` is a path term, not an assignment name.

Bare Expression is forbidden as a Run statement. A line that is not `=` / `Name=` form: Run does nothing.

A valid `=` / `Name=` whose Expression yields 0 Answers or a type error: one blueletter Child `No matches found` (revises 06 redletter for this case). If Run writes Children, unfold that Node.

**Search and Move:** a leading `=` evals a Node Expression locally (zoomRoot, pick, as 06). No leading `=`: today’s word search. `Name=` is Run-only. This revises 06: the language matcher is opt-in with `=`. All eval is local; server postponed: [[14-server-side-search.md]].
