# CloudAgents Console

Standalone command-line interface for running Cursor Cloud Agents.

## Usage

```bash
CloudAgents.Console "<prompt>" [options]
```

## Options

- `--repo <url>` - (Optional) Repository URL to work on
- `--ref <ref>` - Starting branch or commit (default: repository default)
- `--name <name>` - Display name for the agent

## Environment Variables

- `CURSOR_API_KEY` (required) - Your Cursor Dashboard API key

Get your API key from: https://cursor.com/settings

## Examples

### No-repo agent (primary use case)

```bash
export CURSOR_API_KEY=your_key_here
CloudAgents.Console "Explain how async/await works in F#"
```

### Another no-repo example

```bash
export CURSOR_API_KEY=your_key_here
CloudAgents.Console "What are the trade-offs between actor model and CSP?"
```

### With repository (optional)

```bash
export CURSOR_API_KEY=your_key_here
CloudAgents.Console "Add a README with setup instructions" \
  --repo https://github.com/your-org/your-repo \
  --ref main \
  --name "Add README"
```

## Output

The console prints:
- Agent ID and Run ID
- Progress updates
- Final result text
- Git branch and PR information (if repository was provided and agent made changes)
