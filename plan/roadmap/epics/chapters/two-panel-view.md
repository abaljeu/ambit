# Chapter: Two-panel view

**Part of:** [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]
**Blocked by:** None.

## Context

A person works with documents in the Browser or the App. Open on a File node loads that file in a new tab. The long-term view has two panels. Each side can be an Ambit view or a web view. This Chapter is vision. It is not the current Chapter. It is not planned work.

## Goal

Each side of the view is an Ambit view or a web view. Open on a File node loads the file into the other panel.

In the Browser, the web panel is an iframe with the `sandbox` attribute. That attribute does not include `allow-scripts` and does not include `allow-same-origin`. HTML and SVG render with scripts blocked. A sandboxed iframe may not render a PDF in some browsers.

In the App, the web panel can be a WebView2. `CoreWebView2Settings.IsScriptEnabled` is `false`. The built-in Edge PDF viewer still works.

A DAV GET can send the response header `Content-Security-Policy: sandbox`. That header is a server-side backstop.

## Required for done

1. [ ] **Two panels** — Each side is an Ambit view or a web view.
2. [ ] **Browser web panel** — The panel is a sandboxed iframe. The `sandbox` attribute does not include `allow-scripts` and does not include `allow-same-origin`. HTML and SVG render with scripts blocked.
3. [ ] **App web panel** — The panel can be a WebView2. `CoreWebView2Settings.IsScriptEnabled` is `false`. The built-in Edge PDF viewer still works.
4. [ ] **Open on a File node** — The file loads in the other panel.
5. [ ] **CSP sandbox header** — A DAV GET can send `Content-Security-Policy: sandbox`.

## Notes

This Chapter is vision. It is not planned work. No Project implements it. A sandboxed iframe may not render a PDF in some browsers. The nearer server option for the script limit is the CSP sandbox header on [02 — SVG served as image/svg+xml can run script](plan/file-node-open/issues/02-svg-served-as-image-can-run-script.md) and [03 — Native display of HTML](plan/file-node-open/issues/03-native-display-of-html.md).
