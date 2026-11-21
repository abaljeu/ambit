---
name: code-review
description: Review working-tree changes vs HEAD (or a user-named fixed point when given) along Standards, Spec, and binding arch. Spec comes from local plan/ paths. Use when the user wants to review a branch, work-in-progress changes, or asks to "review since X".
---

Review of a working-tree or tip diff:

- **Standards** — does the code conform to this repo's coding standards?
- **Spec** — does the code faithfully implement the originating issue / PRD / spec?
- **Arch** — does the diff obey each binding `arch.md` rule it touches?

The three passes run as **parallel sub-agents** so they don't pollute each other's context, then this skill aggregates their findings.

Issue tracker: local `plan/`. Git: [[.agents/skills/git-protocol/SKILL.md]].

When pinning the range, run the mechanical Standards scan in step 2. Optional F# narrowing (`--fn`, `--range`, `--usage`) stays in [[.agents/skills/code-review-fsharp/SKILL.md]].

## Process

### 1. Pin the review range

**Default**: uncommitted changes vs `HEAD`. Do not invent a base SHA.

If the tree is clean (everything committed), the tip under review is `HEAD`. When the user names an older fixed point (commit, branch, tag, `HEAD~N`), use that; otherwise do not ask for one just to bookkeep SHAs.

For an explicit fixed point: `git diff <fixed-point>...HEAD` (three-dot) and `git log <fixed-point>..HEAD --oneline`. Confirm the ref resolves and the diff is non-empty before spawning sub-agents. Done: the range is either `HEAD` working tree / tip, or a named fixed point whose three-dot diff is non-empty.

### 2. Run the mechanical Standards scan

Run against the range from step 1 (cwd = repo root):

```bash
python .agents/skills/code-review/scripts/standards-scan.py
```

If a fixed point was named, pass `--diff <fixed-point>`. The script invokes [[.agents/skills/code-review-fsharp/SKILL.md]] measure-fs-size when `*.fs` / `*.fsi` are in the range — do not run measure-fs-size as a second parent command. Paste the full stdout into the Standards prompt. Count a printed line as a finding only when it breaks the limit in the rule it cites ([[.agents/rules/fsharp-source.md]], [[.agents/rules/refer-by-name.md]], [[.agents/rules/markdown-writing.md]]), or when it is a [[GLOSSARY.md]] PHRASE line. The phrase list is the Banned phrasings section of [[GLOSSARY.md]]. The file limit is the one in [[.agents/rules/fsharp-source.md]]. Surgical under-100-line preference is not a script fail ([[.agents/rules/core-agent-behavior.md]]). Smells stay human ([[SMELLS.md]]).

Done: the script has been run for this range, its stdout is in the Standards prompt (or the prompt says `scan: none`), and that prompt counts a printed line as a finding only when it breaks the cited limit or it is a GLOSSARY.md PHRASE line.

### 3. Identify the spec source

Look for the originating spec, in this order:

1. A `plan/` path the user passed (preferred).
2. A PRD/spec under `plan/<feature>/` (or rarely `specs/`) matching the feature.
3. If nothing is found, ask the user for the local spec path. If they say there isn't one, the **Spec** sub-agent will skip and report "no spec available".

Do **not** harvest GitHub/GitLab issue numbers from commit messages as the spec source. No SHA bookkeeping on tickets. Done: you have a local spec path to pass, or Spec will report "no spec available".

### 4. Identify the standards sources

