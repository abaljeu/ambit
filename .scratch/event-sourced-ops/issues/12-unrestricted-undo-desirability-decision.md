# 12 — Unrestricted Undo desirability (decision only)

**Context:** Whether Actors may Undo arbitrary merged Changes in global order is open on purpose. Cancel-is-not-Undo is accepted. Ticket 04 must leave History extensible (not own-posts-only). This ticket answers the open question; it invents no Undo protocol.

**What to build:** A written decision answering whether unrestricted (cross-Actor / global-order) Undo is desirable now, or whether Undo stays process-local. Decision/prototype only — invent no Undo protocol until answered. Record the answer under project open-question / decision docs.

**Blocked by:** 04 — Client consumes merge success without reload

**See also:** [[../details/undo.md]], [[../details/open-questions.md]]

**Status:** ready-for-agent

- [ ] The open question “is unrestricted Undo desirable?” has an explicit yes/no (or deferred-with-rationale) answer in project docs.
- [ ] No Undo protocol or wire change is shipped by this ticket.
- [ ] Cancel-is-not-Undo remains undisturbed.
