# Skill structure vs method

First cut: [[.agents/skills/]] only. **Structure** is order, gates, names, schemas, and done. **Method** is how a step is done this month. Two documents, not a method section in the same essay.

[[.agents/skills/writing-for-agents/SKILL.md]] on disk is complete (~81 lines) plus [[.agents/skills/writing-for-agents/SKILL-MECHANICS.md]] and [[.agents/skills/writing-for-agents/GLOSSARY.md]]. The editor showed it truncated. Concurrent edit: not rewritten. That skill splits steps vs disclosed reference. Alan's cut is stricter: structure file vs method file.

## High-value splits

Four whole-document splits. Not every bulky skill.

| Split | Current | Stays (structure) | Leaves (method) | Why method changes |
| --- | --- | --- | --- | --- |
| Ticket publish | [[.agents/skills/to-tickets/SKILL.md]], [[.agents/skills/to-feature-tickets/SKILL.md]] | Each skill: gather → explore → **draft** (vertical slice vs cohesive capability) → quiz → pointer to publish. Distinct step 3 only. | Shared [[.agents/skills/to-tickets/PUBLISH.md]] (plain file; both skills are user-invoked so neither can fire the other): numbering, tracker fields, wait-what style, snippet policy, the local-ticket template. | Template labels and snippet rules change independently of slice-vs-capability. Today they must be edited twice. |
| Fowler smells | [[.agents/skills/code-review/SKILL.md]] | Pin range, spec source, two parallel axes, aggregate, one-line summary. | [[.agents/skills/code-review/SMELLS.md]] — the smell list and “how to fix”. Parent still pastes it into the Standards sub-agent. | Which smells and how to phrase them change; the two-axis process does not. |
| Diagnosis loop recipes | [[.agents/skills/diagnosing-bugs/SKILL.md]] | Phase names, order, gates, completion checklists (red command, minimise, 3–5 hypotheses, cleanup). | [[.agents/skills/diagnosing-bugs/LOOP.md]] — the ten loop-construction recipes, tighten tactics, non-det tactics, instrument tool preference. | How you build a loop this month (Playwright vs curl vs harness) changes; “no red command, no Phase 2” does not. |
| Teach pedagogy | [[.agents/skills/teach/SKILL.md]] | Named workspace files, when to write each, formats already in MISSION-FORMAT / RESOURCES-FORMAT / LEARNING-RECORD-FORMAT / GLOSSARY-FORMAT. | [[.agents/skills/teach/PEDAGOGY.md]] — fluency vs storage, ZPD, quiz design, communities. | Teaching fashion changes; the file layout does not. |

Smaller, still real: [[.agents/skills/implement-fsharp-feature/SKILL.md]] keep Shared-first layout and the Client compile gate; pull filter recipes (`dotnet test --filter`, `./scripts/client.sh build`) to [[.agents/skills/implement-fsharp-feature/TEST-COMMANDS.md]]. Commands follow scripts; layout does not.

## Shared method (one home)

- **Ticket publish** — see above. One template. [[.agents/skills/to-feature-tickets/SKILL.md]] points at it. Do not keep a second copy.
- **Prototype snippet exception** — the same “inline a decision-rich prototype snippet” paragraph lives in to-tickets, to-feature-tickets, and [[.agents/skills/to-spec/SKILL.md]]. Fold into PUBLISH.md (tickets) plus one line in to-spec, or one tiny shared snippet note both point at. Three copies today.
- **Grounding** — duplicated in [[.agents/skills/writing-shape/SKILL.md]] and [[.agents/skills/writing-beats/SKILL.md]]. One [[.agents/skills/writing-shape/GROUNDING.md]] (or a plain file next to both). Each SKILL.md keeps its own loop.
- **DI / mockability examples** — TypeScript “pass the gateway in” lives in [[.agents/skills/tdd/mocking.md]] and again under Designing for testability in [[.agents/skills/codebase-design/SKILL.md]]. Keep examples in mocking.md. Codebase-design keeps the glossary and principles only.

Not a new method file: git bash blocks in [[.agents/skills/git-share/SKILL.md]] and [[.agents/skills/git-master/SKILL.md]]. Those are caches of [[scripts/]]. Structure stays (pull free, push gated, never push `dev`). Point at the scripts; do not duplicate `git fetch` recipes.

