# Gambol Jev MCP

This repository-owned MCP server exposes Jev decisions and supports the response envelope from `https://thejevai.com/v1/systemone`.

## Install

Run `npm install` in this directory.

## Cursor configuration

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

## Verify

Run `npm test` in this directory. The tests do not call the live Jev API or spend credits.
