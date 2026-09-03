# Build or explore a wiki

Stage: charting

A person builds and walks their own wiki in Gambol. Wiki pages are `.md` Files. Authoring is in the App. Explore is a public URL that reads those Files and presents HTML. PKM is scraping and searching, not this Epic.

Current chapter: [[chapters/markdown-codec.md]]

## Chapters

- [[chapters/markdown-codec.md]]
- [[chapters/wiki-public-url.md]]

## Required for done

The Epic is not done until each item is done (or the named part).

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
- [[chapters/wiki-public-url.md]] has no owning Project yet.
