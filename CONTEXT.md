# Gambol

Concise glossary for this repo. Prefer these words; do not invent synonyms.  If a new term seems to be needed, raise the issue.

## About Working

**Agent-done**:
A finished slice on a project branch: tests green, `/code-review` passed, and a commit on the current `w/*` branch. Tickets do not record commit SHAs.
_Avoid_: done, finished, shipped, complete

**Original branch**:
The human-owned long-lived line that is not main or master; the human squashes onto it.
_Avoid_: base branch, long-lived branch, integration branch, feature branch

**Project branch**:
The agent's default workplace branch, always prefixed `w/`. The agent may create or check out `w/<slug>` once from HEAD when not already on `w/`, then stays there.
_Avoid_: work branch, agent branch, workplace branch, scratch branch

**Git bookkeeping**:
The file `.scratch/<feature>/git.md` that records the current project branch, the original it was cut from, and short notes.
_Avoid_: branch notes, git status file, branch tracker

**Manual approval**:
A direct user request that authorizes a named git operation otherwise off-limits (for example checkout of another branch, merge, rebase, reset, clean, remotes, or writes to main/master).
_Avoid_: permission, override, allowlist exception

**Issue tracker**:
Local Markdown under `.scratch/` for specs and issues; see [[docs/agents/issue-tracker.md]]. Not GitHub or GitLab issues.
_Avoid_: backlog, GitHub issues, GitLab issues, tickets board

**Committed Decision**:
A record under [[docs/Decisions/]] of a choice that is costly to reverse, surprising without context, and made between genuine alternatives. The mattpocock skills call this an ADR; in this project always say Committed Decision.
_Avoid_: ADR (outside vendored skills), architecture decision record, decision record

## About the Software

**Graph**:
The editable structure: a root and the nodes reachable from it, with ownership and ref links among those nodes.
_Avoid_: tree, document tree, model, outline

**Node**:
One addressable unit in a Graph, consisting of a Header and Children.
_Avoid_: item, bullet, line, row, entry

**Header**:
Everything in a Node except its Children.
_Avoid_: metadata, node body, properties

**Children**:
The child appearances under a Node (Owned and Ref roles).
_Avoid_: kids, subordinates, child list (as a synonym for the Children themselves)

**Bullet**:
The visual glyph element every Node view shows at its left edge, rendered as a fold chevron, a solid circle, or a hollow circle. A Bullet marks a Node's appearance in the view; a Node is not a Bullet.
_Avoid_: leaf, leafBullet, node marker, dot, tooltip target (as names for this element)

**Owned**:
A child appearance that is a Node's single structural placement in the ownership tree. Prefer this over the code case name `Owner` in speech and docs.
_Avoid_: Owner (spoken synonym for this role), hard link, parent link

**Ref**:
A child appearance that links to a Node Owned elsewhere; it does not place the Node in the ownership tree.
_Avoid_: soft link, alias, pointer

**Kind**:
A Node's classification: Normal, or a Special kind.
_Avoid_: type, class, category

**Normal**:
The default Kind: ordinary outline content with no special structural role.
_Avoid_: regular, plain, standard

**Normal Node**:
A Node whose Kind is Normal. Always say Normal Node, not bare “normal,” when referring to the Node.
_Avoid_: normal (bare, for a Node), regular node, plain node

**Special**:
Any Kind that is not Normal; a structural or system role.
_Avoid_: system node, meta node

**Workspace Node**:
A Node whose Kind is Workspace; maps to a workspace directory on client computers. Always say Workspace Node, not bare “workspace,” when referring to the Node.
_Avoid_: workspace (bare, for a Node), project, vault, folder

**Workspaces Node**:
The unique Node that contains Workspace Nodes.
_Avoid_: Workspaces (bare, for this Node), workspace list, workspace root

**Directory Node**:
A Node whose Kind is Directory; corresponds to a server directory plus that directory's `.amb` file (`DirName/.amb`). Always say Directory Node, not bare “directory,” when referring to the Node.
_Avoid_: directory (bare, for a Node), folder

**Directory File**:
The `.amb` Document that belongs to a Directory Node or Workspace Node (root `.amb` or `DirName/.amb`). It is that node's Document artifact, not a File Node child. Cold bootstrap that reads only Directory Files leaves other File Nodes Unparsed until Parse.
_Avoid_: Marker (for this concept), marker file, directory marker, amb marker, marker-only load (prefer Directory-File-only / Directory File cold load)

**File Node**:
A Node whose Kind is File; a Graph node that stands for a real on-disk file, identified by a relative path (e.g. `SYSTEM/user.css`). Always say File Node, not bare “file,” when referring to the Node. Cold load / stub: know the path exists and create the File Node without reading the file's text yet (Unparsed). After reading/parsing that file's text, it is the same File Node. Prefer “the file” at that relative path — not “file body.”
_Avoid_: file (bare, for a Node), document, page, note, file body

**ROOT**:
The unique nameless Workspace Node at the Graph root.
_Avoid_: root node, graph root

