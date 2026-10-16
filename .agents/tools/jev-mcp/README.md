# Gambol Jev MCP

This repository-owned MCP server exposes Jev decisions and supports the response envelope from `https://thejevai.com/v1/systemone`.

## Install

Run `npm install` in this directory.

## Cursor Desktop configuration

Add this server to the Cursor MCP configuration. Replace the API key placeholder.

```json
{
  "jev": {
    "command": "node",
    "args": [
      "D:/dev/amble/gambol/.agents/tools/jev-mcp/src/index.js"
    ],
    "env": {
      "JEV_BASE_URL": "https://thejevai.com/v1/systemone",
      "JEV_MODEL": "typesafe/jev-1.14",
      "TYPESAFE_API_KEY": "your-api-key"
    }
  }
}
```

Restart Cursor after you change the MCP configuration.

## Cursor Cloud configuration

The Cloud environment install script installs this package during an environment build. Use the Cloud workspace path in the MCP configuration:

```json
{
  "jev": {
    "command": "node",
    "args": [
      "/workspace/.agents/tools/jev-mcp/src/index.js"
    ],
    "env": {
      "JEV_BASE_URL": "https://thejevai.com/v1/systemone",
      "JEV_MODEL": "typesafe/jev-1.14",
      "TYPESAFE_API_KEY": "your-api-key"
    }
  }
}
```

Keep the API key in Cursor's MCP settings. Do not add it to the repository environment configuration. The local server becomes available to new Cloud Agents after the repository change is merged and a successful environment build is active.

## Verify

Run `npm test` in this directory. The tests do not call the live Jev API or spend credits.
