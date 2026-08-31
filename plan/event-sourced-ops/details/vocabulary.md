# Vocabulary

The locked terms of increment 1, plus the words this project deliberately refuses. Top layer: [[plan/event-sourced-ops/overview.md]]. Protocol: [[plan/event-sourced-ops/architecture.md]].

Do not add these terms to [[CONTEXT.md]] yet. They are a project vocabulary, not yet project-wide language.

## Locked terms

| Term | Sense |
| --- | --- |
| **Op** | The mutation. The only mutation. |
| **Change** | A collection of Ops. |
| **Actor** | Anything that produces a Change, synchronous or asynchronous. It may have little or no Local Graph — Parse and later agents are the same kind as a user-edit Actor. |
| **Subgraph** | Part of the Graph: the Nodes this process has. The database has the full Graph. |
| **Local Graph** | This process's graph state. Not a second type. |
| **Local Subgraph** | The same Local Graph, when the point is that it may be incomplete. Today the Server is complete and the Browser is always a Subgraph. |
| **Common prior** | The Local Graph a Change was planned against. The base a merge reasons from. |
| **Merge** | To apply Changes into the Server Graph or a Client Local Graph. **Not** a three-way merge in the git sense. |
| **Amend** | To rewrite the newest Actor's Change so it fits the common prior plus the other Actors' accepted Changes. |
| **Newest** | The Change being merged now. Server arrival decides which Change is first. |
| **Baseline** | The point a Client catches up from. In practice the last revision it received from the Server. |
| **Soft lock** | An advisory reservation of a subtree by a long-running Actor. See [[soft-lock.md]]. |

## Documented, not new law

These words describe the software as it is. They are recorded so that merge does not contradict reality.

| Term | As-implemented sense |
| --- | --- |
| **Orphaned** | A subgraph that Owned child references cannot reach. It stays in the Graph until garbage collection. Not in [[CONTEXT.md]]. |
| **TRASH** | The recycle bin. Its children are Owned by TRASH, so they are **not** Orphaned. |
| **Single owner** | Every Node has one Owned parent, except ROOT and except Orphaned Nodes. |

## Words this project refuses

- **Deleted**, as a state of the Graph. Say **Orphaned**.
- **View**, **branch**, **trunk**. Do not say **checkout** in the git sense; a soft lock is advisory.
- **DocumentState** as a field. It is removed from this standard: no-server-file is inferred from whether the file exists, and unparsed is inferred from the relative dates of the file and the File Node. The implemented field remains a fact until it is removed.
- Worker slang about wiping a pending queue. Not a design case; see [[decision-log.md]].

## Open, with no stake

[[CONTEXT.md]] treats an Action as a History entry — a Change, an Undo, or a Redo — and there is no `Action` union. Actors produce **Changes**; Action stays History and undo speech. This fork is left unpicked on purpose.
