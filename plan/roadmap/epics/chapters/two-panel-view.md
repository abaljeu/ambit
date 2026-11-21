# Chapter: Two-panel view

**Blocked by:** None.

Each side of the view is an Ambit view or a web view. Open on a File node loads the file into the other panel. This Chapter is vision.

In the Browser, the web panel is an iframe with the `sandbox` attribute. That attribute leaves out `allow-scripts` and `allow-same-origin`. HTML and SVG render with scripts blocked.

In the App, the web panel can be a WebView2. `CoreWebView2Settings.IsScriptEnabled` is `false`. The built-in Edge PDF viewer still works.

A DAV GET can send the response header `Content-Security-Policy: sandbox`.

## Projects

- [[plan/file-node-open/issues/02-svg-served-as-image-can-run-script.md]]
- [[plan/file-node-open/issues/03-native-display-of-html.md]]
