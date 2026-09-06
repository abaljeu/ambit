# Pack encoding facts

Facts for grilling [[../issues/01-how-the-pack-is-encoded.md]]. Map: [[../map.md]]. Glossary: [[CONTEXT.md]] Included context, SiteMap, Zoom, Fold, Graph, Node, Agent, Actor, Browser, Server, Shared.

## 1. Where SiteMap, Fold, and Zoom live

SiteMap is a Shared type ([[src/Shared/ViewModel.fs]], [[src/Shared/ViewModelSiteMap.fs]]). The live instance is on Browser VM (siteMap plus zoomRoot). Server source has no SiteMap.

Zoom as the view root is Browser VM zoomRoot. Server [[src/Server/Api.fs]] getState reads query zoom only to widen Load residency ([[src/Shared/ResidentProjection.fs]]). Browser session key gambol-session-v1 stores z (UI Zoom) and b (boot widen) in sessionStorage then localStorage ([[src/Client/SessionState.fs]]). That store is not a Server Graph field.

Fold is SiteEntry.expanded on SiteMap, keyed by SiteId (per occurrence, not per NodeId). Toggle runs in Browser ([[src/Client/UpdateOps.fs]]). Capture and restore use session JSON field f. Graph Node has no fold field ([[src/Shared/Model.fs]]).

Server cannot reconstruct Included context from Graph alone. Fold is absent from Graph. buildSiteMapFrom expands only the Zoom root; child entries start collapsed. Server Graph may hold Loaded Children that the Browser left Unloaded. Chart lock: Unloaded Children stay out of the pack ([[plan/roadmap/issues/04-chart-agent-chat-managed-context.md]] Q3). Reconstructing Included context needs Fold and current Zoom from the Browser, or a payload the Browser already packed.

## 2. Existing Graph / outline encodings

No Agent connector and no pack encoder exist in src/.

Fold-honoring text: [[src/Shared/Paste.fs]] serializeSubtree writes tab-indented Node text and walks unfolded SiteMap children. Browser copy uses it. collectSubtree builds ClipboardContent with Children trimmed to unfolded rows.

Fold-ignoring Graph walks: [[src/Shared/ExportText.fs]] serializeOwnedChildren writes all Owned descendants of focus as tab-indented text. Document Persist ([[src/Shared/documents/AmbDocument.fs]], [[src/Shared/documents/MdDocument.fs]], [[src/Shared/documents/PlainTextDocument.fs]]) writes File Node bodies from Graph, not from SiteMap.

Visible row lists: getVisibleRowIds and getVisibleRowInstanceIds walk SiteMap preorder and omit children of unexpanded entries (Zoom root excluded from the row-id list).

JSON Graph: [[src/Shared/Serialization.fs]] encodeGraph is the HTTP state / Load Graph codec. It does not encode SiteMap or Fold.

Actor launch Graph: Core [[src/Server/Core/CoreActorPool.fs]] ActorFn receives GraphSpan.extract of LaunchRequest span ([[src/Shared/GraphSpan.fs]]): parent Node plus Owned descendants of a child-index range. That extract ignores SiteMap and Fold. [[../issues/02-which-llm-and-credentials.md]] names that launch subgraph as first Agent context; Included context waits on the postponed UI action; pack shape stays this issue.

[[plan/transport-layer/overview.md]] and [[plan/transport-layer/map.md]] do not define an LLM pack. Persist is Graph slice to outside text. Agent Actor is inbound: reply as Owned children. [[plan/event-sourced-ops/details/actors-and-jobs.md]] subgraph snapshot at plan time is Change-merge prior, not pack shape.

## 3. What the person includes when they Ask

Chapter [[plan/roadmap/epics/chapters/ask-from-what-i-see.md]]: Run ? with a message and included context. Reply is Owned children of the focus Node. The call is a long-running Actor.

Epic chart Q2–Q3: same Run command; line form ? plus a message. Included context is SiteMap rows under Zoom, honoring Fold. Visible is speech, not glossary. Unloaded Children stay out of the pack. Epic: [[plan/roadmap/epics/agent-chat-managed-context.md]].

Issue 02 Answer: first Agent context is the launch subgraph. Included context (SiteMap under Zoom, honoring Fold) waits on the postponed UI action. Named may-change: Included context instead of subgraph.

## 4. Fold: view fact or Graph fact

Fold is a Browser view fact on SiteMap, not a Graph Node field. It is not posted to Server. One Node can be expanded at one occurrence and collapsed at another.

## 5. Size / limits

No LLM token limit and no pack-size limit exist in this repo. [[src/Shared/DocumentParseLimits.fs]] refuses Parse input over 50_000 UTF-16 code units (100_000 UTF-16 bytes). Kestrel MaxRequestBodySize is 100 MB for git receive-pack ([[src/Server/Server.fs]]). Filename max length is 255 ([[src/Shared/Filename.fs]]). Node.text has no typed max.
