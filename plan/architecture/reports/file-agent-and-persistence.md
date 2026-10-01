# File Agent And Persistence

Based on facts, we need to correct the reference files.

## Facts

We have dbagent.fs and fileagent.fs.
And we have concepts of:

Graph - an in-memory data store
Event Queue - an in-memory record of happenings.
Parse - a process of turning disk information to graph, 
Persist - a process turning graph information to filesystem held data(disk) , 
Database - the authoritative place where graph and event info is permanently kept.

The .amb file still is a total graph persistence format, but it is not used for the whole graph.
The program will start with database offline, reading file data to recreate the graph, but this is partial and not used for editing.

## Obsolete Notions

Once there was an explicit "save all graph info to disk", creating a single large file.  This is no longer in place.
Once there was a swtich to change from disk-based to db-based.  This is no longer.
Once fileAgent and dbAgent were fully parallel; now what is common should have been factored out so only distinctives between one mode and the other remain.

## Research 

Exact phrase `"Core owns the mailbox and selects one persist filling"` exists only in:

- `d:\dev\amble\gambol\doc\current\core.md` line 6 (lede; same claim the user pointed at as line 7)

No other file has that full sentence.

### What `core.md` did with older wording

It **tightened older sentences**, not a copy from `server.md` / `plan/architecture/server-core.md` / `plan/core-refinement/arch.md` (those lack this claim).

Nearest older committed sources:

1. `doc/Decisions/0003-core-is-a-container-of-subobjects.md` line 17–19 (commit `e13c89e7`, 2026-09-05):
   - *"Core selects between them."*
   - *"persistence-mode selection"*

2. `plan/core-creation/issues/31-one-coremsg-loop-parameterized-persist.md` lines 28–30 (phrase land commit `dbe46bf8`, 2026-09-12):
   - *"File and Db are two fillings."*
   - *"Two persist fillings — FileAgent and DbAgent…"*

Same Write also put the fuller form on `core.md` line 12:
*"Core holds one persist filling. Core selects the Db agent or the File agent."*
and the sibling lede on `file-agents.md` line 18:
*"Core selects one persist filling."*

Mailbox sibling is different: `mailbox.md` line 6 says *hosts* one persist filling, not that Core selects it.

### Who introduced it

- **`doc/current/core.md` is untracked** (`git status`: `??`; not in HEAD).
- Introduced today (~11:51) by the Core-page subagent in [Write Core architecture page](fbd63f2c-8853-4d56-aa41-00ee3a58356b) (`b52350d2…`), which wrote the lede in one shot when creating the Core hub pages.

No line deleted.