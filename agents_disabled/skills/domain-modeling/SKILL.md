---
name: domain-modeling
description: Build and sharpen a project's domain model. Use when the user wants to pin down domain terminology or a ubiquitous language, record a Committed Decision, or when another skill needs to maintain the domain model.
---

# Domain Modeling

Actively build and sharpen the project's domain model as you design. This is the *active* discipline ( challenging terms, inventing edge-case scenarios, and writing the glossary and decisions down the moment they crystallise. (Merely *reading* [[GLOSSARY.md]] for vocabulary is not this skill ) that's a one-line habit any skill can do. This skill is for when you're changing the model, not just consuming it.)

This skill writes [[GLOSSARY.md]]. Hard choices that are costly to reverse, surprising without context, and made between genuine alternatives are Committed Decisions under [[doc/Decisions/]]. Layout, lazy create, and single-vs-multi-context: [[CONTEXT-FORMAT.md]].

## During the session

### Challenge against the glossary

When the user uses a term that conflicts with the existing language in [[GLOSSARY.md]], call it out immediately. "Your glossary defines 'cancellation' as X, but you seem to mean Y — which is it?"

### Sharpen fuzzy language

When the user uses vague or overloaded terms, propose a precise canonical term. "You're saying 'account' — do you mean the Customer or the User? Those are different things."

### Discuss concrete scenarios

When domain relationships are being discussed, stress-test them with specific scenarios. Invent scenarios that probe edge cases and force the user to be precise about the boundaries between concepts.

### Cross-reference with code

When the user states how something works, check whether the code agrees. If you find a contradiction, surface it: "Your code cancels entire Orders, but you just said partial cancellation is possible — which is right?"

### Update GLOSSARY.md inline

When a term is resolved, update [[GLOSSARY.md]] right there. Don't batch these up — capture them as they happen. Use the format in [CONTEXT-FORMAT.md](./CONTEXT-FORMAT.md).

[[GLOSSARY.md]] should be totally devoid of implementation details. Do not treat [[GLOSSARY.md]] as a spec, a scratch pad, or a repository for implementation decisions. It is a glossary and nothing else.

### Offer Committed Decisions sparingly

Offer a Committed Decision only when the three criteria in [[COMMITTED-DECISION-FORMAT.md]] hold. Write it under [[doc/Decisions/]] with sequential numbering (`0001-slug.md`). Format and optional sections: [[COMMITTED-DECISION-FORMAT.md]](./COMMITTED-DECISION-FORMAT.md).

A project's Out of scope section is **scope**, not a Committed Decision. Do not promote inferred exclusions to Committed Decisions or `doc/` without human confirmation. See [[doc/agents/scope-vs-commitment.md]].
