# Code review — Move queries to Ask

- Range: `git diff origin/staging...HEAD`
- Spec: [63 — Move queries to Ask](../issues/63-move-queries-to-ask.md)
- Arch: [core-refinement architecture](../arch.md)

## 1. Standards

1. **Judgement — Duplicated Code** — [CoreMailboxApi.fs](../../../src/Server/Core/CoreMailboxApi.fs) `eventHistory`. The history door is `eventsSince host EventId.zero`. That is the ticket's history read, not a second query body.
2. **Note** — The mechanical scan lists changed bindings under the 40-line limit in [fsharp-source](../../../.agents/rules/fsharp-source.md). No added line exceeds 100 characters. No [GLOSSARY](../../../GLOSSARY.md) banned phrasing. `MailboxHost.fs::internal` is the scanner reading `let internal create`.
3. **Note** — Ask failure labels are `State`, `LastEventId`, and `EventsSince`. The achieved read claims in [Mailbox](../../../doc/current/mailbox.md) are their own paragraphs.

## 2. Spec

1. **No findings.**

## 3. Arch

1. **No findings.**

## 4. Totals

- Standards: 1 finding. Worst: `eventHistory` calls `eventsSince` of `EventId.zero`.
- Spec: 0 findings.
- Arch: 0 findings.
