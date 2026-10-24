# Ticket work

## 1. Work

The input is one ticket's scope from [[slicing-work.md]].

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

## 3. Done

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
