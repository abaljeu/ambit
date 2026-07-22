# cPanel transparent proxy (custom domain → Azure)

Category: Deployment
See also: [[doc/reference/deploy-azure.md]], [[.htaccess]], [[proxy.php]], [[src/Server/Server.fs]]

Production serves Gambol at `https://collaborative-systems.org/ambit`. Azure App Service runs the .NET app and PostgreSQL connection. The cPanel (HostGator) host at that domain only forwards `/ambit` traffic; it does not run .NET, a Gambol process, or application data.

## Request flow

```mermaid
%%{init: {'themeVariables': {'fontSize': '20px'}}}%%
sequenceDiagram
    participant Browser
    participant cPanel as cPanel Apache
    participant PHP as proxy.php
    participant Azure as Azure Web App

    Browser->>cPanel: GET /ambit/login
    cPanel->>PHP: rewrite path=/login
    PHP->>Azure: GET /ambit/login (curl)
    Azure-->>PHP: response + headers
    PHP-->>Browser: same body; Location rewritten to custom domain
```

1. Apache matches `^ambit(/.*)?$` in [[.htaccess]] and routes to [[proxy.php]] with the subpath in `path`.
2. `proxy.php` builds the Azure backend URL (`$BACKEND` + `/ambit` + subpath), forwards the request with curl, and returns the response.
3. `Location` headers from Azure are rewritten so redirects stay on `collaborative-systems.org` instead of exposing the `*.azurewebsites.net` host.

## cPanel host files

Upload repo-root [[.htaccess]] and [[proxy.php]] to the cPanel document root after changes.

### `.htaccess`

- Collapses doubled paths: `/ambit/ambit/login` → `/ambit/login` (301).
- Rewrites `/ambit` and `/ambit/*` to `proxy.php?path=…` with query string preserved (`QSA`).

Apache `mod_proxy` is not used — shared hosting often disables it. PHP curl in `proxy.php` is the workaround.

### `proxy.php`

- **`$BACKEND`** — Azure Web App origin (no trailing slash).
- **Path mapping** — `/ambit` → backend `/ambit`; `/ambit/login` → `path=/login` → backend `/ambit/login`.
- **Forwarded request headers** — `Accept`, `Accept-Language`, `Accept-Encoding`, `Content-Type`, `Content-Length`, `Cookie`, `Authorization`, `X-Requested-With`, `User-Agent`; request body for POST/PUT/PATCH. Default curl timeout 60s (raise per route if large uploads need it).
- **Response headers** — passes through except `transfer-encoding` and `content-length` (curl reassembles the body). Rewrites absolute and root-relative `Location` values to the browser's origin (`https://collaborative-systems.org`).

Update `$BACKEND` in `proxy.php` if the Azure Web App hostname changes.

## Azure cooperation (JS/CSS cross-origin)

HTML and API routes go through the PHP proxy on the custom domain. Fable emits **relative** `import` paths in `Program.js`; the browser resolves them against the script URL. If `Program.js` were served through the proxy at `collaborative-systems.org`, every chunk would also hit PHP.

Production avoids that by loading the module graph directly from Azure:

- **Shell template** — [[src/Server/wwwroot/gambol.template.html]] (not `gambol.html`; a static `gambol.html` under `wwwroot` would bypass URL rewrites).
- **`PublicAssetBase`** — optional origin override in `appsettings` / Azure app settings. On Azure App Service in Production, when unset, defaults to `https://` + `WEBSITE_HOSTNAME`. [[src/Server/Server.fs]] rewrites `style.css`, `user.css`, and `Program.js` in the served HTML to absolute Azure URLs.
- **`JsModuleCorsOrigins`** — comma-separated allowed `Origin` values for `/ambit/*.js`. When empty but `PublicAssetBase` is in effect, defaults to `*` so the proxied HTML origin can load ES modules from Azure.

API routes (`/ambit/state`, `/ambit/changes`, login, etc.) still traverse the PHP proxy; only static JS/CSS (and their cache-bust query params) target Azure directly.

## Redirect rewriting (server-side)

[[src/Shared/RedirectRewrite.fs]] rewrites `Location` headers when the desktop local proxy forwards to the cloud app. The PHP proxy performs an equivalent rewrite for the cPanel path. Tests: [[tests/Shared.Tests/RedirectRewriteTests.fs]].

## Troubleshooting

- **Redirects land on `*.azurewebsites.net`** — check `Location` rewriting in `proxy.php`; confirm `$BACKEND` matches the live Web App.
- **Blank app / module load errors** — confirm `PublicAssetBase` resolves to the Azure origin and `/ambit/*.js` responses include `Access-Control-Allow-Origin` (default `*` when asset base is set).
- **Stale client after deploy** — HTML is cache-busted via server build stamps; hard-reload if a CDN or browser cache interferes.
- **502 from proxy** — curl error from cPanel to Azure; check Web App is running and outbound HTTPS from HostGator is allowed.
