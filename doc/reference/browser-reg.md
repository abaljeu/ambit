This is a browser registration while you are logged into Cursor. The Dashboard API key stays out of it. You want a private Origin App (an internal app), installed on your codebase with `repository:contents:read`.

[Origin App registration steps](bc-771aca25-15f2-5153-83ff-bc3d683a5ffa) checked [Codebase settings](https://cursor.com/docs/origin/codebase-settings), [Origin API](https://cursor.com/docs/api/origin), and [CloneKit CI app setup](https://cursor.com/docs/origin/clonekit-ci).

## 1. Open Origin app settings

Log into Cursor in the browser, then open [https://cursor.com/codebase/settings/apps](https://cursor.com/codebase/settings/apps).

That page is team-wide. You can also open a repo, choose Settings, then Apps, then Manage Apps. You need to be a workspace admin, and Origin has to already be on. If [https://cursor.com/codebase](https://cursor.com/codebase) still asks you to claim a codebase name, finish that first. A team on legacy privacy mode cannot turn Origin on.

## 2. Create a private app

On that Apps page, create an app for your team. The docs do not publish the button label. Create App always creates the app private. Leave third-party apps (Vercel, Depot, Buildkite) alone. Leave webhook URL, events, and website empty. You do not need a webhook to read one file.

## 3. Generate an Ed25519 key pair on Windows

Git for Windows includes OpenSSL 1.1.1 or newer. Use Git Bash or PowerShell. Put the files outside the Ambit repo. If those filenames already exist, pick new names. `genpkey -out` replaces the file.

Git Bash:

```bash
mkdir -p "$HOME/.origin-apps"
cd "$HOME/.origin-apps"
openssl genpkey -algorithm ED25519 -out origin-app-private.pem
openssl pkey -in origin-app-private.pem -pubout -out origin-app-public.pem
```

PowerShell, when Git is in the default location:

```powershell
$dir = Join-Path $env:USERPROFILE ".origin-apps"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$openssl = "C:\Program Files\Git\usr\bin\openssl.exe"
& $openssl genpkey -algorithm ED25519 -out (Join-Path $dir "origin-app-private.pem")
& $openssl pkey -in (Join-Path $dir "origin-app-private.pem") -pubout -out (Join-Path $dir "origin-app-public.pem")
```

If that `openssl.exe` path is missing, in Git Bash run `where openssl` and use the `usr\bin\openssl.exe` under your Git install.

Keep `origin-app-private.pem` on that machine only. Do not commit it and do not paste it into chat. Cursor stores only the public key.

## 4. Paste only the public key

Open `origin-app-public.pem`. It starts with `-----BEGIN PUBLIC KEY-----` and ends with `-----END PUBLIC KEY-----`. Paste that whole PEM into the app’s signing-key field. An app can hold up to 10 active signing keys.

If the create form has a default-scopes field, set it to `repository:contents:read`. That is the scope Get Contents requires. `repository:metadata:read` is added on install by itself, so leave it off the list.

## 5. Copy the App ID

On the app’s page, copy the App ID. It starts with `app_`.

## 6. Install it and select the repo

Stay in Apps settings. Open that app’s install page and install it on the owner that holds the repository. Select that repository. Approve `repository:contents:read`.

For one file in one repo, select that repository. A first install is this browser consent. The API cannot do the first install.

## 7. Skip the partner redirect URL for now

The partner install URL requires a `redirect_uri` that is already saved on the app: an absolute `https://` URI with no `#` fragment. The docs do not describe a localhost callback, and they do not say you can drop `redirect_uri` from that URL.

You do not have an Ambit HTTPS callback yet, so use the install page in Apps settings from step 6. That path does not take a `redirect_uri`. After you approve the install, the installation id is in the browser address bar.

## 8. Copy these three things back

1. App ID, the `app_...` value from the app page.
2. Installation ID, the `i_...` value in the address bar. The page is `https://cursor.com/codebase/settings/apps/installations/{installationId}`.
3. The path of the private key file, for example `C:\Users\Alan\.origin-apps\origin-app-private.pem`. Send the path only.

Do not paste the private key.

## 9. The Dashboard API key is unused here

Do not send it to `https://api.cursor.com`, and do not paste it into the app form.

After this, the Windows program signs an app JWT with the private key, exchanges it for an `oit_` installation token, and calls `GET https://api.cursor.com/v1/origin/repos/{owner}/{repo}/contents`.