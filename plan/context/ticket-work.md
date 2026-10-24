# Ticket work

## 1. Work

The input is one ticket's scope from [[slicing-work.md]].

Start with [Cursor scope](cursor-scope.md) in `write-ticket` mode. Fill this ticket from that report.

Define the details:

- type
- blockers
- look at
- context
- changes
- tests
- done

Write `plan/<slug>/issues/NN-<ticket-name>.md` from the template below. Write that file for the scope just received.

Hand the written ticket back to [[slicing-work.md]].

## 2. Ticket template

Copy these headings into the ticket. Leave each pointer line as written. A pointer line names its target file.

```
# <NN> — <ticket name>

## 1. Type

## 2. Blockers

## 3. Look at

## 4. Context

## 5. Changes

## 6. Tests

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.

```

## 3. Fill

These sections are a technical spec. Write each one in the Technical writing form in [Writing markdown](.agents/rules/writing-markdown.md):

- Look at
- Context
- Changes
- Tests
- Done

1. **Type.** Set `coding` or `bug-fixing`. `coding` adds behavior. `bug-fixing` corrects behavior.
2. **Blockers.** Name the earlier tickets by number and name. Blockers are the order. Write `none` when this ticket can start.
3. **Look at.** One reading list. It names
   - the owner module
   - the files, types, and functions this ticket changes
   - the named tests
   - the match sites and callers
4. **Context.** State the current types, replies, and helpers this change uses. Write those facts here.
5. **Changes.** Name the one expand, change, or contract. Name each edit in a file Look at already named.
6. **Tests.** Name each test to add or change, its file, what it checks, and the `--filter` names that run them.
7. **Done.** Leave the template pointer lines under **7. Done**:
   - [ ] Run `scripts/agent-test.sh` with no arguments.
   - [ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
   - [ ] All executed tests pass.

## 4. Done

The ticket names

- its type
- its blockers
- the Look at reading list
  - the owner module
  - the files, types, and functions this ticket changes
  - the named tests
  - the match sites and callers
- the current types, replies, and helpers
- the changes
- the tests, with their files and filter names
- what done looks like
- one step of expand, change, or contract
- the Technical writing form in [Writing markdown](.agents/rules/writing-markdown.md)

The implementer reads the ticket and the files Look at names.
