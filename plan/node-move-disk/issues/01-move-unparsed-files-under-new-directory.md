# 01 — Moving never-parsed File Nodes under a new Directory Node leaves the files behind

**Type:** bug
**Status:** defined
**Blocked by:** None

## Context

A person creates a new Directory Node, then moves existing File Nodes under it. Those File Nodes have never been parsed.

## Observed

The File Nodes sit under the new Directory Node in the Graph. Their files on disk stay where they were and do not move into the new directory.

## Expected

A File Node's file directory comes from its strictly enclosing artifact. Moving a File Node so that its strictly enclosing artifact is a different Directory Node or Workspace Node moves the file on disk into that node's directory. This holds whether the File Node lands as a direct child or deeper in the Body, and whether or not it has been parsed.

## Notes

- 2026-10-29 — Reported by Alan from interactive use. Cause not yet investigated.
