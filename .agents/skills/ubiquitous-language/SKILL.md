---
name: ubiquitous-language
description: Extract domain terms from the current conversation, flag ambiguities, and propose canonical terms. The glossary is CONTEXT.md; domain-modeling writes it.
disable-model-invocation: true
---

# Ubiquitous Language

Extract and formalize domain terminology from the current conversation. Propose a canonical glossary in the conversation. [[CONTEXT.md]] is the one glossary; [[.agents/skills/domain-modeling/SKILL.md]] writes it. Hard choices are Committed Decisions under [[doc/Decisions/]], recorded by domain-modeling.

## Process

1. **Scan the conversation** for domain-relevant nouns, verbs, and concepts.
   Done: every domain-relevant term in the conversation is listed for the next steps.
2. **Identify problems**: same word for different concepts (ambiguity); different words for the same concept (synonyms); vague or overloaded terms.
   Done: each problem is named with the conflicting uses.
3. **Read [[CONTEXT.md]]** if it exists — that file is the glossary.
   Done: existing terms are loaded, or it is confirmed that no glossary file exists yet.
4. **Propose a canonical glossary** with opinionated term choices.
   Done: every scanned term has a proposed canonical form or is deferred with a reason.
5. **Output a summary** in the conversation using the format below.
   Done: the conversation shows the full proposal structure (tables, relationships, dialogue, flagged ambiguities).
6. **Record accepted terms** by following [[.agents/skills/domain-modeling/SKILL.md]] into [[CONTEXT.md]].
   Done: every term the user accepted is written via domain-modeling; the proposal remains conversation-only until then.

## Output Format

Show this structure in the conversation. It is a proposal, not a file to write.

```md
# Ubiquitous Language

## Order lifecycle

| Term        | Definition                                              | Aliases to avoid      |
| ----------- | ------------------------------------------------------- | --------------------- |
| **Order**   | A customer's request to purchase one or more items      | Purchase, transaction |
| **Invoice** | A request for payment sent to a customer after delivery | Bill, payment request |

## People

| Term         | Definition                                  | Aliases to avoid       |
| ----------- | ------------------------------------------- | ---------------------- |
| **Customer** | A person or organization that places orders | Client, buyer, account |
| **User**     | An authentication identity in the system    | Login, account         |

## Relationships

- An **Invoice** belongs to exactly one **Customer**
- An **Order** produces one or more **Invoices**

## Example dialogue

> **Dev:** "When a **Customer** places an **Order**, do we create the **Invoice** immediately?"
> **Domain expert:** "No — an **Invoice** is only generated once a **Fulfillment** is confirmed. A single **Order** can produce multiple **Invoices** if items ship in separate **Shipments**."
> **Dev:** "So if a **Shipment** is cancelled before dispatch, no **Invoice** exists for it?"
> **Domain expert:** "Exactly. The **Invoice** lifecycle is tied to the **Fulfillment**, not the **Order**."

## Flagged ambiguities

- "account" was used to mean both **Customer** and **User** — these are distinct concepts: a **Customer** places orders, while a **User** is an authentication identity that may or may not represent a **Customer**.
```

When domain-modeling writes accepted terms, use the format in [[.agents/skills/domain-modeling/CONTEXT-FORMAT.md]]. Glossary term rules for that write: [[.agents/skills/domain-modeling/CONTEXT-FORMAT.md]] Rules.

## Proposal rules

- **Flag conflicts explicitly.** Ambiguous uses go in "Flagged ambiguities" with a clear recommendation.
- **Show relationships.** Bold term names; express cardinality where obvious.
- **Write an example dialogue.** 3–5 exchanges between a dev and a domain expert that demonstrate the terms interacting and clarify boundaries.

<example>

## Example dialogue

> **Dev:** "How do I test the **sync service** without Docker?"

> **Domain expert:** "Provide the **filesystem layer** instead of the **Docker layer**. It implements the same **Sandbox service** interface but uses a local directory as the **sandbox**."

> **Dev:** "So **sync-in** still creates a **bundle** and unpacks it?"

> **Domain expert:** "Exactly. The **sync service** doesn't know which layer it's talking to. It calls `exec` and `copyIn` — the **filesystem layer** just runs those as local shell commands."

</example>

## Re-running

When invoked again in the same conversation:

1. Read [[CONTEXT.md]].
   Done: current glossary terms are loaded.
2. Incorporate any new terms from subsequent discussion.
   Done: every new domain term since the last run is in the proposal set.
3. Update definitions if understanding has evolved.
   Done: changed definitions match the latest settled meaning.
4. Re-flag any new ambiguities.
   Done: new conflicts appear under Flagged ambiguities.
5. Rewrite the example dialogue to incorporate new terms.
   Done: the dialogue uses the updated term set.
6. Record accepted updates through [[.agents/skills/domain-modeling/SKILL.md]].
   Done: every newly accepted term is written via domain-modeling.
