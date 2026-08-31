# Build or explore a wiki

Stage: charting

A person builds and walks their own wiki in Gambol. Wiki pages are `.md` Files. Authoring is in the App. Explore is a public URL that reads those Files and presents HTML. PKM is scraping and searching, not this Epic.

Current chapter: Markdown codec

## Markdown codec

**What to build:** Read and write `.md` Files as wiki pages. A person authors in the App as today.

**Blocked by:** None.

- [ ] [[plan/document-formats/map.md]] — `.md` File codec

## Public URL

**What to build:** Visitors open a public URL. The Server reads `.md` Files and presents HTML. Not the HTML File body of [[create-and-publish-web-pages.md]]. In-App walk is not a Chapter.

**Blocked by:** Markdown codec.

## Required for done

Not a Chapter. The Epic is not done until each item is done (or the named part).

Live:

- [ ] [[plan/expression-language/project.md]] — remainder beyond Find / `?`
- [ ] [[plan/end-user-wiki/map.md]] — portion for this Epic (not yet filed)
- [ ] [[plan/marketing-wiki/map.md]] — portion for this Epic (not yet filed)

## Notes

- [[plan/transport-layer/project.md]] cross-cutting pattern — the `.md` codec is a Parse/Persist round-trip (authoring) leg; Public URL (wiki publish) is an outbound transport instance (Graph / `.md` File content → HTML for visitors), not the HTML File body or in-app HTML of [[create-and-publish-web-pages.md]]. Publish still uses Parse/Persist as the text-processing unit. See [[plan/transport-layer/overview.md]], [[plan/transport-layer/map.md]].
- Scaling is [[organize-huge-outlines.md]]; first use does not wait on it.
- In-product wiki. The three documentation wikis stay Projects: [[plan/end-user-wiki/map.md]], [[plan/architecture/map.md]], [[plan/marketing-wiki/map.md]].
- Two document classes: `.md` (this Epic) vs HTML Files ([[create-and-publish-web-pages.md]]).
- [[work-with-text-files-from-anywhere.md]] Markdown styling is look in the App, not the `.md` codec.
- English **wiki page** vs File Node: do not say page for a File Node. [[CONTEXT.md]].
- Chapters stay open-ended: add one when a specific need is named.
- Wiki write-up is not a Chapter. Portions are Required for done.
- Public URL has no owning Project yet.
