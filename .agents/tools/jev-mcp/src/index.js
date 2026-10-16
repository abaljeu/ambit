#!/usr/bin/env node

import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { createJevServer } from "./server.js";

const server = createJevServer();
const transport = new StdioServerTransport();

await server.connect(transport);

console.error("gambol-jev-mcp running");
