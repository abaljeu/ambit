namespace Gambol.Shared

/// Document-root stub edits on an Unparsed Directory or Workspace shell.
[<RequireQualifiedAccess>]
module internal UnparsedShellEdit =

    let private isOwnedDocumentRoot
        (graph: Graph)
        (parentId: NodeId)
        (child: ChildNode)
        =
        Node.childOwnership graph parentId child = Ownership.Owner
        && DocumentPartition.isDocumentRootNode graph child.id

    let private allOwnedAreRoots
        (graph: Graph)
        (parentId: NodeId)
        (children: ChildNode list)
        =
        children
        |> List.forall (fun child ->
            Node.childOwnership graph parentId child <> Ownership.Owner
            || DocumentPartition.isDocumentRootNode graph child.id)

    let private isSuffixOfRoots
        (graph: Graph)
        (parentId: NodeId)
        (prefix: ChildNode list)
        (full: ChildNode list)
        =
        let n = List.length prefix
        List.length full >= n
        && List.take n full = prefix
        && (List.skip n full
            |> List.forall (isOwnedDocumentRoot graph parentId))

    /// Same list, roots-only edit, or a suffix of document-root stubs.
    let isStubEdit
        (graph: Graph)
        (parentId: NodeId)
        (oldChildren: ChildNode list)
        (newChildren: ChildNode list)
        =
        (allOwnedAreRoots graph parentId oldChildren
         && allOwnedAreRoots graph parentId newChildren)
        || isSuffixOfRoots graph parentId oldChildren newChildren
        || isSuffixOfRoots graph parentId newChildren oldChildren
