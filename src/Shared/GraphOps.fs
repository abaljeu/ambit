namespace Gambol.Shared

// ---------------------------------------------------------------------------
// Stable public facade for Graph ops (AutoOpen type extensions).
// F# requires companion modules to share a file with their type; extensions in
// an AutoOpen module preserve Graph.xxx call sites across files.
// ---------------------------------------------------------------------------

[<AutoOpen>]
module GraphOps =

    type Graph with
        static member rootId = GraphBuild.rootId
        static member trashId = GraphBuild.trashId
        static member workspacesId = GraphBuild.workspacesId
        static member systemId = GraphBuild.systemId
        static member isSystemFolderNode nodeId = GraphBuild.isSystemFolderNode nodeId
        static member isSystemDirectoryNode nodeId = GraphBuild.isSystemDirectoryNode nodeId
        static member isCanonicalDataRoot nodeId = GraphBuild.isCanonicalDataRoot nodeId
        static member isCanonicalNode nodeId = GraphBuild.isCanonicalNode nodeId
        static member rootPlaceholder = GraphBuild.rootPlaceholder
        static member fromNodes root nodes = GraphBuild.fromNodes root nodes
        static member nodeCount graph = GraphBuild.nodeCount graph
        static member contains nodeId graph = GraphBuild.contains nodeId graph
        static member newNode text graph = GraphBuild.newNode text graph
        static member create () = GraphBuild.create ()

        static member fileTreeInsertIndex graph parentId =
            GraphQuery.fileTreeInsertIndex graph parentId
        static member isValidOwnedFileDirectoryParent graph parentId =
            GraphQuery.isValidOwnedFileDirectoryParent graph parentId
        static member ownedNameTaken graph parentId excludeId nameLower =
            GraphQuery.ownedNameTaken graph parentId excludeId nameLower
        static member tryFindParentAndIndex targetId graph =
            GraphQuery.tryFindParentAndIndex targetId graph
        static member resolveOwnedFileDirectoryInsert graph focusId =
            GraphQuery.resolveOwnedFileDirectoryInsert graph focusId
        static member owner graph id = GraphQuery.owner graph id
        static member nodeFirstChild graph id = GraphQuery.nodeFirstChild graph id
        static member nodeLastChild graph id = GraphQuery.nodeLastChild graph id
        static member makeNodeRangeForInsertingUnder nodeId graph =
            GraphQuery.makeNodeRangeForInsertingUnder nodeId graph

        static member setText nodeId oldText newText graph =
            GraphMutate.setText nodeId oldText newText graph
        static member setClasses nodeId oldClasses newClasses graph =
            GraphMutate.setClasses nodeId oldClasses newClasses graph
        static member setName nodeId oldName newName graph =
            GraphMutate.setName nodeId oldName newName graph
        static member setDocumentState nodeId oldState newState graph =
            GraphMutate.setDocumentState nodeId oldState newState graph
        static member replace parentId index oldChildren newChildren graph =
            GraphMutate.replace parentId index oldChildren newChildren graph
