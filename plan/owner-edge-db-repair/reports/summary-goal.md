# Summary goal rewrite

## New Summary

Persisted Owned Children can fail to be a tree. After Server restart, every surviving non-ROOT Node has exactly one Owned parent that reaches ROOT, unreachable Nodes are deleted, and a reachable Node with no Owned parent has a Ref promoted to Owned, durable with no History Change.

## Why it matches wait-what and CONTEXT

[[.agents/skills/wait-what/SKILL.md]] asks for a re-pitch with a little context, ASD-STE100, and ubiquitous language from [[CONTEXT.md]]. The first sentence is the context: persisted Owned Children can fail to be a tree. The second sentence is the goal: what the operator has after Server restart, not the DbAgent sweep recipe.

Words follow [[CONTEXT.md]]: **Owned** (not Owner), **Ref**, **ROOT**, **Graph** (spoken as the persisted Graph via Owned Children, not `node_children`), **Server**, **Node**, **History**, **Change**. It does not say Owner, tree as a synonym for Graph, GC, ACID, or projection-maintenance steps.

## What changed

In [[plan/owner-edge-db-repair/project.md]] only: rewrote `Summary:` as that goal re-pitch; set `Updated: 2026-09-02`; left `Stage: active`.
