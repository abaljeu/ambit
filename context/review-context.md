# Review context

## 1. Work

Write one report per ticket to `plan/<slug>/review/NN-<ticket-name>.md`. Write that report when the review step is about to be worked.

On cloud, the range is the ticket's PR diff against staging.
On Windows, the range is the current diff, or whatever diff is related to the ticket.

## 2. Review template

Copy these headings into the report. Leave each pointer line as written. A pointer line names its target file.

```
# <NN> — <ticket name>

Ticket: [[../issues/NN-<ticket-name>.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

## 2. Spec

## 3. Arch

## 4. Tests

## 5. Miss

## 6. Found elsewhere
```

## 3. Fill

Follow the full suite in [[.agents/skills/tdd/SKILL.md]]. Do the rest of the review while it runs.

Write each section as a list, one finding per item. An empty section is one item: `No findings.`

1. **Standards.** Run `python scripts/standards-scan.py --diff origin/staging` from the repo root. A printed line is a finding when it breaks the limit in the rule it cites, or when it is a GLOSSARY.md PHRASE line. Then check the diff against each file in [[.agents/rules/]] and against the smells below. Label each finding:
   - **Hard** — breaks a rule. Cite the rule file and the rule.
   - **Judgement** — a smell. Name the smell and quote the hunk.
2. **Spec.** Score the diff against each story the ticket names, using `spec.md` and the ticket. Label each finding:
   - **Missing** — a requirement with no code.
   - **Partial** — a requirement with incomplete code.
   - **Wrong** — code that does something other than the requirement.
   - **Extra** — behavior the ticket and spec leave out.
   - **Unnamed** — a global type or interface the diff adds or changes that the ticket and spec leave unnamed.
3. **Arch.** Check the diff against the story paths and the module map in `arch.md`. Each break is a **Blocker** and names the arch section and the code location.
4. **Tests.** Check that each test the ticket named exists and checks what the ticket says. Record what `scripts/check-test.sh <pid>` prints.
5. **Miss.** Name what returns the ticket to build:
   - each Hard finding
   - each Missing, Partial or Wrong finding
   - each Blocker
   - a named test that is missing
   - a failing test
   - a test that checks something other than the ticket's done

6. **Found elsewhere.** Take each item in the PR body's **Found elsewhere** list. Mark it **Accept** or **Reject** and give the reason. An accepted item names the file and the wording the document step writes.

Judgement, Extra and Unnamed findings go to the user to decide. Found elsewhere items go to the user to confirm.

## 4. Smells

- **Mysterious Name** — the name hides what it does or holds. Rename it.
- **Duplicated Code** — the same logic in two hunks or files. Extract it.
- **Feature Envy** — a function reaches into another type's data more than its own. Move it to that data.
- **Data Clumps** — the same fields or parameters travel together. Give them one type.
- **Primitive Obsession** — a primitive stands in for a domain concept. Give the concept its own type.
- **Shotgun Surgery** — one change edits many files. Gather what changes together.
- **Speculative Generality** — parameters or hooks the spec has no need for. Inline them.
- **Middle Man** — a function that only forwards. Call the target.
- **Argument Count** — more than four positional arguments. Give them a record type.
- **Overcomplication** — a simpler approach does the same work. Use it.

Done: the report has the six sections, each section is a list, the Miss section names what returns the ticket to build, and each Found elsewhere item is accepted or rejected.
