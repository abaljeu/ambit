# Ask / Run seam facts

Facts for grilling [[../issues/03-seam-after-ask-recognition.md]]. No Answer on that ticket. Map: [[../map.md]].

## 1. Issue 33

[[plan/expression-language/issues/33-recognize-ask-run-statement.md]] **Status:** `ready-for-agent`. No Answer. No Comments. Checkboxes open.

It commits: a Focus line that starts with `?` plus a message is a Run statement, not a no-op; `=` and `Name=` stay as they are; the message is not an Expression.

It does not own pack, LLM call, included context, or reply Children. Those belong to [[../project.md]]. Chapter [[plan/roadmap/epics/chapters/ask-from-what-i-see.md]] lists 33 as recognition only.

[[plan/expression-language/spec.md]] chapter 8 still names two Run line forms: `= Expression` and `Name=Expression`. Other lines are not statements. `?` is not in that chapter.

## 2. How Run executes today

User command: [[src/Shared/CommandEntry.fs]] `CommandId.Exec`, name Run. Browser handler: [[src/Client/Commands.fs]] `execRunOp` → [[src/Client/UpdateAmbleRun.fs]] `runAmbleOp` / `applyRunPlan`.

Shared plan: [[src/Shared/AmbleRun.fs]] `shouldExec`, `runPlanOnNode`, `runPlan` → [[src/Shared/ExprRun.fs]] `classify`, `isRunStatement`, `run`. `ExprRun.Line` is `Ignore` or `Apply` of `ExprRun.Plan` (`ops`, `unfold`). Input is the Focus Node Header text.

If `Ignore` and the line starts with `>`, [[src/Shared/AmbleRun.fs]] `legacyRun` uses [[src/Shared/AmbleParse.fs]] `parse` and [[src/Shared/AmbleEval.fs]] `evalStatement` on [[src/Shared/AmbleTypes.fs]] `AmbleStatement` (`Assign` | `ExprStmt`).

Non-empty `Plan.ops` become a Change. `applyAndPost` applies locally and Sync posts to Server `POST /ambit/changes` ([[src/Server/Api.fs]] `postChange` → Core `CoreChanges.postChange`). Server and Core do not classify or eval the Run line.

Special Kind Focus: `shouldExec` is false; plan is empty.

## 3. `?` in code

No Run-statement `?`. `ExprRun.classify` looks for `=`. A line with no `=` is `Ignore`. `AmbleStatement` has no Ask case. No tests treat `? …` as a Run statement.

Other `?`: glob one-character wildcard in [[src/Shared/RefExprMatch.fs]]; Expression glob retired that (issue 17). Filename forbids `?`. Not Run.

## 4. Object if 33 lands as specified

A third Run statement: Focus line starts with `?`; message is the rest of the line; not Expression; not LLM; not included context; no reply Children.

Run has no statement AST today. Recognition is `classify` / `isRunStatement` on Header text, then `ExprRun.Line`. 33 does not name a new DU, a `CommandId`, or a Core `LaunchRequest`.

## 5. Actor launch from a command

Core API Command is [[src/Server/Core/CoreActorPool.fs]] on [[src/Server/Core/CoreRuntime.fs]] `command`. `LaunchRequest` is `ActorName`, `Revision`, [[src/Shared/Model.fs]] `NodeRange`. `launch` starts a registered `ActorFn`. Contract: [[plan/core-creation/issues/09-define-core-command-launch-contract.md]] (resolved). Command has no Parse, shell, or Agent cases.

`register` and `launch` callers are [[tests/Server.Tests/CoreActorPoolTests.fs]] only. [[src/Server/RouteRegistration.fs]] does not expose `Core.command`. Browser Run (`Exec`) does not call `launch`. [[../issues/02-which-llm-and-credentials.md]] postpones the user action that launches the Actor.
