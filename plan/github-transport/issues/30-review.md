# 30 — GitHub save path: missing workspace upward scan and workspace upload

Ticket: [[30-github-save-path-missing-workspace-upward-scan-and-workspace-upload.md]]
Spec: [[plan/github-transport/spec.md]]
Arch: [[plan/github-transport/arch.md]]

## 1. Standards

1. **Bare id on spec item 11.** **Hard.** `.agents/rules/refer-by-name.md` says never identify anything only by number, and always include a name. `python3 scripts/standards-scan.py --diff origin/staging` prints `plan/github-transport/issues/30-github-save-path-missing-workspace-upward-scan-and-workspace-upload.md:33` `BARE_ID` `item 11`. That line says "Spec item 11" and links the spec file. The spec line's name is "git Save is commit then push".
2. **Scan limits.** The scan prints seven bindings in `src/Server/GithubTransportActor.fs`, each inside the 40-line limit in `.agents/rules/fsharp-source.md`: `actorOutcome` 5, `finishActor` 10, `readWorkspace` 15, `persistFocus` 8, `commitPullPush` 10, `runSaveBody` 15, `runLoadBody` 21. The other scan lines stay inside the 100-character line limit.

## 2. Spec

1. **Four stages against spec story 11 — git Save.** **Wrong.** Spec story 11 — git Save and story 27 — Persist stays independent of git Save say commit then push, and say git Save does not own Persist. `runSaveBody` calls `persistFocus`, then `commitPullPush`. `commitPullPush` calls `git.commitAll`, then `git.pullTracked`, then `git.pushTracked`. Ticket 30 Check orders persist focus, commit, pull, push, and orders that spec text to stay.

## 3. Arch

1. **Save persists and pulls.** **Blocker.** Arch §1 story 11 — git Save items 4 to 6 say the actor commits, then pushes, and does not invoke Graph→file Persist. Arch §1 story 27 — Persist stays independent of git Save items 2 to 4 say the composition is commit then push. Arch §2 module WorkspaceGit interface items 12 and 17, module Server Actor interface item 7, and §3 seam WorkspaceGit git facts items 4 and 8 say commit then push and no Persist call. `persistFocus` calls `DocumentPersistChange.persistNode`. `runSaveBody` calls `persistFocus`, then `commitPullPush`.

## 4. Tests

1. **Suite.** `scripts/check-test.sh 2940` exits 126 and prints `Permission denied`. `bash scripts/check-test.sh 2940` prints `DONE` and `/tmp/ambit-full-suite-20261108-160148.log`. The log ends `full suite finished`. `tests/Server.Tests` passed 755, skipped 1, failed 0. `tests/Shared.Tests` passed 1937, skipped 3, failed 0.
2. **Gate.** ``git Save gates commit then releases before push`` expects gate acquire, commit, pull, gate release, push, on the workspace root, and `ActorSucceeded`.
3. **Persist.** ``git Save persists focus then commits pulls and pushes`` expects `note.txt` on that root at commit time, the same step order, and `ActorSucceeded`, with focus on the owned file.
4. **Pull failure.** ``git Save stops when the pull fails and does not push`` expects `ActorFailed` with the pull error, and the steps commit, pull, and no push.

## 5. Miss

1. **Four stages against spec story 11 — git Save.** The diff persists the focus and pulls before push. Spec story 11 — git Save and story 27 — Persist stays independent of git Save still say commit then push, with Persist off git Save.
2. **Save persists and pulls.** The same diff breaks Arch §1 story 11 — git Save, story 27 — Persist stays independent of git Save, and the WorkspaceGit and Server Actor interface lines that say commit then push.

## 6. Found elsewhere

The Found elsewhere section takes the PR body's Found elsewhere list.

1. **check-test.sh mode.** Accepted. **Problem.** `scripts/check-test.sh` is mode 644. **Fix.** Mark it executable so `scripts/check-test.sh <pid>` runs.
2. **Absent actor notes.** Accepted. **Problem.** `plan/actor-as-client/reports/startactor-deprecated-gap.md` and `plan/actor-as-client/reports/git-load-and-save-actor-functions.md` are not on this branch. **Fix.** Leave them on their documentation draft. This build does not depend on them.
3. **FirstFactorStart.** Accepted. **Problem.** Spoken name `FirstFactorStart` is not in `src/`. **Fix.** Do not add a type for that name until the source name is known.
4. **Ticket 10 — git Save is commit then push.** Accepted. **Problem.** [10 — git Save is commit then push](10-git-save-commit-then-push.md) does not name persist or pull. **Fix.** Leave that lock as written. This ticket is the later sequence.
