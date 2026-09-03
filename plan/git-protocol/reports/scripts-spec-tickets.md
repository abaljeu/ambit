# Scripts spec tickets

Date: 2026-09-02. No commit. No script implementation.

## Issues created

One issue. The spec is one rename/wrapper slice.

- [[plan/git-protocol/issues/04-named-ux-scripts.md]] — 04 — Named UX scripts for git-protocol moves. Status `ready-for-agent`. Spec: [[plan/git-protocol/scripts-spec.md]].

## Stage

[[plan/git-protocol/project.md]] stays `Stage: active`. This is more work on an already-active Project, not a new spec effort. Project-work sets Stage when an effort starts or advances; it does not send an active Project back to `spec` or `tickets` for one added slice.

Summary was not changed. The project goal is still the git procedure. The named scripts are UX for that procedure.

`Updated:` is already 2026-09-02. [[plan/index.md]] was not regenerated (Stage did not change).

## Ambiguity

Spec writes `gitdev.sh "dev"` and `merge.sh forward dev`, then `i.e. forward-merge the squash from master to dev`.

[[scripts/merge.sh]] has no `forward dev`. `forward [master|ready]` takes the source place. Default is `master`. It uses stock messages and does not take `-m`. `"dev"` is not a commit message (unlike `"desc"` on the other lines). The issue implements `gitdev.sh` as `forward` from `master` with no dev/desc argument.

Spec writes that [[scripts/gitpush.sh]] does what push.sh did, then `Switch to master or ready and push it`. [[scripts/push.sh]] takes `ready` or `master` and runs `git push origin` for that place. It does not switch HEAD. The issue keeps that behavior and does not add a checkout.

Left [[plan/git-protocol/scripts-spec.md]] as the user's artifact. Did not fix `whate` or `"dev"` there.

## What I did not do

- Did not implement the scripts.
- Did not edit [[.cursor/skills/git-protocol/SKILL.md]] or other skills.
- Did not edit [[scripts/commit.sh]], [[scripts/merge.sh]], or [[scripts/push.sh]].
- Did not retire old script names.
- Did not commit.
