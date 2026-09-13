# to-tickets: what / why / how

Feedback only. No skill or ticket rewrite. New tickets only if Alan adopts a shape.

## Current ticket mapped

The published shape is [[.agents/skills/to-tickets/PUBLISH.md]]. Style pointer: [[.agents/skills/wait-what/SKILL.md]].

| Part | Cut | What it actually is |
| --- | --- | --- |
| Status, Blocked by, file path | WHERE | Graph of work. The path is the artifact location. |
| Context | WHERE | Situation: who, before the new behaviour. Product why already leaks in (see [[plan/core-creation/issues/21-client-shows-lock-present.md]]). |
| What to build (prose) | WHAT | End-to-end behaviour from the person's view. |
| Criteria checkboxes | WHAT done-bar | Checkable. They become HOW when they name files, types, or layer steps. |
| See also | WHERE | Pointers to spec and decisions. Product why already lives there. |
| Comments | history | Session locks. Not a method file. |
| Prototype snippet | HOW, rare | Allowed only when a prototype encoded a decision better than prose. |

The template already bans a layer-by-layer list. wait-what repeats that ban.

## Where HOW lives today

Not on the ticket, by design.

- Act: [[.agents/skills/implement/SKILL.md]] plus [[.agents/skills/tdd/SKILL.md]].
- Project locked shape: spec Implementation Decisions ([[.agents/skills/to-spec/SKILL.md]]), plus [[doc/Decisions/]].
- Skill method for publishing: [[.agents/skills/to-tickets/PUBLISH.md]] (already split from both ticket skills).
- Failure exhibit: [[plan/core-creation/issues/29-prove-testactor-hello.md]] kept the headings, then put numbered layer sections, file paths, and CoreMsg cases under What to build.

## Separate files vs omitted HOW

A sibling WHY.md and HOW.md per ticket is too heavy.

Skills earn SKILL vs method vs WHY because the recipe is re-read every run and method churns independently. A ticket is a thing consumed once by implement. Writing HOW at publish time is a plan before the tracer teaches anything.

Named sections with HOW omitted matches the existing intent. Do not add a How heading. An empty How heading invites filling it.

New process → new tickets in the new shape. Not a new three-file family. [[.agents/rules/no-retrofit.md]] already keeps old issues as they are.

## Risks if HOW is added

- Stale how: [[.agents/skills/to-spec/SKILL.md]] already forbids file paths for that reason.
- Tickets become implementation plans. Issue 29 is that ticket.
- Tracer-bullet and wait-what: HOW on the ticket is a horizontal list. Feature tickets ([[.agents/skills/to-feature-tickets/SKILL.md]]) will leak module steps even faster.
- Duplicate spec: Implementation Decisions, ticket How, and Comments would all hold the same locks.

## Experimental shape (new tickets only)

Keep one file. Headings:

```
# <NN> — <title>
**Status:** ready-for-agent
**Blocked by:** ...

## Context
## What to build
- [ ] ...

## See also
```

Do not put on the ticket:

- How, Why, or Method headings
- WHY.md or HOW.md siblings
- Layer lists, file paths, implement/tdd steps
- Product why (point at spec or a decision from See also)
- A restatement of the spec

Slice scope ("this increment does not") may stay as one short paragraph under What to build. That is scope, not method.

If a type shape must travel with the slice, use the existing prototype-snippet exception, or point at a decision. Do not open a How section.

## Pushback

The skill-level cut is already in force: SKILL.md drafts slices; PUBLISH.md is the method; wait-what is the style. Applying "wholly separate documents" to each issue fights to-tickets: the ticket is the thing; implement is acting with it. Clone the orientation (WHAT/WHERE on the ticket, why disclosed, HOW omitted). Do not clone the packaging.
