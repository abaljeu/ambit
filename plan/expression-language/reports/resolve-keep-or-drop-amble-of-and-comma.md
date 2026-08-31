# Resolve Keep or drop Amble of and comma

Recorded the HITL lock on [[plan/expression-language/issues/11-keep-or-drop-amble-of-and-comma.md|Keep or drop Amble of and comma]]. Status is resolved.

## Changed

- Ticket Answer: drop `of`; drop Amble comma-as-`FunCall`; comma stays `OR`. `sort 3,5,2` is not defined.
- [[doc/roadmap/language-syntax-and-semantics.md]] examples reworked to postfix pipeline. Shell examples unchanged. `of` removed from EBNF. Functions are postfix; `child` not `children`.
- [[plan/expression-language/issues/03-first-primitive-catalog.md|First primitive catalog]]: `of` dropped from later-word list.
- Map [[plan/expression-language/map.md]] Decisions so far: 11 gist added.
- [[plan/expression-language/spec-draft.md]] catalog out-of-slice: `of` dropped.

## Frontier

No open expression-language decision tickets. Fog remains on the map (unification, cut, aggregations, Unloaded walks, number-returning functions, shell, quoted path segments).
