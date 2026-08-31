# 24 — Retire Amble prefix juxtaposition and `of`

**Context:** The pipeline is the query surface. Amble prefix `FunCall` juxtaposition and `of` must go. The Run command ([[src/Shared/CommandEntry.fs]] `Exec`) stays. A bare RefExpr line such as `//x/y` is not a Run statement. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** Remove Amble prefix `FunCall` juxtaposition so `text Ref` and similar old orders fail as type or parse errors per spec. Drop `of`. Drop Amble comma-as-`FunCall` sugar. Comma-as-`OR` is ticket 23, after this surface is clean. Where postfix `text` is later enabled, the spelling is `Ref text`. Run accepts only `= Expression` and `Name=Expression`; a line that is not that form, including bare `//x/y`, does nothing. Do not unwire the Run command. Do not change `>` shell.

**Blocked by:** none. Do this before [[plan/expression-language/issues/23-and-or-not-and-comma-combinators.md]].

**See also:** [[plan/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md]]; [[plan/expression-language/issues/07-statements-in-this-spec.md]]; [[plan/expression-language/issues/11-keep-or-drop-amble-of-and-comma.md]]; [[plan/expression-language/spec.md]] chapter 9 divergences 6 and 8.

**Status:** done

- [x] Amble prefix `text #todo` (and similar prefix orders) no longer evaluate; they fail as type or parse errors per spec.
- [x] `of` is not accepted.
- [x] Amble comma-as-`FunCall` sugar is gone.
- [x] Run on a bare RefExpr such as `//x/y` does nothing; only `=` and `Name=` statements evaluate.
- [x] The Run command remains; `>` is unchanged.

## Comments

HITL 2026-08-28. The Run command is not retired. Ticket 07 locked Expression statement syntax: `Name=Expression` and `= Expression`. Shell `>` was out of discussion.

HITL 2026-08-28 (correction). Amble prefix juxtaposition and `of` are retired (spec chapter 9 divergence 6).

HITL 2026-08-28 (correction). Run on a Node whose text is a bare RefExpr such as `//x/y` is retired, because only `=` statements are accepted (spec chapter 9 divergence 8). That is not retirement of the Run command.

HITL 2026-08-28. Do this before ticket 23 so combinators land after prefix `FunCall`, `of`, and comma-as-`FunCall` are gone.
