# 24 — Retire Amble prefix juxtaposition and legacy Run paths

**Context:** The pipeline is the single query surface. Amble prefix `FunCall` juxtaposition, `of`, and legacy Run entry points must not remain as a parallel eval model once consumers and combinators land. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** Remove or gate legacy Amble prefix forms so only the spec pipeline evaluates. Prefix `text Ref` and similar old orders become type errors or are removed from the parse surface. Drop `of` and comma-as-`FunCall` sugar (comma is `OR` in Expressions). Stop evaluating bare anchored RefExpr lines in Run; only `=` and `Name=` run. Ensure `Ref text` postfix order is the supported spelling where `text` is later enabled.

**Blocked by:** [[.scratch/expression-language/issues/21-run-consumer-equals-and-name-equals-statements.md]], [[.scratch/expression-language/issues/22-search-and-move-consumer-leading-equals.md]], [[.scratch/expression-language/issues/23-and-or-not-and-comma-combinators.md]].

**See also:** [[.scratch/expression-language/reports/amble-refexpr-seams.md]]; [[.scratch/expression-language/spec.md]] chapter 9 divergence 6.

**Status:** ready-for-agent

- [ ] Amble prefix `text #todo` (and similar prefix orders) no longer evaluate; they fail as type or parse errors per spec.
- [ ] `of` is not accepted in Expression text; comma in an Expression means `OR`, not `FunCall`.
- [ ] Run no longer evaluates a bare RefExpr line without `=`; legacy redletter error Children for that path are gone.
- [ ] No code path still uses a second juxtaposition eval model beside the catalog pipeline.
