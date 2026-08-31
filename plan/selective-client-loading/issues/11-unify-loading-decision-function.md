# Unify the loading decision function

Type: grilling
Status: resolved
Blocked by: 05, 06, 07, 08, 09, 10

## Question

What single loading decision function, implementing one current policy, should map client graph, residency, and interaction state plus startup, explicit Load, navigation, search selection, move and edit, session restoration, and synchronization triggers to required load requests and continuation outcomes without introducing configuration or future-policy abstraction?

## Answer

- Reject the premise of a client-wide loading-policy planner. Each client trigger directly chooses one fixed load mode and its target node IDs; command-specific continuations remain with the invoking command. Generic response handling installs valid snapshots and owns protocol failure, synchronization, and submission-rejection behavior.
- One pure server load-state function expands `Direct | ArtifactClosure | Workspace` against the authoritative fully resident graph. These are closed domain cases, not configurable algorithm names or a future-policy interface.
- `Direct` returns each target's complete immediate child list. It does not recurse.
- `ArtifactClosure` follows each target's canonical Owner chain to its nearest Workspace, Directory, or File artifact, then follows Owner edges within that artifact. It includes each nested artifact root's header but leaves that nested root's child list `Unknown`.
- `Workspace` follows Owner edges throughout each targeted Workspace, crossing Directory and File artifact roots, but includes a nested Workspace root without loading its child list. No mode follows Ref edges; every exposed Ref target still receives its required header.
- Initial startup and session restoration send the established no-revision `Workspace` batch for ROOT and the remembered zoom Workspace. The response establishes the client revision and first render still waits for the complete bootstrap.
- Explicit Load normalizes its full selection to canonical artifacts. A Workspace target uses `Workspace`; every other artifact uses `ArtifactClosure`. Cross-Workspace or mixed-mode selections reject before any pipeline stage. The final residency stage always requests its scope after upload and parse/reconciliation, even if the client believes it resident.
- Hollow-circle loading and load-backed framing navigation use `Direct` only when their target child list is `Unknown`. A successful hollow-circle load unfolds a non-leaf result; a loaded leaf remains folded-inapplicable. Double-click Zoom frames the loaded target without a separate fold step.
- A committed Find result frames immediately when its child list is `Loaded`; when `Unknown`, it requests `ArtifactClosure` for the found node and then resumes through the preserved discovery occurrence.
- Move Selected searches resident data and requests `Direct` only for an `Unknown` destination list. Success applies the move optimistically; load failure, a missing destination, or stale source intent cancels it. Hidden ownership-placement and artifact-name validation stays on the fully resident server, and generic post-response handling owns any submission rejection.
- Other edits, clipboard operations, undo, and redo do not implicitly load. Their previously decided resident-only or reject behavior remains unchanged.
- Identical in-flight `(mode, normalized target set)` requests coalesce and carry all waiting command continuations. Overlapping but non-identical requests remain separate. A still-current valid response installs even when one initiating continuation has become stale.
- Polling does not choose a load mode. Every load response uses the established catch-up transaction through the server's latest atomically read revision, and all existing atomic installation, mutation-epoch, stale-response, and failure rules remain authoritative. Applying a successful response does not rerun loading policy.
