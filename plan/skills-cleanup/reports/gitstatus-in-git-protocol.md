# gitstatus in git-protocol

[[.agents/skills/git-protocol/SKILL.md]] now makes [[scripts/gitstatus.sh]] the first git step: run it from the project root with no arguments. The script is `git status --short --branch` only; the skill does not invent flags. [[scripts/_git-protocol.sh]] is the merge-script helper, not the skill implementation, so wrappers were left alone. The skill change is on `dev`. [[.cursor/rules/core-agent-behavior.mdc]] still names `./status.sh`; that rule was out of this surgical edit.
