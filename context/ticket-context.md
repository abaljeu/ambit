# Ticket context

## 1. Work

Each ticket is one file, `plan/<slug>/issues/NN-<ticket-name>.md`. Write each ticket when the ticket step is about to be worked.

## 2. Cut and order

The arch sets the direction of the cuts. This step cuts the slices and orders them.

1. **Read the strategy.** Read the slicing strategy at the end of `plan/<slug>/arch.md`. It is one of `tracer-cut`, `module-build` or `expand-contract`. When it is absent, stop and ask.
2. **Cut.** Cut the slices that strategy names, using the rules below. Size each slice to fit one fresh context window.
3. **Order.** Give each ticket its blockers. A ticket's blockers are the tickets that come before it.
   - The first ticket has no blockers.
   - The last ticket is the one no other ticket is blocked by.
4. **Number and name.** Number and name each ticket per [[../.agents/rules/refer-by-name.md]].

### Tracer cut (`tracer-cut`)

One ticket per story path in the arch.

- Each slice cuts a narrow, complete path through every layer it touches.
- Each finished slice is demoable or verifiable on its own.
- An instruction-file slice is verified by repo search and by the instructions' published behavior.

### Module build (`module-build`)

One ticket per module map entry in the arch.

- Each ticket is one capability that hangs together and is proven by tests.
- Each finished ticket is verifiable once its blockers are done.
- Split two capabilities that test independently. Merge a split that would leave an untestable fragment.
- Add tickets for user-visible behavior after the testable capabilities, when the arch's stories still need them.

### Expand-contract (`expand-contract`)

For a wide mechanical change, such as renaming a column or retyping a shared symbol, whose single edit breaks many call sites.

1. **Expand.** Add the new form beside the old, so every caller still builds.
2. **Migrate.** Move call sites in batches sized by blast radius, such as one package or one folder per batch. Each batch is its own ticket, blocked by the expand. The old form keeps each batch green.
3. **Contract.** Delete the old form once no caller remains. This ticket is blocked by every migrate batch.

When a batch can only be green together with others, the batches share an integration branch, and a final integrate-and-verify ticket is blocked by all of them. Green is promised at that ticket.

### Size

- A ticket is one step: one expand, one change, or one contract.
- A ticket with numbered steps is split into one ticket per step.
- A ticket changes 8 source files or fewer.
- A ticket adds or changes 4 tests or fewer.

### Ripple

When a ticket adds a union case or changes a public signature, Look at names every site that changes:

- each match on that union, with its file and function
- each caller of that signature, with its file and function
- each test that calls it, with its file and count

Look at also names the helper that does the same job today, such as the append function or the post function.

### Scope

- A ticket changes source and tests.
- GLOSSARY, arch and spec change in their own steps.
- Each name in Tests is a test that exists or that the ticket adds.

## 3. Ticket template

Copy these headings into the ticket. Leave each pointer line as written. A pointer line names its target file.

```
# <NN> — <ticket name>

## 1. Type
Set `coding` or `bug-fixing`. `coding` adds behavior. `bug-fixing` corrects behavior.

## 2. Blockers
Name the earlier tickets by number and name. Blockers are the order. Name none when this ticket can start.

## 3. Look at
Name the files, the types, and the functions to read.

## 4. Context
Describe the state before this work is done.

## 5. Changes
Name the changes to make.

## 6. Tests
Name each test to add or change, its file, and what it checks. Name the `--filter` names that run them.

## 7. Done
How we can validate the ticket is accomplished.

```

## 4. Done
The ticket names

- its type
- its blockers
- the files to look at
- the types to look at
- the functions to look at
- the context
- the changes
- the tests, with their files and filter names
- what done looks like
- one step of expand, change, or contract
- every match site and caller it ripples to

The implementer uses the ticket and the code.
