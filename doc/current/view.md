# View

Category: Architecture
See Also: [[doc/current/browser.md]], , [[doc/current/workspace-graph.md]]

The Browser view layer is not the server graph. It is the site tree, the selection span, and line rendering.

## Is

Site/composite model:

- type sitenode (conceptual)
- node + occurrence scope
- opened (include children)
- children : nodeview list
- root : sitenode; selection : nodeview + span

Lines:

- viewroot → nodeview + trace
- lines: editable, key capture, recursive sitenodes respecting fold state
- incremental line updates on site node replace/remove/insert

## Should Become

No later view shape is recorded here.

## Where

- [[src/Shared/ViewModel.fs]]
- [[src/Client/View.fs]]
