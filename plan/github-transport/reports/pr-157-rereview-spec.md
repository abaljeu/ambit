# PR 157 Spec re-review

## 1. Client Load does not mark Unparsed

The spec says: "Client Load on Directory — Mark the Directory Node **Unparsed** (re-process)." ([Here→There — step 1 locked](plan/github-transport/here-to-there.md) §4 item 8) The spec also says: "Client Load on File — Mark the File Node **Unparsed**." ([Here→There — step 1 locked](plan/github-transport/here-to-there.md) §4 item 9) [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md) Notes items 8 and 9 record the same writes. Mikado: "Set both the new axes and the old `DocumentState` wherever state changes." ([Here→There — step 1 locked](plan/github-transport/here-to-there.md) §3)

[gitLoadAfterOp](src/Client/UpdateWorkspaceLoad.fs) starts File Parse or Workspace directory-match. [runLoad](src/Server/GithubTransportActor.fs) pulls, then [reconcileWorkspace](src/Server/LazyLoadReconciliationServer.fs), then [parseFocusFile](src/Server/GithubTransportActor.fs). Those Load sites do not set `ParseState.Unparsed` or `DocumentState` Unparsed on the Directory Node or File Node that Load selected. The diff does not change those files. Ticket 20 axes are missing on top of existing Load.

## 2. Discovery does not mark Directory Unparsed for a new or deleted member

The spec says: "A new member or a deleted member marks the Directory Node **Unparsed**. A modified file marks the File Node **Unparsed**." ([Here→There — step 1 locked](plan/github-transport/here-to-there.md) §4 item 3) Map decision 23 Axis-write mechanics locked uses the same rule. ([map.md](plan/github-transport/map.md))

Modified files call `markUnparsed`; [setDocumentState](src/Shared/GraphMutate.fs) dual-writes parse Unparsed. New members: [markAddedDocumentsUnparsed](src/Shared/dotnet/LazyLoadReconciliationApply.fs) marks stubs Unparsed and the parent Directory or Workspace Current (parse Parsed). Deleted members: [planDeletedInfo](src/Shared/dotnet/LazyLoadReconciliation.fs) moves the node to trash and does not mark the Directory Unparsed. [currentDiscoveredAsModified](src/Shared/dotnet/LazyLoadReconciliation.fs) reads `documentState = Current` only. The diff does not add parse Unparsed on those Directory discovery writes.
