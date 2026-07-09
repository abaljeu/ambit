namespace Gambol.Shared

type DiskTreeEntry =
    { name: string
      kind: SpecialKind
      mtimeUtc: int64 }

/// One disk entry plus nested directory contents (empty for files).
type DiskTreeBranch =
    { entry: DiskTreeEntry
      children: DiskTreeBranch list }

type WorkspaceTreeSyncSummary =
    { created: int
      reused: int
      renamed: int
      notes: string list }

type WorkspaceTreeSyncPlan =
    { ops: Op list
      summary: WorkspaceTreeSyncSummary
      status: StatusMessage option }
