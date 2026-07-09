namespace Gambol.Shared

type DiskTreeEntry =
    { name: string
      kind: SpecialKind
      mtimeUtc: int64 }

type WorkspaceTreeSyncSummary =
    { created: int
      reused: int
      renamed: int
      notes: string list }

type WorkspaceTreeSyncPlan =
    { ops: Op list
      summary: WorkspaceTreeSyncSummary
      status: StatusMessage option }
