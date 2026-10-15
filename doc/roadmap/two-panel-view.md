# Two-panel view

Category: Browser
Status: Vision. This page is not planned work.
See also: [02 — SVG served as image/svg+xml can run script](plan/file-node-open/issues/02-svg-served-as-image-can-run-script.md), [03 — Native display of HTML](plan/file-node-open/issues/03-native-display-of-html.md)

Each side of a two-panel view can be an Ambit view or a web view. This page records that long-term product vision.

## 1. Panels

1. **Two sides** — The view has two panels.
2. **Ambit or web** — Each side can be an Ambit view or a web view.

## 2. Browser web panel

1. **Sandboxed iframe** — In the Browser, the web panel is an iframe with the `sandbox` attribute.
2. **No scripts** — That attribute does not include `allow-scripts`.
3. **No same origin** — That attribute does not include `allow-same-origin`.
4. **HTML and SVG** — HTML and SVG render in that panel with scripts blocked.
5. **PDF** — A sandboxed iframe may not render a PDF in some browsers.

## 3. Desktop web panel

1. **WebView2** — In the Desktop app, the web panel can be a WebView2.
2. **Scripts off** — `CoreWebView2Settings.IsScriptEnabled` is `false`.
3. **PDF viewer** — The built-in Edge PDF viewer still works.

## 4. Open on a File node

1. **Other panel** — Open on a File node would later load the file into the other panel.

## 5. Server backstop

1. **CSP sandbox header** — A DAV GET can send the response header `Content-Security-Policy: sandbox`. That header is a server-side backstop.

## 6. Status of this page

1. **Vision** — This page is Alan's long-term product vision.
2. **No work item** — No Project and no ticket implement this page.
