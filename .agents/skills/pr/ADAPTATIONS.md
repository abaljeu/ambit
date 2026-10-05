# Adaptations

Upstream is mattpocock/skills `skills/engineering/pr` (v1.3). Steps live in [[SKILL.md]]. This note tells the next editor what was adapted.

## Credits

The Summary visual menu (pseudocode, call tree, file tree, Mermaid, diff sketch) comes from Dex Horthy's `show-me` skill (Humanlayer). [[SKILL.md]] keeps a short copy of that menu. This skill does not install `show-me`.

## Kept

- Three sections: Summary, Evidence, Merge Danger.
- Model-invoked when an agent writes or updates a pull request body.
- One-way door versus two-way door, plus blast radius, so the human knows where to spend review time.

## Skipped or redirected

- No mattpocock plugin install.
- Domain words come from [[GLOSSARY.md]]. Upstream v1.3 renamed `CONTEXT.md` to `GLOSSARY.md`. The root glossary already uses that name. Many skills still say `CONTEXT.md`, and [[.agents/skills/domain-modeling/SKILL.md]] still writes that path. [[doc/current/CONTEXT.md]] is architecture page rules, not the glossary. This skill does not run the rename.
- Standards and Spec review stay [[.agents/skills/code-review/SKILL.md]]. This skill does not repeat that review.
- Git steps stay [[.agents/skills/git-protocol/SKILL.md]] and [[.agents/skills/cloud-agent-git/SKILL.md]]. Day-to-day land on GitHub `abaljeu/ambit` is a squash onto `staging` after Alan approves. This skill does not merge.
- [[.agents/skills/resolving-merge-conflicts/SKILL.md]] stays. Upstream deleted that skill. Ambit did not.
- The Jev match-check is Ambit-only. Upstream has no Jev step.
- The path is listed in [[.agents/rules/gambol.md]] after code-review. [[.agents/skills/ask-matt/SKILL.md]] does not copy that catalog, so this change does not edit ask-matt. [[.agents/skills/implement/SKILL.md]] already names code-review in its check step, so that step has one pointer here.
- Examples stay generic. They do not restate Project vocabulary locks.

## Neighbours

After [[.agents/skills/implement/SKILL.md]] and [[.agents/skills/code-review/SKILL.md]]. Before a human land. [[.agents/skills/retro/SKILL.md]] is the later human invoke. Also [[.agents/skills/git-protocol/SKILL.md]] and [[.agents/skills/project-work/SKILL.md]].
