---
name: retro
description: Examine the agent's work log, surface problems, and propose improvements.
disable-model-invocation: true
---

# Retro

**Friction** is a turn that cost a redo, a search or tool call past a result the log already held, a question whose answer the repo already held, or an edit a later turn undid. The work log is the session transcript and every subagent transcript that session dispatched. A session the human names is the work log. No name means the current session.

## 1. Read the work log

Read the work log through its last turn. Done: the session has a name, the last user turn and the last assistant turn can be quoted, and every dispatched subagent transcript has been read.

## 2. Surface problems

List each friction as one problem. Cite the turn. Only turns that match the friction definition appear. An empty list is complete. Done: every friction in the work log is one problem, and each problem cites its turn.

## 3. Propose improvements

Give each problem one improvement. The improvement names the change and the path that would hold it. Order by how many turns show the problem, highest first. The reply is that list. Done: the human has the ordered list, and every file matches its contents at the start of this skill.
