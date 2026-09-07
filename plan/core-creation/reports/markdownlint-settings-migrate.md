# markdownlint settings migrate

Date: 2026-09-06

vscode-markdownlint v0.62.1 source: uploaded dump of [source](https://github.com/DavidAnson/vscode-markdownlint/tree/v0.62.1)

## What was deprecated

The extension still reads `markdownlint.config` in user and workspace settings, but that property is deprecated. The documented replacement is a markdownlint configuration file. For user scope, put `.markdownlint.json` (or `.markdownlint.jsonc`) in the home directory and set `markdownlint.configFile` to that path (example: `${userHome}/.markdownlint.json`).

Flat rule IDs under `markdownlint.config` were the deprecated design. Comments in settings.json were valid JSONC and were not the deprecation.

## What changed

Removed the `markdownlint.config` object from `C:\Users\Windows 8\AppData\Roaming\Cursor\User\settings.json`.

Set `markdownlint.configFile` to `${userHome}/.markdownlint.jsonc`.

Created `C:\Users\Windows 8\.markdownlint.jsonc`. There was no existing home-directory markdownlint file. Used `.jsonc` so the tab comments stay next to the rules.

No gambol product files were changed. This workspace has no `.markdownlint*` file and no `markdownlint.config` in JSON settings.

## New config shape

User settings:

```jsonc
// Rule toggles live in ${userHome}/.markdownlint.jsonc (markdownlint.config is deprecated)
"markdownlint.configFile": "${userHome}/.markdownlint.jsonc"
```

Home file `.markdownlint.jsonc`:

```jsonc
{
    "MD022": false,
    "MD031": false,
    "MD032": false,
    "MD004": false,
    "MD010": false, // allow tabs
    "MD007": false // a tab is 4 wide.  it's fine.
}
```

Preserved intent: MD032 off, MD004 off, MD010 off (allow tabs), MD007 off (tab width 4). Also moved MD022 and MD031, which were already in the same deprecated object. Leaving them in settings would keep the deprecation.

## Leftover warning risk

If Cursor still shows a deprecation on `markdownlint.config`, reload the window. The extension docs say config-file changes take effect immediately.

A workspace or folder `settings.json` that still has `markdownlint.config` will keep the warning. This gambol workspace does not.

A project `.markdownlint-cli2.*` or `.markdownlint.*` file still wins over the user `configFile`. That is documented precedence. This user file applies when no project config is present.

If a later tool reads the home file as strict JSON, comments will fail. markdownlint-cli2 accepts `.jsonc`.