Live coding standards for this repo live under [[.agents/rules/]]: [[.agents/rules/fsharp-source.md]] (F#), [[.agents/rules/core-api.md]] (Core vs Adapter), [[.agents/rules/core-agent-behavior.md]] (surgical changes), and the other scoped rules. Do not hunt missing `CODING_STANDARDS.md` or `CONTRIBUTING.md`.

The Standards axis also always carries the smell baseline in [[SMELLS.md]]. Done: the Standards prompt will list the rules paths plus [[SMELLS.md]].

### 5. List binding arch

List the binding arch files for this diff. Use the discovery in [[.agents/skills/implement/SKILL.md]] step 2 on this diff. Read those files. Done: the Arch prompt lists those files, or it says no binding arch.

### 6. Spawn the sub-agents in parallel

Send a single message with three `Agent` tool calls. Use the `general-purpose` subagent for each. When Spec or Arch is skipped, spawn only the passes that run.

Paste the matching step 7 labels into each brief. Each sub-agent returns a Markdown list in that shape: one finding per item, no paragraphs. Under 400 words.

**Standards sub-agent prompt** — include:

- The full diff command (default `git diff HEAD`) and commit list if a fixed point was named.
- The stdout of the mechanical scan from step 2 (or `scan: none`).
- The list of standards-source files you found in step 4, **plus [[SMELLS.md]]**: include that file's contents in the prompt, or give the path and instruct the sub-agent to read it. The sub-agent has no other access to the smell baseline.
- The brief: each standard violation cites the standard (file + the rule) as a **Hard** item. Each smell you will stand behind names the smell, quotes the hunk, and is a **Judgement** item. Distinguish hard violations from judgement calls per the included smell baseline. Count a scan line only when it breaks the limit in the rule it cites, or when it is a GLOSSARY.md PHRASE line. The phrase list is the Banned phrasings section of GLOSSARY.md. The file limit is in .agents/rules/fsharp-source.md. No findings: one item, `No findings.` Scan notes that are not findings are further items.

**Spec sub-agent prompt** — include:

- The same diff command (and commit list if any).
- The path or contents of the local spec.
- The brief: one item per finding, using **Missing**, **Partial**, **Extra**, **Wrong**, or **Unnamed**. (a) Requirements the spec asked for that are missing or partial. (b) Behaviour in the diff that was not asked for (scope creep) is **Extra**. (c) Requirements that look implemented but where the implementation looks wrong. (d) Each global type or interface the diff adds or changes that the local spec did not name, focusing on the data, not functions, is **Unnamed**.

If the spec is missing, skip the Spec sub-agent and note this in the final report.

**Arch sub-agent prompt** — include:

- The same diff command (and commit list if any).
- The binding arch file list from step 5, or `no binding arch`.
- The brief: check each relevant rule in the binding arch files. A break is a **Blocker** item and names the arch file, the section, and the code location. When no arch.md binds this diff, one item: `No binding arch.` When every touched rule holds, one item: `No findings.`

When no arch.md binds this diff, skip the Arch sub-agent and note that in the final report. Done: Standards has been spawned; Spec has been spawned or skipped with "no spec available"; Arch has been spawned or skipped with "no binding arch".

### 7. Aggregate

Write the report under a Project path (`plan/<slug>/reports/…`). Do not write the report directly under `plan/`. The report is a title plus lists. Each section body is a list. If a sub-agent returns a paragraph, split it into list items before writing the file. Keep the axes separate: do not merge or rerank findings, and do not pick a single winner across axes.

```markdown
# Code review — <name>

- Range: `<diff command>`
- Spec: [<ticket name>](<path>)
- Arch: [<arch name>](<path>)

## 1. Standards

1. **Hard** — [<rule file>](<path>): "<rule sentence>". <file, binding, what is wrong>.
2. **Judgement — <smell>** — <file, binding>. <what is wrong>.

## 2. Spec

1. **Missing** — <requirement>. <what is absent>.
2. **Partial** — <requirement>. <what is incomplete>.
3. **Extra** — <behaviour>. <why the spec did not ask for it>.
4. **Wrong** — <requirement>. <what the code does instead>.
5. **Unnamed** — `<Type>`. <why the spec did not name this data>.

## 3. Arch

1. **Blocker** — [<arch file>](<path>) §<section>. <code location>. <rule that broke>.

## 4. Totals

- Standards: <n> findings. Worst: <one clause>.
- Spec: <n> findings. Worst: <one clause>.
- Arch: <n> findings. Worst: <one clause>.
```

Omit a header bullet when that source was not in the review. An empty axis is one item: `No findings.` Notes that are not findings, such as a clean scan, are further items in that section. A skipped Spec section is one item: `No spec available.` A skipped Arch section is one item: `No binding arch.` Totals has one item per axis, and the worst issue within that axis when the count is not zero. Done: the report file has the four numbered headings, every section body is a list, and Totals has one item per axis.

### 8. Ticket Status

A report is not approval. Leave `coded` unless the user approves the review for a named ticket. Then set that ticket `**Status:** done`. Do not rewrite a legacy Status on a ticket you did not set to `coded`. Done: Status is `done` only after that approval; otherwise Status is unchanged.
