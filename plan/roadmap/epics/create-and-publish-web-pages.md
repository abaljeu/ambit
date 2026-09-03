# Create and publish web pages

Stage: charting

A person creates web pages and publishes them. Visitors open a public URL and read without the App. Authoring stays in the App. HTML is a File codec. The public URL returns that HTML File body (same bytes the codec writes).

Current chapter: [[chapters/html-codec.md]]

## Chapters

- [[chapters/html-codec.md]]
- [[chapters/in-app-styling.md]]
- [[chapters/web-pages-public-url.md]]
- [[chapters/published-page-css.md]]

## Required for done

The Epic is not done until each item is done (or the named part).

Live:

- [ ] [[plan/end-user-wiki/map.md]] — portion for this Epic (not yet filed)
- [ ] [[plan/marketing-wiki/map.md]] — portion for this Epic (not yet filed)

## Notes

- [[plan/transport-layer/project.md]] cross-cutting pattern — HTML codec and publish are outbound transport: generate HTML and send attachments and CSS through transport-layer (Graph / HTML File content → visitor-facing site), not a separate publisher stack. Distinct from wiki Public URL (`.md` → HTML) in [[build-or-explore-a-wiki.md]]. In-app styling and Published-page CSS are the same outbound transport with CSS as payload. See [[plan/transport-layer/overview.md]], [[plan/transport-layer/map.md]].
- Scaling is [[organize-huge-outlines.md]]; first use does not wait on it.
- Wiki page and published page are the same family; presentation and needs vary greatly at the detail level. Do not collapse with [[build-or-explore-a-wiki.md]].
- Not documents-from-anywhere: that audience is the person working; this is visitors without the App. Not export to another host.
- English **web page** vs File Node: do not say page for a File Node. [[CONTEXT.md]].
- Chapters stay open-ended: add one when a specific need is named. Custom domain is not named yet.
- Wiki write-up is not a Chapter. Portions are Required for done.
- [[chapters/web-pages-public-url.md]], [[chapters/published-page-css.md]], and [[chapters/in-app-styling.md]] have no owning Project yet.
