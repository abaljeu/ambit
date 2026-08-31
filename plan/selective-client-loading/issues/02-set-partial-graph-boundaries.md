# Set partial-graph and document boundaries

Type: grilling
Status: resolved

## Question

What node/header closure must remain present when only selected direct-child lists are loaded, and where must owner/reference traversal stop or cross the current Workspace, Directory, and File document roots so ownership, references, breadcrumbs, and parent indexes remain valid without assuming whole-workspace loading?

## Answer

- Installing a `Loaded` direct-child list also installs a resident header for every listed `Owner` and `Ref` target. A target header does not imply that target's own child list is loaded; the load snapshot ticket decides the exact wire payload.
- Loading recursion follows `Owner` edges only. `ArtifactClosure` resolves each target's nearest Workspace, Directory, or File artifact on its canonical Owner chain, loads that artifact, and stops after including any nested artifact root's header; that nested root's children remain `Unknown`. `Workspace` crosses Directory and File artifact roots but likewise stops after including a nested Workspace root. `Direct` does not recurse. A `Ref` target's header is present, but no mode follows the `Ref` edge.
- Interactive traversal never crosses an `Unknown` child list. The exact no-op, failure, or load-and-resume behavior remains for the navigation ticket.
- The zoom root retains its exact zoom-ingress occurrence path. Every ancestor header and every `Loaded` parent list containing the next ingress edge remain resident, including when the path used a `Ref` occurrence. This preserves the current breadcrumb and zoom-out semantics under monotonic residency.
- Other resident nodes need no ancestry closure. Their known owner may name an absent header; owner traversal then ends as incomplete knowledge rather than inventing ROOT or “no owner.”
- `parentByChild` continues to index only occurrences exposed by `Loaded` lists, while known ownership follows ticket 01. Existing Workspace, Directory, and File document roots define the `ArtifactClosure` boundary; they add no fourth residency mode.
