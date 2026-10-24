# Why — writing for agents

Terms: [[GLOSSARY.md]]. Recipe: [[SKILL.md]]. Skill packaging: [[SKILL-MECHANICS.md]].

The packaging of a skill, [[AGENTS.md]], a `.agents/` bridge, or a pointed doc differs. The writing does not. The same levers make the agent take the same process every run, not the same output. **Predictability** is that process.

## Spend the two loads

Every document and pointer you add spends **context load** or **cognitive load**. An always-loaded [[AGENTS.md]] line and a skill description are the same spend: tokens and attention on every turn, whether the pointer fires or not. Material reached only through a **context pointer** escapes context load at the price of the pointer's own line. Material with no pointer rides entirely on cognitive load.

Cognitive load is the price of human agency. Spend it where human judgement matters. Remove it where it does not.

## Protect the ladder

**Progressive disclosure** is how the **information hierarchy** stays legible. It is not a token optimisation. Inline what every **branch** needs. Push behind a pointer what only some branches reach. When a document has **steps**, in-file **reference** that should be disclosed buries them and turns attending to them into a coin-flip — a variance lever, not only a legibility one. Maintainer rationale, theory, and history belong in a sibling file behind a pointer.

**Sprawl** is length itself, even when every line is live. Attention thins across the excess. The cure is the ladder: disclose reference, and split by branch or sequence so each path carries only what it needs.

**Co-location** decides what sits beside a piece once the ladder ranks it. Keep a concept's rules and caveats under one heading. Grouped material reads as documentation written for the agent. Scattered material does not.

## Completion as a lever

Apply the **completion criterion** as a lever: sharpen the bound before you hide **post-completion steps**. A vague bound invites **premature completion**. Hiding works only across a real context boundary — a hand-off or a subagent dispatch. An inline call leaves the later steps in context and clears nothing.

Demand drives **legwork**. "Every modified model accounted for" forces thorough work where "produce a change list" does not. Demand also binds a body of flat reference: "every rule applied" is how an all-reference document still carries an exhaustiveness bar. The strongest criteria are both checkable and exhaustive.

## Split by sequence

Split a run of steps when the later steps tempt the agent to rush the one in front. Keeping them out of view drives more legwork on the current task. Merging sequences exposes each step's later steps and invites premature completion. The invocation cut is [[SKILL-MECHANICS.md]].

## Hunt leading words

A triad spelled out at three sites, or a pointer spending a sentence to gesture at one idea, is a passage that wants a single token:

- "fast, deterministic, low-overhead" → _tight_ (a _tight_ loop)
- "a loop you believe in" → _red_ — a fuzzy gate becomes a binary observable state (the loop goes _red_ on the bug, or it does not)

You win twice: fewer tokens, and a sharper hook for the agent to hang its thinking on. Assume every document carries restatements that **leading words** retire. Go find them.

**Negation** names the elephant into the frame and makes the forbidden behaviour more available. Prompt the positive so the banned behaviour is never spoken. A prohibition earns its place only as a hard guardrail you cannot phrase positively. Pair it with the target so attention lands on what to do.

## Prune

A document that restates the environment is a **cache**. Leave one-file, one-command lookups to the environment, where they cannot go stale. If [[doc/]] or a script already states the fact, leave the why there.

Check every line for **relevance**. Without a pruning discipline the default fate is **sediment**: adding feels safe and removing feels risky.

Hunt **no-ops** sentence by sentence. The test is model-relative: does the sentence change behaviour versus the default? When it fails, delete the whole sentence. The same test grades leading words. A word too weak to beat the default (_be thorough_ when the agent is already thorough-ish) is a no-op. The fix is a stronger word (_relentless_), not a different technique.
