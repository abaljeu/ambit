# App WebView2 Navigate to Azure `/ambit`

Worker for the retired WORK.md App slice on [[direct-api-vs-proxy.md]]. Did not edit [[WORK.md]]. Did not commit.

## Outcome

Durable issue: [[plan/webview2-azure-origin/issues/01-webview2-navigate-azure-ambit.md]] (rehomed from Roadmap `issues/`). Linked from the recommendation report. Status is `ready-for-agent` after a claim-and-check. No F# change.

The report names no Epic or Chapter. No Epic file edit. Roadmap [[project.md]] stays Stage `steering`. No [[plan/index.md]] regenerate.

## Why no Navigate this sitting

[[src/Desktop/Desktop.fs]] still sets WebView2 `Source` to loopback LocalProxy. [[src/Desktop/LocalProxy.fs]] forwards `/ambit/*` to `resolveTargetUrl`, which defaults to `https://collaborative-systems.org/ambit`. There is no Azure host discovery in the App.

A `Navigate` to Azure `/ambit` would move the document origin. Relative `/_desktop/*` from [[src/Client/Program.fs]] and Workspace/file handlers would miss LocalProxy. Cookie copy via WebView2 `CookieManager` has no call site. [[src/Desktop/AuthStore.fs]] stores username and password; LocalProxy rebuilds `gambol_auth` on the **proxy** hop only.

Shipping Navigate without those seams would drop App host routes or leave the user unauthenticated on Azure. First increment is still on the issue checklist, not a speculative helper module.

## Board mutations (parent applies)

- `remove` Pending [[plan/roadmap/reports/direct-api-vs-proxy.md]] — later App slice: WebView2 `Navigate` to Azure `/ambit` after host discovery; login-after-navigate or copy `gambol_auth` via the WebView2 cookie API (not fetch Azure from the pretty URL)
- `add` Pending [[plan/webview2-azure-origin/issues/01-webview2-navigate-azure-ambit.md]] — after host discovery, WebView2 `Navigate` to Azure `/ambit`; copy `gambol_auth` via CookieManager or login after Navigate; keep `/_desktop` on LocalProxy; do not fetch Azure from the pretty URL

Keep the other Pending line on [[direct-api-vs-proxy.md]] (pretty URL stays on PHP unless a same-site Azure hostname is charted).

## Stage

No `Stage:` change.
