# Cursor scope

## 1. Work

Grok sends Cursor one instruction. Cursor writes one report. Grok reads that report, then writes the ticket or implements the ticket.

Grok sends when that report path is empty. Grok fills when the report exists.

## 2. Send

Grok calls Task with

- `subagent_type`: `explore`
- `model`: `composer-2.5-fast`
- `description`: Scope `<ticket name>`

The prompt names

- `.agents/subagents.md`
- `plan/context/cursor-scope.md`
- the mode: `write-ticket` or `implement`
- the slice sentence, or the ticket path
- the report path `plan/<slug>/reports/<NN>-<ticket-name>-scope.md`

## 3. Report

Cursor writes only that report. The report uses the Technical writing form in [Writing markdown](.agents/rules/writing-markdown.md).

The report obeys numbers get names in [Refer by name](.agents/rules/refer-by-name.md) before it is done.

### Signatures

[ ] Current type and function signatures this change uses

### Match sites

[ ] Each constructor, match, and caller, with file and function

### Existing tests

[ ] Each named test that already exists, its file, and what it checks

### Copy-from

[ ] The helper that already does the same job, with file and function

### write-ticket

[ ] Source files this slice would change
[ ] Tests this slice would add or change

### implement

[ ] Each ticket claim that differs from the code
[ ] How each named new test observes the change at a public seam

## 4. Then

`write-ticket`: Grok fills [ticket-work.md](ticket-work.md) from the report.

`implement`: Grok fills [build-work.md](build-work.md) from the ticket and the report.

Grok's Fill context is that report, the slice or the ticket, and the files those name for the edit.

## 5. Done

- the report exists at that path
- each item is one fact
- Grok wrote the ticket or the source from that report
- a bare ticket id fails the report: `ticket 102` fails, and `102 — Place Parse changes as Order.Change` is the named form
