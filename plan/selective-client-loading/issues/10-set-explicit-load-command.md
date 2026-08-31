# Set explicit Load command responsibility

Type: grilling
Status: resolved
Blocked by: 03

## Question

Should the existing Upload command be renamed or extended, or should client residency loading remain a separate command; what entity and scope should explicit Load target; and how should it sequence with optional desktop push, server-disk reconciliation, and parsing while preserving current platform behavior?

## Answer

- Rename the existing user-facing Upload command to Load and preserve its `Ctrl+Shift+>` binding. Upload, parse/reconcile, and client download remain separate functions behind one contextual command.
- Load takes the full selection. Normalize every selected occurrence to its nearest canonical Workspace, Directory, or File artifact and deduplicate; a Ref follows the referenced node's canonical Owner chain rather than the occurrence's displayed ancestry.
- Every normalized artifact must belong to one Workspace. Reject a cross-Workspace selection rather than processing it partially. A Workspace target requires `Workspace` mode and every other artifact requires `ArtifactClosure`; reject a selection that mixes those modes before any pipeline stage starts.
- Run three stage-wide steps in order: upload applicable desktop files, apply the existing server parse or reconciliation behavior, then request the normalized targets in their one accepted mode for the client. Each step skips targets to which it does not apply.
- Preserve current platform behavior: when desktop push is unavailable or a scope has no local mapping, skip that step but continue with server-side parse or reconciliation and client download.
- Preserve current parse boundaries. A File or one of its subnodes parses that owning File; a Directory or Workspace performs its current structural reconciliation and never recursively parses contained Files.
- If an upload, parse/reconciliation, or download stage fails, retain completed earlier effects without rollback, stop the pipeline, and do not run later stages.
- Preserve invoking the command on the Workspaces container: choose one desktop folder, create one Workspace, continue the pipeline for it, and download it in `Workspace` mode.
- `ArtifactClosure` stops after including a nested artifact root; `Workspace` crosses Directory and File artifacts but stops after including a nested Workspace root. Explicit Load is distinct from the hollow-circle interaction, which requests only the node's `Direct` snapshot.
- The final residency stage always sends its request because earlier stages may have changed server state. Identical in-flight mode and normalized-target requests coalesce as decided in [Unify the loading decision function](11-unify-loading-decision-function.md).