**TRASH**:
The unique Directory Node that acts as a recycle bin for deleted Nodes.
_Avoid_: trash node

**Loaded**:
A Node whose Children are present.
_Avoid_: lazy, expanded, hydrated (for this meaning)

**Unloaded**:
A Node whose Children are not present.
_Avoid_: stub, hollow, partial, collapsed (for this meaning)

**Resident**:
A Node whose Header is present in this Graph. Children may still be Loaded or Unloaded.
Antonym: **Absent**. _Avoid_: materialized, present, cached (for this meaning)

**Server**:
The server project and the server process it runs.
_Avoid_: backend, API host (as casual synonyms)

**Browser**:
Spoken name for the Client project: the browser-side code. Both the App and a web browser are clients of the Server, so do not say Client for this project.
_Avoid_: Client (in speech), frontend, web app

**App**:
Spoken name for the Desktop project: the desktop host that contains a browser; also a client of the Server.
_Avoid_: Desktop (in speech), shell, host app

**Shared**:
Projects (`Shared` and `Shared/dotnet`) whose code is shared across modules and tests.
_Avoid_: common, core, lib

**Document**:
The project responsible for loading and saving between Graph and file.
_Avoid_: codec package, documents project, parsers (as the project name)

**Load**:
A user-facing command that runs up to three operations in sequence: Upload, Parse, then Fetch. Often only one of the three applies for a given run. The final stage Fetches part of the Graph and also Polls updates.
_Avoid_: Upload (for the command), Download (for this command), sync (for this command)

**Fetch**:
A Load-stage operation that brings a subgraph from the Server into the Browser Graph (residency). Load's final stage pairs Fetch with Poll updates.
_Avoid_: Download (for this stage), pull, materialize

**Upload**:
A Load-stage operation that pushes App files to the Server. Not the user-facing command name.
_Avoid_: Load (for this stage), push (as the stage name)

**Parse**:
A Load-stage operation that turns server files into Graph content.  This step is more than simple parsing but also reconciles pre-existing graph content with file content.
_Avoid_: import, reconcile (as the stage name)

**Download**:
A user-facing command that downloads files from the Server. Not Fetch.
_Avoid_: Fetch (for this command), pull (as the command name)

**Change**:
A Graph modification unit: typically produced by a user command, applied by both Browser and Server to update their Graphs. A Change is multiple Ops. One kind of Action.
_Avoid_: mutation, edit, transaction, patch (as synonyms for Change)

**Op**:
A single Graph modification, either to a Node Header or to its Children.
_Avoid_: operation (casually for Change), mutation, edit

**Action**:
A History entry: a Change, an Undo, or a Redo.
_Avoid_: operation, event (as synonyms for Action)

**Undo**:
An Action that reverses a prior Change, following Emacs undo semantics; numbered like other Actions.
_Avoid_: revert, rollback

**Redo**:
An Action that re-applies after Undo, following Emacs undo semantics; numbered like other Actions.
_Avoid_: un-undo

**Revision**:
The number of an Action (Change, Undo, or Redo).
_Avoid_: version, sequence number, change id (for this integer)

**History**:
A log of Actions.
_Avoid_: undo stack, change log (as a synonym for History)

**Sync**:
Keeping Browser and Server Graphs aligned by exchanging Actions (and related residency work). Not a synonym for Load.
_Avoid_: Load (for this meaning), reconcile (as a synonym for Sync)

**Poll**:
A Browser request for Actions since a known Revision in History; used in Sync and also as part of Load's final stage with Fetch.
_Avoid_: sync (as a synonym for Poll), fetch (for this meaning)

**Expression**:
A non-deterministic predicate over the Graph that can yield many Answers. Most Expressions find a Node; text and numbers are also in scope.
_Avoid_: query (as the language name), FunCall, RefExpr (that is the path subset)

**Answer**:
One possible value of an Expression: a Node, text, or a number. Failure produces no Answer. Boolean succeed and fail are control, not an Answer type.
_Avoid_: result, solution, match (as this value)

## Additional approved terms
These terms are permitted with standard definition:

- **SiteMap**: the client's derived view index over the resident Graph.
- **ChangeRequest**: the client's pending-queue and submit-payload unit (Change, Undo, or Redo).
- **StateResponse**: the `/state` endpoint's response payload.
- **ChangeLog**: the server's durable ordered log of Changes.
- **Session**: one webpage lifetime from load to refresh or close.
- **Selection**: the set of Nodes a user has currently selected.  It will always be a range of children of a node.
- **Focus**: the active node.  It will always be the first or last of selection.
- **Zoom**: the command that focuses the view on a Node.
- **Find**: the command that searches the resident Graph.
- **Fold**: the collapsed/expanded display state of a Node's Children.
- synchronization: plain noun form of **Sync**, same meaning.

## Additional Unwanted terms
- affordance
- **Marker** (for `.amb` Directory/Workspace documents, or “marker-only” cold bootstrap) — deprecated; say **Directory File**