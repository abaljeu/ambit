# Handoff summary

A compact summary of the current conversation so a fresh agent can continue the work.

The summary includes:

- A **suggested skills** section naming skills the next agent should invoke
- Pointers (path or URL) to existing artifacts the work already captured ( specs, plans, ADRs, issues, commits, diffs ) in place of restating their content
- Redaction of secrets: API keys, passwords, and personally identifiable information

When the user passed arguments, treat them as the next session's focus and tailor the summary to that focus.
