# Design It Twice

When the user wants to explore alternative interfaces for a module — including a chosen deepening candidate, or when they say "design it twice" — use this parallel sub-agent pattern. Based on "Design It Twice" (Ousterhout) — your first idea is unlikely to be the best.

Uses the vocabulary in [SKILL.md](SKILL.md) — **module**, **interface**, **seam**, **adapter**, **leverage**.

## Process

### 1. Frame the problem space

Before spawning sub-agents, write a user-facing explanation of the problem space:

- What problem the module solves, who the callers are, and the key operations
- What should be hidden inside vs exposed
- The constraints any new interface would need to satisfy
- The dependencies it would rely on, and which category they fall into when deepening (see [DEEPENING.md](DEEPENING.md))
- A rough illustrative code sketch to ground the constraints — not a proposal, just a way to make the constraints concrete

Show this to the user, then immediately proceed to Step 2. The user reads and thinks while the sub-agents work in parallel.

### 2. Spawn sub-agents

Spawn 3+ sub-agents in parallel using the Agent tool. Each must produce a **radically different** interface for the module.

Prompt each sub-agent with a separate technical brief (file paths, coupling details, dependency category from [DEEPENING.md](DEEPENING.md) when known, what sits behind the seam). The brief is independent of the user-facing problem-space explanation in Step 1. Give each agent a different design constraint:

- Agent 1: "Minimize the interface — aim for 1–3 entry points max. Maximise leverage per entry point."
- Agent 2: "Maximise flexibility — support many use cases and extension."
- Agent 3: "Optimise for the most common caller — make the default case trivial."
- Agent 4 (if applicable): "Design around ports & adapters for cross-seam dependencies."

Include both [SKILL.md](SKILL.md) vocabulary and CONTEXT.md vocabulary in the brief so each sub-agent names things consistently with the architecture language and the project's domain language.

Each sub-agent outputs:

1. Interface (types, methods, params — plus invariants, ordering, error modes)
2. Usage example showing how callers use it
3. What the implementation hides behind the seam
4. Dependency strategy and adapters (see [DEEPENING.md](DEEPENING.md)) when relevant
5. Trade-offs — where leverage is high, where it's thin

### 3. Present and compare

Present designs sequentially so the user can absorb each one, then compare them in prose. Contrast by **depth** (leverage at the interface), **locality** (where change concentrates), **seam placement**, ease of correct use vs ease of misuse, and whether the shape allows efficient internals.

After comparing, give your own recommendation: which design you think is strongest and why. If elements from different designs would combine well, propose a hybrid. Be opinionated — the user wants a strong read, not a menu.

Ask which design fits the primary use case and whether any elements from other designs are worth incorporating.

## Anti-patterns

- Do not let sub-agents produce similar designs — enforce radical difference
- Do not skip comparison — the value is in contrast
- Do not implement — this is about interface shape
- Do not evaluate based on implementation effort alone
