# View

Category: Capability

See Also:

[Browser](browser.md)
[Workspace graph](workspace-graph.md)
[Gambol.Client](gambol-client.md)
[Gambol.Shared](gambol-shared.md)

The Browser view layer is the site tree, the selection span, and line rendering.

## Site

[x] Conceptual type: sitenode.
[x] A sitenode holds a node and an occurrence scope.
[x] Opened means the view includes children.
[x] Children: a nodeview list.
[x] Root: a sitenode. Selection: a nodeview plus a span.
[x] Site model lives in `src/Shared/ViewModel.fs`.

## Lines

[x] `viewroot` yields a nodeview and a trace.
[x] Lines are editable, capture keys, and recurse through sitenodes while they respect fold state.
[x] Line updates are incremental on site-node replace, remove, and insert.
[x] Line rendering lives in `src/Client/View.fs`.
