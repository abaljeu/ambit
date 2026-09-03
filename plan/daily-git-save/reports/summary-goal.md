# Daily git save — Summary goal

## New Summary

Summary: The Server saves Graph documents in App DataDir. Commit that directory each day so the operator can recover those files from git without a manual commit.

## Why wait-what and CONTEXT

[[.agents/skills/wait-what/SKILL.md]] asks for a re-pitch: a little context, ASD-STE100 Simplified Technical English, and ubiquitous language from [[CONTEXT.md]].

Context: the Server writes Graph documents to App DataDir. That is the primary file-content store, not Gambol's own git places (`dev`, `ready`, `master`).

Goal: the operator gets a daily git history of those files and can recover them without a manual commit. Two short sentences. Active voice. No stamp-file recipe.

Terms from [[CONTEXT.md]]: Server, Graph, document, App. DataDir is the on-disk store for that save. Nested workspace repos sit under DataDir; they are how, not why.

## What changed

In [[plan/daily-git-save/project.md]]:

- Replaced the old Summary (UTC day, listen, DbAgent, `commitAll`, stamp `SYSTEM/gambol.git-save-day`) with the goal line above.
- Set `Updated: 2026-09-02`.
- Left `Stage: active`.

This report is new. No other files.
