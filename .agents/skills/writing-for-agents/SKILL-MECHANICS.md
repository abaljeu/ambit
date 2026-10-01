# Skill mechanics

The skill-specific branch of [[SKILL.md]]: frontmatter, the invocation choice, and router skills. Terms: [[GLOSSARY.md]]. Writing theory: [[WHY.md]].

## Invocation

[[GLOSSARY.md]] defines **Model-Invoked**, **User-Invoked**, and **Description**. Frontmatter:

- Model-invoked: omit `disable-model-invocation`; write a model-facing `description` carrying the trigger branches (pointer-writing rules in [[SKILL.md]] apply in full).
- User-invoked: set `disable-model-invocation: true`; `description` is human-facing — a one-line summary, trigger lists stripped.

Pick model-invocation only when the agent must reach the skill on its own, or another skill must. Otherwise user-invoked.

Shared reference that two user-invoked skills both need: [[GLOSSARY.md]] **External Reference** — a plain file outside the skill system any skill can point at.

## Splitting by invocation

The invocation cut of splitting (the sequence cut lives in [[WHY.md]]): split off a model-invoked skill when you have a distinct leading word that should trigger it on its own — a trigger word you actually use in your prompts — or another skill must reach it. You pay context load for the new always-loaded description, so that independent reach has to be worth it.

## Router skills

[[GLOSSARY.md]] **Router Skill**. One user-invoked skill that names the others and when to reach for each.
