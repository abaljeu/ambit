# Chart chapters for Agent chat with managed context

Type: grilling
Status: resolved
Blocked by: 01

## Question

Name the Chapters of [[plan/roadmap/epics/agent-chat-managed-context.md]] as useful increments of capability. Set Current chapter. Point each Chapter at owning Projects or issues; do not own that work on the Epic file. Pin the User Epic so product language does not collide with glossary Agent (agent-done, DbAgent) or context (ownership ancestry).

Recommended: grill the in-product job first, then name increments breadth-first. Do not create an Epic Project folder. Later tickets chart those pointers onto the Projects that own them.

## Comments

- 2026-08-29 Q1: Inside Gambol. Same family as Run (`CommandId.Exec`, Ctrl+Enter, focus line) but the line is a message to an LLM, not an Expression statement. Run today: [[doc/roadmap/amble-run.md]], [[src/Shared/CommandEntry.fs]].
- 2026-08-29 Q2: Same Run command, third statement form `?` plus a message (example: `? Based on the visible nodes (included context) what should i do next`). Not a sibling command. Spec today: Run is only `=` / `Name=` ([[plan/expression-language/spec.md]] ch. 8).
- 2026-08-29 Q3: Included context is SiteMap rows under Zoom, honoring Fold. Visible is de facto speech, not glossary. Unloaded Children stay out of the pack. Term: [[CONTEXT.md]] Included context.
- 2026-08-29 Q4: Reply is Owned child Nodes under the focus Node, then unfold; errors as one child — same as Run Text Answers.
- 2026-08-29 Q5: Five Chapters, in order: (1) Ask from what I see, (2) Talk again, (3) Change the Graph, (4) Queries to the Graph or the files behind, (5) Agent work via CLI or MCP. Names still to pin as person-jobs.
- 2026-08-29 Q6, ownership corrected 2026-09-05: New feature-set Project owns pack, LLM call, and write-back. Expression-language only recognizes `?`. Long-running launch, apply, identity, and cancel belong to [[plan/core-creation/project.md]]: [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]], [[plan/core-creation/issues/02-core-actor-pool.md]]. ESO retains [[plan/event-sourced-ops/details/actors-and-jobs.md]] as background and owns advisory soft-lock behavior.
- 2026-08-29 Q7: Chapter names: Ask from what I see; Talk again; Change the Graph; Query the Graph or the files; Act through CLI or MCP.
- 2026-08-29 Q8, ownership corrected 2026-09-05: Long-running work for Ask from what I see depends on the Core Actor spine. CLI/MCP stays Chapter 5.

## Answer

Chapters named on [[plan/roadmap/epics/agent-chat-managed-context.md]]. Current chapter: Ask from what I see. First increment is Run `?` with included context, Owned-child replies, long-running Actor. New feature-set Project (not created this session): [[05-create-ask-from-what-i-see-project.md]]. Expression-language recognizes `?` only. Glossary: [[CONTEXT.md]] Included context. Do not say Agent for the LLM or for CLI/MCP work.
