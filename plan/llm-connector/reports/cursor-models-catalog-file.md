# Report: cursor-models catalog file

Date: 2026-09-22
Workplace: `dev` (`scripts/gitstatus.sh`); tree already dirty with sibling Console edits and local Development appsettings. No commit.

## Ticket

Filed [22 — Checked-in cursor-models catalog and Console model/params selection](../issues/22-cursor-models-catalog-file.md). Project Stage stays `done`. Notes and Implementation tickets updated on [project.md](../project.md).

**Status:** `coded` for this workstream's deliverables (catalog file, fsproj Content, Development `Models` removal). Console runtime selection remains open for the sibling workstream.

## Deliverables

### 1. [src/CloudAgents/cursor-models.json](../../../src/CloudAgents/cursor-models.json)

Seeded from live `GET https://api.cursor.com/v1/models` using Basic auth (`ApiKey:` from `AiKeys[0]` in [src/appsettings.Development.json](../../../src/appsettings.Development.json)). The key was not printed and is not in the catalog file.

- Envelope: `{ "models": [ ... ] }` (API used `items`; catalog uses `models`).
- Count: **40** models.
- Each model: `id`, `displayName`, `aliases` (array, possibly empty), `parameters`, `variants`.
- Each parameter: `id`, `displayName`, `values[]` of `{ value, displayName }` (from API when present; 33 models have parameters).
- Each variant: `id`, `displayName`, plus `params` (API param bindings for create) and optional `isDefault`.

**Variant id synthesis:** Live variants have no `id` (only `params`, `displayName`, optional `isDefault`). Stable ids are:
- empty `params` → `default` (when `isDefault` or as fallback);
- otherwise join `paramId=value` with commas, e.g. `context=256k,reasoning_effort=low,fast=false`.

Zero-width characters in some API display names were stripped.

### 2. [Gambol.CloudAgents.fsproj](../../../src/CloudAgents/Gambol.CloudAgents.fsproj)

Added:

```xml
<Content Include="cursor-models.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

### 3. Development appsettings

Removed the `Models` array from [src/appsettings.Development.json](../../../src/appsettings.Development.json). Left `AiKeys`, `AiRepos`, and other local secrets untouched. That file stays untracked / uncommitted.

## Not touched (sibling-owned)

- [CursorHttp.fs](../../../src/CloudAgents/Internal/CursorHttp.fs)
- [PublicTypes.fs](../../../src/CloudAgents/PublicTypes.fs)
- [Program.fs](../../../src/CloudAgents.Console/Program.fs)
- [Config.fs](../../../src/CloudAgents.Console/Config.fs)

## Next

Sibling: load the checked-in catalog in Console, print it, and resolve model id + params for create without relying on a Development `Models` array or a mandatory live list call for the start catalog.
