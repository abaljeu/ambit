# Command-line actor

Map: [map.md](map.md)

## 1. User stories

1. Pick the actor — As a person, I want Run on a node whose text starts with `>` to start the command-line actor, so that I do not choose an actor by hand.
2. Run the command — As the owner, I want the text after the `>` to run as a program with arguments, so that a node holds a command I can run again.
3. Run in place — As a person, I want the command to run in the focus node's directory, so that relative paths in the command mean the files beside that node.
4. See the output — As a person, I want the output of the command to become child nodes of the focus node, so that I can read and edit the output as an outline.
5. Run on the server — As the owner, I want the command to run on the server, so that it runs where the server and the data are, on Azure or on a local server.

## 2. Locked decisions

Product owner, 2026-11-21.

### Context

[ ] Private MVP — Ambit is a private application. The owner runs and owns the server. This is an MVP.
[ ] Owner only — Only the owner runs commands. There is no permission gating.
[ ] Environment — The command sees the server environment, including App Settings environment variables. See [Secrets](../../doc/reference/secrets.md).
[ ] Part of Run — The command-line actor is part of the Run command.
[ ] Same everywhere — The local server runs the command the same way as Azure, because `Process.Start` is cross-platform.

### Trigger

[ ] Pick — When the first character of the focus node's text is `>`, Run executes the command-line actor.
[ ] First character — Leading whitespace does not count. A text with whitespace before the `>` is not a command line.
[ ] Precedence — A `>` first character wins over `=`. `> x =y` runs the command-line actor. `x=y` runs Amble as today.
[ ] Replaces — This replaces the client-side legacy Amble meaning of a `>` line.
[ ] Any node — No node type is special. A File Node, Directory Node, or Workspace Node whose text starts with `>` runs the actor.
[ ] Empty — A bare `>`, with no program, does nothing.
[ ] Owner scan — The Run code is the same for a `>` line that the owner scan of [22 — Run sends = lines to Amble](../online-search/issues/22-run-sends-equals-lines-to-amble.md) reaches. It runs the command like a focused line.
[ ] Start path — Run posts an order to a new server actor named `CommandActor`. This follows the pattern of the search actor.
[ ] Live actor — A second Run while the actor is live is refused, as already coded for actors.

### Execution

[ ] Program and arguments — The next word after the `>` is the program. The rest of the line is passed as the arguments. `> x` executes `x`. `> x =y` executes `x` with the argument `=y`.
[ ] No shell — The actor uses .NET `Process.Start` with the first word as the file name and the rest as the arguments. This works the same on Windows and Linux.
[ ] Shell on request — A person who wants a shell writes `> pwsh -c "..."` or `> sh -c "..."`.
[ ] Working directory — The working directory is the focus node's enclosing directory. For a File Node or Directory Node, that is its directory. For any other node, that is the nearest enclosing directory.
[ ] Thread — The actor runs on its own thread.
[ ] No timeout — A command has no timeout.
[ ] Cancel — The actor inherits the actor cancel. Cancel kills the process.

### Output

[ ] Child per line — Each output line becomes one plain child node. The actor does no outline parsing for now.
[ ] stdout and stderr — The actor captures both. It interleaves them in arrival order, as a terminal shows them.
[ ] Cap — The actor adds at most 1000 output lines as children. At the cap it stops adding children and adds a final child reading `output truncated`. The process keeps running until it exits or is cancelled.
[ ] Exit code — A non-zero exit sets the cssclass `error` on the start node, the `>` node. No extra child is added. A successful run clears the `error` class.
[ ] Re-run — New output replaces the start node's existing children.

### Out of scope

[ ] Future — The AMBLE language will later expand to encompass exec. That is not in scope.
[ ] Placement — This Project has no Epic or Chapter for now. It stays unplaced.

## 3. Acceptance criteria

### Pick

[ ] A Run on a focus node whose first character is `>` starts the command-line actor.
[ ] A Run on a focus node whose first character is not `>` does not start the command-line actor.
[ ] A text with whitespace before the `>` does not start the command-line actor.
[ ] A text that starts with `>` and contains `=` starts the command-line actor and does not run Amble.
[ ] A bare `>` does nothing.
[ ] A `>` text on a File Node, Directory Node, or Workspace Node starts the actor.
[ ] A `>` line that the owner scan reaches runs the command.
[ ] A second Run while the actor is live is refused.

### Command

[ ] The program is the first word after the `>`.
[ ] The arguments are the rest of the line, passed unchanged.
[ ] The actor starts the program with `Process.Start` and no shell.
[ ] The command sees the server environment variables.
[ ] The same text runs the same way on a local server and on Azure.

### Directory

[ ] The working directory of the command is the focus node's enclosing directory. For a node that is not a File Node or Directory Node, it is the nearest enclosing directory. The hosting is in [Deploying to Azure](../../doc/reference/deploy-azure.md). The on-disk data root is `DataDir`, in [Persistence model](../../doc/current/persistence-model.md).

### Output

[ ] The actor captures stdout and stderr, interleaved in arrival order.
[ ] Each output line becomes one plain child node of the focus node.
[ ] After 1000 output lines the actor stops adding children and adds one final child reading `output truncated`.
[ ] The process keeps running after the cap until it exits or is cancelled.
[ ] A non-zero exit code sets the cssclass `error` on the focus node and adds no child.
[ ] A zero exit code clears the cssclass `error` on the focus node.
[ ] A re-run replaces the existing children with the new output.

### Cancel

[ ] The command has no timeout.
[ ] Cancel kills the process.

## 4. Existing behavior this Project meets

[ ] [Run](../../doc/user/reference/commands/run.md) says the Amble evaluator runs on a line that starts with `>`.
[ ] `AmbleRun.shouldExec` in `src/Shared/AmbleRun.fs` is true when the trimmed line starts with `>`. `AmbleRun.runPlan` sends a `>` line that is not a run statement to `legacyRun`, which parses it as Amble on the client.
[ ] `execAmbleRunOp` in `src/Client/Commands.fs` deletes the focus node's children before it runs a `>` line.
[ ] `CommandRequest.isQueryText` in `src/Shared/CommandRequest.fs` is true for text that contains `=` and does not start with `?`. `submitQuery` in `src/Client/Commands.fs` runs before the `>` check.
[ ] The [Amble language roadmap](../../doc/roadmap/language-syntax-and-semantics.md) describes `>` as a shell command with argv tokens, embedded expressions, redirections, and pipes.
[ ] [22 — Run sends = lines to Amble](../online-search/issues/22-run-sends-equals-lines-to-amble.md) puts `>` lines out of scope.
[ ] The Glossary defines the enclosing artifact and the Directory Node. See [GLOSSARY.md](../../GLOSSARY.md).

## 5. Open questions

None. The product owner answered every question on 2026-11-21. Each answer is a decision in section 2 and a criterion in section 3.
