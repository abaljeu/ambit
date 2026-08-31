# 33 — Recognize `?` as a Run statement

**Context:** Run accepts only `=` Expression and `Name=Expression`; any other line does nothing. Ask from what I see needs a third form `?` plus a message. Pack, LLM call, and write-back belong to [[.scratch/llm-connector/project.md]]. Do not implement those here.

**What to build:** Recognize a focus line that starts with `?` as a Run statement. The message is the rest of the line. Do not parse the message as Expression. Do not call an LLM. Do not assemble included context. Do not write Owned children for a reply.

**Blocked by:** none.

**See also:** [[../spec.md]] chapter 8; [[21-run-consumer-equals-and-name-equals-statements.md]]; [[.scratch/llm-connector/map.md]]; [[.scratch/roadmap/epics/agent-chat-managed-context.md]].

**Status:** ready-for-agent

- [ ] A focus line `?` plus a message is a Run statement, not a no-op.
- [ ] `=` and `Name=` are unchanged.
- [ ] The message is not parsed as Expression.
- [ ] No LLM call, no included-context pack, no reply Children from this ticket.
