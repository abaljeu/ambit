namespace Gambol.Shared

/// String encoding for `NodeKind` in SQL projection (matches Serialization vocabulary).

[<RequireQualifiedAccess>]
module NodeKindPersistence =

    let toPersistString (kind: NodeKind) : string =
        match kind with
        | Normal -> "normal"
        | Special Workspaces -> "workspaces"
        | Special Workspace -> "workspace"
        | Special Directory -> "directory"
        | Special File -> "file"

    let fromPersistString (s: string) : Result<NodeKind, string> =
        match s with
        | "normal" -> Ok Normal
        | "workspaces" -> Ok (Special Workspaces)
        | "workspace" -> Ok (Special Workspace)
        | "directory" -> Ok (Special Directory)
        | "file" -> Ok (Special File)
        | "trash" -> Ok (Special Directory)
        | other -> Error $"Unknown node kind: {other}"

    /// Pre-migration rows default to `normal`; canonical ids still map to system kinds.
    let legacyKindForCanonical (nid: NodeId) (kind: NodeKind) : NodeKind =
        if kind <> Normal then
            kind
        elif nid = Graph.rootId then
            Special Workspace
        elif nid = Graph.workspacesId then
            Special Workspaces
        elif nid = Graph.trashId then
            Special Directory
        else
            Normal
