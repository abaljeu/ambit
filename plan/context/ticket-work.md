# Ticket work

## 1. Work

The input is one ticket's scope from [[slicing-work.md]].

Start with [Cursor scope](cursor-scope.md) in `write-ticket` mode. Fill this ticket from that report.

Write `plan/<slug>/issues/NN-<ticket-name>.md` from the template in section 2. Use section 3.

Hand the written ticket back to slicing.

## 2. Ticket template

Copy these headings into the ticket. Leave each pointer line as written. A pointer line names its target file.

The template is one stage contract: Inputs, Process, and Outputs (Interpretable Context Methodology, Van Clief and McDermott, arXiv 2603.16021).

```
# <NN> — <ticket name>

One job: <plain prose>

Type: <coding or bug-fixing>
Blockers: <none, or each earlier ticket by number and name>

## Inputs

### Working

### Reference

## Do not load

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.

## Outputs

## Stop when

[ ] Record the stop at `plan/<slug>/reports/<NN>-<ticket-name>-stop.md`, then ask.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
[ ] Write the verdict to `plan/<slug>/reports/<NN>-<ticket-name>-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
```

## 3. Fill

One job is plain prose. Write the other sections in the Technical writing form in [Writing markdown](.agents/rules/writing-markdown.md).

1. **One job.** One or two sentences. Name who, and what they can do or see after this ticket.
   - a user
   - the Browser
   - an actor
   - the Server
   One more sentence says why and links the Spec section the job comes from.
2. **Type.** `coding` adds behavior. `bug-fixing` corrects behavior.
3. **Blockers.** Name each earlier ticket by number and name. Write `none` when this ticket can start.
4. **Inputs.** Exact paths.
   - **Working**
     - code files, types, and functions this ticket changes
     - named tests
     - match sites and callers
     - current types, replies, and helpers, each with a short note on why it matters to this change
   - **Reference.** Stable docs and rules.
5. **Do not load.** Files or areas this ticket leaves closed.
6. **Process.** Leave step 1. Then number each edit. Each edit is in a file Inputs names. The edits are one of
   - expand
   - change
   - contract
7. **Outputs.**
   - files that change
   - each test added or changed, with its file and its `--filter` name
   - any report path
8. **Stop when.** Leave the record line. Step 1 uses this section. Stop when
   - the fix needs a file outside Inputs
   - the fix contradicts the Spec
9. **Check.** One act. Leave the pointer lines.

## 4. Done

The ticket has

- One job
- Type
- Blockers
- Inputs
- Do not load
- Process
- Outputs
- Stop when
- Check

The implementer reads the ticket and the files Inputs names.
