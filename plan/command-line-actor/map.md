# Command-line actor

## 1. Problem

The owner cannot run a command from an outline node and see the output as nodes.

Run on a `>` line takes the Amble path today. The Amble path parses that line as an Amble statement on the client. See [Amble Run](../../doc/roadmap/amble-run.md) and the Run entry in [Run](../../doc/user/reference/commands/run.md).

## 2. Solution

A command-line actor is part of the Run command. Run picks it when the first character of the focus node's text is `>`. Ambit is a private MVP, and only the owner runs commands.

The actor

- is a server actor named `CommandActor`
- sets the working directory to the focus node's enclosing directory
- starts the program named by the first word after the `>`, with the rest of the line as arguments, with no shell
- turns each line of stdout and stderr into a plain child node, up to 1000 lines
- sets the cssclass `error` on the `>` node when the exit code is non-zero
- stops the process on Cancel

The [Spec](spec.md) holds the stories, the decisions, and the acceptance criteria. The [Architecture](arch.md) holds the owners and seams.

## 3. Scope

In scope

- the Run choice for a `>` line
- the actor that runs the command and builds the children
- the user and current docs for that behavior

Out of scope

- the Amble `=` and `?` paths in [22 — Run sends = lines to Amble](../online-search/issues/22-run-sends-equals-lines-to-amble.md)
- edits to the Roadmap
- the AMBLE language expanding to encompass exec
