# Chart the map

Map: [[plan/expression-language/map.md]]. Working draft: [[plan/expression-language/spec-draft.md]]. Branch stayed `w/broken`.

## Tickets

| Title | Type | Blocked by | Status |
| --- | --- | --- | --- |
| [[plan/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md]] Pipeline versus Amble juxtaposition | grilling | none | open |
| [[plan/expression-language/issues/02-path-references-as-pipeline-terms.md]] Path references as pipeline terms | grilling | none | open |
| [[plan/expression-language/issues/03-first-primitive-catalog.md]] First primitive catalog | grilling | Pipeline versus Amble juxtaposition; Path references as pipeline terms | open |
| [[plan/expression-language/issues/04-boolean-operators-as-control.md]] Boolean operators as control | grilling | Pipeline versus Amble juxtaposition | open |
| [[plan/expression-language/issues/05-how-multiple-answers-surface.md]] How multiple answers surface | grilling | none | open |
| [[plan/expression-language/issues/06-top-level-context-node-versus-text.md]] Top-level context: Node versus text | grilling | How multiple answers surface | open |
| [[plan/expression-language/issues/07-statements-in-this-spec.md]] Statements in this spec | grilling | none | open |
| [[plan/expression-language/issues/08-prototype-pipeline-examples.md]] Prototype: pipeline examples | prototype | none | open (artifact written) |
| [[plan/expression-language/issues/09-research-prolog-control-mapped.md]] Research: Prolog control mapped to this language | research | none | resolved |
| [[plan/expression-language/issues/10-research-amble-refexpr-seams.md]] Research: existing Amble and RefExpr seams the spec must not contradict | research | none | resolved |

Prototype artifact: [[plan/expression-language/reports/pipeline-examples.md]].

## Research resolved

Research: Prolog control mapped to this language — conjunction, disjunction, and negation-as-failure map as control; collection is a context rule; cut, unification, and `bagof` stay fog. [[plan/expression-language/reports/prolog-control-mapping.md]]

Research: existing Amble and RefExpr seams the spec must not contradict — empty miss is fail-to-answer; pipeline space must share operators with Amble juxtaposition; Find AND is not the pipeline; `**` Owned-only versus other steps following Ref. [[plan/expression-language/reports/amble-refexpr-seams.md]]

## CONTEXT.md

Added **Expression** and **Answer** under About the Software. Find (the command) is unchanged. No other glossary edits.

## Frontier

Open, unblocked, unclaimed: Pipeline versus Amble juxtaposition; Path references as pipeline terms; How multiple answers surface; Statements in this spec; Prototype: pipeline examples. First by number: Pipeline versus Amble juxtaposition.