`./scripts/client.sh build` is restated in implement-fsharp-feature and [[.agents/skills/investigate-fable-client/SKILL.md]]. After TEST-COMMANDS.md exists, the client skill points there.

## Already separated

| Skill | Siblings | Cut today | Vs structure/method |
| --- | --- | --- | --- |
| tdd | [[.agents/skills/tdd/tests.md]], [[.agents/skills/tdd/mocking.md]] | Examples vs loop | Close. SKILL.md is seams, anti-patterns, red-green (structure). tests.md / mocking.md are how-to. SKILL.md still restates “what a good test is” that tests.md shows. |
| prototype | [[.agents/skills/prototype/LOGIC.md]], [[.agents/skills/prototype/UI.md]] | By branch | Right by-branch split. SKILL.md is pick-branch + shared rules (structure). LOGIC/UI are still mixed process + how-to; do not split further this cut. |
| codebase-design | [[.agents/skills/codebase-design/DEEPENING.md]], [[.agents/skills/codebase-design/DESIGN-IT-TWICE.md]] | Glossary + principles in SKILL.md | Already method/process siblings. Remaining TS examples should leave SKILL.md (see DI above). |
| domain-modeling | CONTEXT-FORMAT, COMMITTED-DECISION-FORMAT | Schemas | Structure siblings. Session tactics stay in SKILL.md (short; leave). |
| teach | four *-FORMAT.md | Schemas | Formats are structure. Pedagogy still sits in SKILL.md (the split above). |
| cloud-agent-git | [[.agents/skills/cloud-agent-git/LAND.md]] | Work vs download | Sequence split, not structure/method. Keep. |
| ask-matt | [[.agents/skills/ask-matt/PHASE-BOUNDARIES.md]] | Router vs phase tree | Structure sibling. Leave. |
| writing-for-agents | SKILL-MECHANICS, GLOSSARY | Reference disclose | Concurrent; skip. Not Alan's structure/method cut. |

## Stay put (tight recipes)

Idea to ship: [[.agents/skills/grill-me/SKILL.md]], [[.agents/skills/grilling/SKILL.md]], [[.agents/skills/grill-with-docs/SKILL.md]], [[.agents/skills/implement/SKILL.md]], [[.agents/skills/add-shared-test/SKILL.md]]. to-spec stays: the spec template is a schema.

On-ramps: [[.agents/skills/wayfinder/SKILL.md]] is long because map/ticket schemas are structure. [[.agents/skills/qa/SKILL.md]] is long because issue templates are schemas. [[.agents/skills/request-refactor-plan/SKILL.md]] already points at wayfinder for the map.

Adapters: [[.agents/skills/git-protocol/SKILL.md]], [[.agents/skills/project-work/SKILL.md]], [[.agents/skills/to-archive/SKILL.md]], [[.agents/skills/scratch-script/SKILL.md]], [[.agents/skills/write-simple-parser/SKILL.md]], [[.agents/skills/prepare-agent-instruction-change/SKILL.md]] (concurrent; skip rewrite), [[.agents/skills/code-review-fsharp/SKILL.md]].

Standalone: [[.agents/skills/handoff/SKILL.md]], [[.agents/skills/research/SKILL.md]], [[.agents/skills/wait-what/SKILL.md]], [[.agents/skills/resolving-merge-conflicts/SKILL.md]], [[.agents/skills/wizard/SKILL.md]], [[.agents/skills/to-questionnaire/SKILL.md]] (template is schema).

Imported bulky recipes (azure, migrate-to-shoehorn, scaffold-exercises, setup-*) are not Gambol pipeline. Do not split this cut.

## Cluster remainder (no extra files)

- **Vocabulary:** [[.agents/skills/improve-codebase-architecture/SKILL.md]] report card + grill loop stay. Explore heuristics can stay; they are short. [[.agents/skills/ubiquitous-language/SKILL.md]] is mostly output format; if it is split later, the proposal tables are schema, not a new method essay.
- **Adapters:** [[.agents/skills/plan-roadmap-change/SKILL.md]] outline is structure. [[.agents/skills/maintain-doc-currency/SKILL.md]] directory fit is a cache of [[doc/README.md]]. [[.agents/skills/co-edit-format-plan/SKILL.md]] interaction rules are structure.
- **Do not split for length:** qa, wayfinder, codebase-design SKILL.md, ubiquitous-language. Length here is schema or glossary.
