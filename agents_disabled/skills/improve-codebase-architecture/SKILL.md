---
name: improve-codebase-architecture
description: Scan a codebase for deepening opportunities, present them as a Markdown report under plan reports, then grill through whichever one you pick.
disable-model-invocation: true
---

# Improve Codebase Architecture

Surface architectural friction and propose **deepening opportunities** — refactors that turn shallow modules into deep ones. The aim is testability and AI-navigability.

This command is _informed_ by the project's domain model and built on a shared design vocabulary:

- Run `/codebase-design` for the architecture vocabulary and principles. Use those terms exactly in every suggestion.
- The domain language in [[GLOSSARY.md]] gives names to good seams; Committed Decisions under [[doc/Decisions/]] record decisions this command should not re-litigate.

## Process

### 1. Explore

**Scope before you scan — YAGNI.** Deepening a module pays off by making future changes to it easier, so put extra weight on the parts of the codebase that have recently changed. Decide *where* to look before you look:

- If the user named a direction ( a module, a subsystem, a pain point ) take it, and skip the inference below.
- Otherwise, walk back a good stretch of the commit history (`git log --oneline`) to find the codebase's hot spots ( the files and areas that keep coming up ) and let those paths pull your attention first. If the changes are scattered with no clear hot spot, widen the net.

Read the project's domain glossary ([[GLOSSARY.md]]) and any Committed Decisions in the area you're touching first.

Then use the Agent tool with `subagent_type=Explore` to walk the codebase and note where you experience friction:

- Where does understanding one concept require bouncing between many small modules?
- Where are modules **shallow** — interface nearly as complex as the implementation?
- Where have pure functions been extracted just for testability, but the real bugs hide in how they're called (no **locality**)?
- Where do tightly-coupled modules leak across their seams?
- Which parts of the codebase are untested, or hard to test through their current interface?

Apply the **deletion test** to anything you suspect is shallow: would deleting it concentrate complexity, or just move it? A "yes, concentrates" is the signal you want.

Done: scope is fixed (user direction or commit hot spots), GLOSSARY.md and relevant Decisions are read, and friction notes cover shallowness, locality, seam leaks, and testability in that scope.

### 2. Present candidates as a Markdown report

Write one Markdown file under `plan/<slug>/reports/` for the current Project, or the Project under review. Name the file for this review. Tell the user the path.

Use Mermaid when a graph, flow, or sequence communicates the structure. Each candidate gets a **before/after** diagram.

For each candidate, include:

- **Files** — which files/modules are involved
- **Problem** — why the current architecture is causing friction
- **Solution** — plain English description of what would change
- **Benefits** — explained in terms of locality and leverage, and how tests would improve
- **Before / After diagram** — side-by-side, illustrating the shallowness and the deepening
- **Recommendation strength** — one of `Strong`, `Worth exploring`, `Speculative`

End the report with a **Top recommendation** section: which candidate you'd tackle first and why.

**Use [[GLOSSARY.md]] vocabulary for the domain, and the `/codebase-design` vocabulary for the architecture.** If [[GLOSSARY.md]] defines "Order," talk about "the Order intake module" — not "the FooBarHandler," and not "the Order service."

**Committed Decision conflicts**: if a candidate contradicts an existing Committed Decision, only surface it when the friction is real enough to warrant revisiting that Decision. Mark it clearly in the card. Don't list every theoretical refactor a Committed Decision forbids.

Do NOT propose interfaces yet. After the file is written, ask the user: "Which of these would you like to explore?"

Done: the report exists under `plan/<slug>/reports/`, each candidate has the required fields and a before/after diagram, Top recommendation is present, no interfaces are proposed, and the user has been asked which to explore.

### 3. Grilling loop

Once the user picks a candidate, run the `/grilling` skill to walk the decision tree with them — constraints, dependencies, the shape of the deepened module, what sits behind the seam, what tests survive.

Side effects happen inline as decisions crystallize — run the `/domain-modeling` skill to keep the domain model current as you go:

- **Naming a deepened module after a concept not in `GLOSSARY.md`?** Add the term to `GLOSSARY.md`. Create the file lazily if it doesn't exist.
- **Sharpening a fuzzy term during the conversation?** Update `GLOSSARY.md` right there.
- **User rejects the candidate with a load-bearing reason?** Offer a Committed Decision under [[doc/Decisions/]], framed as: _"Want me to record this as a Committed Decision so future architecture reviews don't re-suggest it?"_ Only offer when the reason would actually be needed by a future explorer to avoid re-suggesting the same thing — skip ephemeral reasons ("not worth it right now") and self-evident ones.
- **Want to explore alternative interfaces for the deepened module?** Run the `/codebase-design` skill and use its design-it-twice parallel sub-agent pattern.

Done: grilling has run on the chosen candidate, and any domain-model or design-it-twice side effects from this step are applied when they apply.
