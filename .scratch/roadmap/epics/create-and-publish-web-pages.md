# Create and publish web pages

Stage: charting

A person creates web pages and publishes them. Visitors open a public URL and read without the App. Authoring stays in the App. HTML is a File codec. The public URL returns that HTML File body (same bytes the codec writes).

Current chapter: HTML codec

## HTML codec

**What to build:** Read and write HTML Files. A person authors in the App as today.

**Blocked by:** None.

- [ ] [[.scratch/document-formats/map.md]] — HTML File codec (extend that destination; HTML was excluded)

## In-app styling

**What to build:** HTML look in the App. Not Published-page CSS. Not the App stylesheet [[doc/reference/style.md]]. Parallel to [[work-with-text-files-from-anywhere.md]] Markdown styling.

**Blocked by:** HTML codec.

## Public URL

**What to build:** Visitors open a public URL and receive the HTML File body. Same bytes the codec writes. No App.

**Blocked by:** HTML codec.

## Published-page CSS

**What to build:** Stylesheet for published pages. Not the App stylesheet [[doc/reference/style.md]].

**Blocked by:** HTML codec.

## Required for done

Not a Chapter. The Epic is not done until each item is done (or the named part).

Live:

- [ ] [[.scratch/end-user-wiki/map.md]] — portion for this Epic (not yet filed)
- [ ] [[.scratch/marketing-wiki/map.md]] — portion for this Epic (not yet filed)

## Notes

- [[.scratch/transport-layer/project.md]] cross-cutting pattern — HTML codec and publish are outbound transport: generate HTML and send attachments and CSS through transport-layer (Graph / HTML File content → visitor-facing site), not a separate publisher stack. Distinct from wiki Public URL (`.md` → HTML) in [[build-or-explore-a-wiki.md]]. In-app styling and Published-page CSS are the same outbound transport with CSS as payload. See [[.scratch/transport-layer/overview.md]], [[.scratch/transport-layer/map.md]].
- Scaling is [[organize-huge-outlines.md]]; first use does not wait on it.
- Wiki page and published page are the same family; presentation and needs vary greatly at the detail level. Do not collapse with [[build-or-explore-a-wiki.md]].
- Not documents-from-anywhere: that audience is the person working; this is visitors without the App. Not export to another host.
- English **web page** vs File Node: do not say page for a File Node. [[CONTEXT.md]].
- Chapters stay open-ended: add one when a specific need is named. Custom domain is not named yet.
- Wiki write-up is not a Chapter. Portions are Required for done.
- Public URL, Published-page CSS, and In-app styling have no owning Project yet.
