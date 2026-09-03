# Summary goal re-pitch

## New Summary

Summary: A person uses a Ref in Children to link to a Node Owned elsewhere in the Graph; this Project makes Delete unlink that appearance from Children and leave the Node in place, and makes Delete of an Owned Node with a self-Ref finish: the command must not hang and must not promote the self-Ref.

## Why it matches wait-what and CONTEXT

[[.agents/skills/wait-what/SKILL.md]] asks for a re-pitch with a little context, ASD-STE100 Simplified Technical English, and ubiquitous language from [[CONTEXT.md]].

The first clause is the context. A Ref in Children links to a Node that is Owned in another place. That is why Delete of a Ref is not the same as Delete of the Owned placement: the person drops one appearance and the Node stays in the Graph.

The rest of the line is the goal, not ticket status. Delete of any Ref unlinks that appearance from Children. Delete of an Owned Node that has a self-Ref must finish. The command must not hang. The command must not promote the self-Ref (a self-Ref is not a second home).

The line uses **Ref**, **Owned**, **Node**, **Children**, and **Graph**. It does not use "soft link", "alias", "hard link", or "pointer".

## What changed

In [[plan/delete-ref/project.md]]: replaced the prior Summary (intended behavior with no why) with the wait-what re-pitch above. Set `Updated:` to 2026-09-02. Left `Stage:` as `active`.
