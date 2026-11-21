# Command-line actor

Map: [map.md](map.md)

## 1. User stories

1. Pick the actor — As a person, I want Run on a node whose text starts with `>` to start the command-line actor, so that I do not choose an actor by hand.
2. Run the command — As a person, I want the text after the `>` to run as a shell command, so that a node holds a command I can run again.
3. Run in place — As a person, I want the command to run in the focus node's directory, so that relative paths in the command mean the files beside that node.
4. See the output — As a person, I want the stdout of the command to become child nodes of the focus node, so that I can read and edit the output as an outline.
5. Run in Azure — As a person, I want the command to run in the Azure environment, so that it runs where the server and the data are.

## 2. Locked decisions

Alan, 2026-11-21.

[ ] Part of Run — The command-line actor is part of the Run command.
[ ] Pick — Run picks the command-line actor when the focus node's text starts with `>`.
[ ] Environment — The actor runs in the Azure environment.
[ ] Working directory — The actor sets the working directory to the focus node's directory.
[ ] Command — The actor runs the focus node's text, after the `>`, as a shell command.
[ ] Output — The actor captures stdout and builds child nodes from it.

## 3. Acceptance criteria

### Pick

[ ] A Run on a focus node whose text starts with `>` starts the command-line actor.
[ ] A Run on a focus node whose text does not start with `>` does not start the command-line actor.

### Command

[ ] The command is the focus node's text after the `>`.
[ ] The actor runs that command as a shell command.

### Environment and directory

[ ] The actor runs in the Azure environment. The hosting is in [Deploying to Azure](../../doc/reference/deploy-azure.md). The on-disk data root is `DataDir`, in [Persistence model](../../doc/current/persistence-model.md).
[ ] The working directory of the command is the focus node's directory.

### Output

[ ] The actor captures stdout.
[ ] The actor builds child nodes of the focus node from that stdout.

## 4. Existing behavior this Project meets

[ ] [Run](../../doc/user/reference/commands/run.md) says the Amble evaluator runs on a line that starts with `>`.
[ ] `AmbleRun.shouldExec` in `src/Shared/AmbleRun.fs` is true when the trimmed line starts with `>`. `AmbleRun.runPlan` sends a `>` line that is not a run statement to `legacyRun`, which parses it as Amble on the client.
[ ] `execAmbleRunOp` in `src/Client/Commands.fs` deletes the focus node's children before it runs a `>` line.
[ ] `CommandRequest.isQueryText` in `src/Shared/CommandRequest.fs` is true for text that contains `=` and does not start with `?`. `submitQuery` in `src/Client/Commands.fs` runs before the `>` check.
[ ] The [Amble language roadmap](../../doc/roadmap/language-syntax-and-semantics.md) describes `>` as a shell command with argv tokens, embedded expressions, redirections, and pipes.
[ ] [22 — Run sends = lines to Amble](../online-search/issues/22-run-sends-equals-lines-to-amble.md) puts `>` lines out of scope.
[ ] The Glossary defines the enclosing artifact and the Directory Node. See [GLOSSARY.md](../../GLOSSARY.md).

## 5. Open questions

Nothing in this section is decided. A ticket that depends on a question has Status `needs-info`.

### From Alan's brief

1. Child shape — Does each stdout line become one child, or does the actor parse stdout as an outline?
2. stderr — What happens to stderr?
3. Exit code — What happens to a non-zero exit code?
4. Timeouts — Does a command have a timeout? If it does, what is the limit and what happens at the limit?
5. Re-run — What happens when the focus node already has children?
6. Directory of a non-file node — Where does the directory come from for a node that is not a file?

### From the existing Run, path, and actor docs

7. Precedence over `=` — A `>` line that contains `=` is query text today, and `submitQuery` runs first. Which rule wins for such a line?
8. Leading whitespace — `shouldExec` trims the start of the text. Does the actor need `>` as the first character, or may whitespace come before it?
9. Existing `>` meaning — Run treats a `>` line as an Amble shell statement today. The roadmap describes argv tokens, embedded expressions, redirections, and pipes. Does the command-line actor replace that meaning, or does it run beside it?
10. Shell text — Does the shell receive the text after the `>` unchanged, or does Ambit expand node references first?
11. Which shell — Which shell program runs the text on the Azure host, and which environment variables does the command see? Azure App Settings are environment variables. They hold secrets, as in [Secrets](../../doc/reference/secrets.md).
12. Who may run — Which people may run an arbitrary shell command on the Azure host?
13. Where it starts — Today a `>` line runs on the client. The actor runs on the server. Which start path does the actor use, and what is its name?
14. Not on Azure — What does Run do on a `>` line when the Server is not in the Azure environment, for example in a local development run?
15. Owner scan — [22 — Run sends = lines to Amble](../online-search/issues/22-run-sends-equals-lines-to-amble.md) scans owners upward for `?` and `=` lines. Does that scan include `>` lines?
16. Special focus — `shouldExec` is false for a Special focus node. Does a File Node, Directory Node, or Workspace Node whose text starts with `>` run the actor?
17. Live actor — [Run](../../doc/user/reference/commands/run.md) fails a second query Run on a line that has a live Actor. Does the same rule hold for this actor?
18. Cancel — Cancel stops the live run on the focused node. Does Cancel stop the process and the processes it started?
19. Output cap — Does stdout have a size cap, as one run has a cap of 50 Answers in [25 — The server evaluates Run](../online-search/issues/25-server-evaluates-run.md)?
20. Empty command — What happens when the text after the `>` is empty?
21. Placement — Does this Project belong under an Epic or Chapter on the Roadmap?
