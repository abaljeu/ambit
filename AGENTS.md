# Required of Agents

# All Agents

# BEFORE YOU DO ANYTHING ELSE
You must write on the correct branch. [[.agents/skills/git-protocol/SKILL.md]].

Don't use SageFs.

## Concept
This repo builds Ambit.  Never called Gambol.


## Interpretable Context Methodology
Rather than central skills, how to work on files is mostly located with those files.

Read [[CONTEXT.md]]. Once you are inside a top-level folder, that folder's CONTEXT.md takes over.

## Communication
A question is a question, not a request to change.  Research and answer the question.
Long output over 50 lines is hard to follow in chat. Refer the user to a file.  report/ within a project folder is good place.  Never edit reports made by another.

## Duplication
We aim to reduce this repo's size by 50%, without removing content.  If you spot opportunities outside your scope, report them.  If you spot opportunties within scope, reduce.

A major method is to reduce duplication.
In markdown, this usually means reference rather than repeat.
In source, this usually means refactoring out the duplication.  There are many ways, such as pull-up, extract-method, abstract.

## Writing
You are not permitted to work in every branch.  If the wrong branch, stop.  Read more: [[.agents/skills/git-protocol/SKILL.md]].

Refer to [[GLOSSARY.md]] for terms.

Follow [[.agents/rules/refer-by-name.md]].


MarkDown: Follow [[.agents/rules/writing-markdown.md]].
F#: follow [[.agents/rules/fsharp-source.md]] and [[.agents/rules/core-api.md]].


## Multitasking
Delegate to a Grok family model.

Main agent must always read [[.agents/rules/main-agent-behavior.md]].
Sub agents must always read [[.agents/subagents.md]].  MAIN AGENTS PASS THIS IN!  It may instruct the agent to write a report file.

## Beta testing the skills.
We deleted all the skills, and are starting fresh.  Re-adding things as needed.

Therefore: If a referenced markdown file is missing, you must stop and point it out so things can be improved.
