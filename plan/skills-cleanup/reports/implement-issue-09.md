# Implement issue 09 — one glossary; Committed Decision

[[.agents/skills/domain-modeling/SKILL.md]] writes [[CONTEXT.md]] and records hard choices as Committed Decisions under [[doc/Decisions/]]. It does not say ADR. [[.agents/skills/ubiquitous-language/SKILL.md]] proposes terms in conversation and hands recording to domain-modeling. It does not create `UBIQUITOUS_LANGUAGE.md`. Product F# was not touched. [[CONTEXT.md]] lists were not rewritten (ticket 03). Ask-Matt and gambol.mdc were not edited (ticket 12). improve-codebase-architecture was not edited (ticket 08).

## What changed

- domain-modeling: lazy create of [[doc/Decisions/]] is for the first Committed Decision. The offer section uses that name and an inline short template. The skill no longer points at [[.agents/skills/domain-modeling/ADR-FORMAT.md]].
- ubiquitous-language: description and process name [[CONTEXT.md]] as the glossary. Conversation output stays a proposal. Accepted terms go through domain-modeling. Re-run reads [[CONTEXT.md]].
- Ticket [[plan/skills-cleanup/issues/09-one-glossary-committed-decision.md]] is `Status: done`.
- Project Stage stays `build`. [[plan/index.md]] was not edited. This commit stages only the four owned paths.

## How verified

- `rg ADR` on [[.agents/skills/domain-modeling/SKILL.md]] is empty.
- ubiquitous-language contains the guardrail not to create UBIQUITOUS_LANGUAGE.md and has no step that writes that file.
- Both skills name Committed Decision for the [[doc/Decisions/]] record.
- Owned paths only: the two SKILL.md files, this ticket, and this report.

## Leftover risks

- [[.agents/skills/domain-modeling/ADR-FORMAT.md]] still says ADR, including optional Status `superseded by ADR-NNNN`. Out of this ticket's exclusive files.
- Ask-Matt, grill-with-docs, and [[doc/agents/domain.md]] still say ADR at HEAD. Tickets 08 and 12. Architecture-review wording was left to ticket 08.
- ubiquitous-language conversation tables still differ from CONTEXT-FORMAT. Recording uses CONTEXT-FORMAT.
- Conversation proposals still use a heading "Ubiquitous Language"; the written glossary remains [[CONTEXT.md]].
