# Command-line actor

## 1. Problem

A person cannot run a shell command from an outline node and see the output as nodes.

Run on a `>` line takes the Amble path today. The Amble path parses that line as an Amble statement on the client. See [Amble Run](../../doc/roadmap/amble-run.md) and the Run entry in [Run](../../doc/user/reference/commands/run.md).

## 2. Solution

A command-line actor is part of the Run command. Run picks it when the focus node's text starts with `>`.

The actor

- runs in the Azure environment
- sets the working directory to the focus node's directory
- runs the text after the `>` as a shell command
- captures stdout and builds child nodes from it

The [Spec](spec.md) holds the stories, the acceptance criteria, and the open questions.

## 3. Scope

In scope

- the Run choice for a `>` line
- the actor that runs the command and builds the children
- the user and current docs for that behavior

Out of scope

- the Amble `=` and `?` paths in [22 — Run sends = lines to Amble](../online-search/issues/22-run-sends-equals-lines-to-amble.md)
- edits to the Roadmap
- any answer to an open question in the [Spec](spec.md)
