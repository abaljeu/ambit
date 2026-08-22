# 06 — Recovery safety decisions (Kind 4 + orphan)

**Context:** Kind 4 delete-against-edit recovery (`deleted` Owned wrapper; possible Change baseline + history scan) and hard Orphaning / orphan-collection policy are still proposed or open. Deciding late after wire and log retention freeze would force painful rework. This ticket is decision/prototype only; implementing recovery is a follow-on after accept.

**What to build:** A decision (and light prototype if needed) that (a) accepts, revises, or rejects the tentative `deleted` Owned-wrapper recovery and whether a Change must carry an explicit baseline for history scan; (b) names orphan-collection policy versus proving hard Orphaning cannot arise. Do not implement production recovery in this ticket. Record the decision in project decision/open-question docs.

**Blocked by:** 03 — Server amends recoverable field collisions (text, name, classes), 04 — Client consumes merge success without reload

**See also:** [[../details/conflict-resolution.md]], [[../details/open-questions.md]]

**Status:** ready-for-agent

- [ ] Kind 4 wrapper recovery is explicitly accepted, revised, or rejected, including whether Changes need an explicit baseline for history scan.
- [ ] Orphan-collection policy versus “hard Orphaning cannot arise” has a written decision.
- [ ] No production recovery implementation is claimed done; follow-on work is named only after accept.
