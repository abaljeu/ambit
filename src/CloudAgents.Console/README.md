# CloudAgents Console

Standalone command-line interface for running Cursor Cloud Agents.

## Usage

```bash
dotnet run --project src/CloudAgents.Console -- "<prompt>" [options]
```

Prompt is required on the CLI. Other create fields may come from `appsettings.<level>.json`.

## Options

- `--repo <url>` — Repository URL
- `--ref <ref>` — Starting branch or commit
- `--name <name>` — Display name for the agent
- `--model <id>` — Cursor model id from the printed catalog
- `--api-key <key>` — Cursor API key

## Config files

Level is `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT` (default `Development`).

Load order: tracked `appsettings.json`, then `appsettings.<level>.json`. Search the current directory, then the exe directory.

CLI wins over file. File wins over `CURSOR_API_KEY` for the key.

Tracked `appsettings.json` holds empty placeholders. Put secrets in gitignored `appsettings.Development.json` or `appsettings.Production.json`.

```json
{
  "ApiKey": "",
  "Model": "",
  "Repo": "",
  "Ref": "",
  "Name": ""
}
```

## Environment Variables

- `CURSOR_API_KEY` — fills `ApiKey` when CLI and file omit it
- `ASPNETCORE_ENVIRONMENT` / `DOTNET_ENVIRONMENT` — config level

Get your API key from: https://cursor.com/settings

## Examples

### Prompt only (rest from appsettings)

```bash
dotnet run --project src/CloudAgents.Console -- "Explain how async/await works in F#"
```

After the key is known, Console prints the Cursor model catalog (`id`, displayName, variants), then starts and waits.

### Override model on the CLI

```bash
dotnet run --project src/CloudAgents.Console -- "What are F# options?" --model cursor-grok-4.7-low-fast
```

### With repository

```bash
dotnet run --project src/CloudAgents.Console -- "Add a README with setup instructions" \
  --repo https://github.com/your-org/your-repo \
  --ref main \
  --name "Add README"
```

## Output

The console prints:

- Cursor model catalog
- Agent ID and Run ID
- Final result text
- Git branch and PR information (if a repository was provided and the agent made changes)
