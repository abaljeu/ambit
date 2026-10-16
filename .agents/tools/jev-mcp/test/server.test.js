import assert from "node:assert/strict";
import test from "node:test";
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { InMemoryTransport } from "@modelcontextprotocol/sdk/inMemory.js";
import { createJevServer } from "../src/server.js";

test("advertises all tools and runs a check through MCP", async () => {
  let received;
  const runJev = async (state, questions) => {
    received = { state, questions };
    return { answers: { result: { type: "noul", noul: 0.93 } } };
  };
  const server = createJevServer({ runJev });
  const client = new Client({ name: "jev-mcp-test", version: "1.0.0" });
  const [clientTransport, serverTransport] =
    InMemoryTransport.createLinkedPair();

  await server.connect(serverTransport);
  await client.connect(clientTransport);

  try {
    const listed = await client.listTools();
    assert.deepEqual(
      listed.tools.map(({ name }) => name).sort(),
      [
        "jev_check",
        "jev_classify",
        "jev_decide",
        "jev_gate",
        "jev_score",
      ],
    );

    const called = await client.callTool({
      name: "jev_check",
      arguments: {
        state: "Connection test",
        instructions: "Is this a connection test?",
      },
    });

    assert.deepEqual(received, {
      state: "Connection test",
      questions: {
        result: {
          type: "noul",
          instructions: "Is this a connection test?",
        },
      },
    });
    assert.equal(called.isError, undefined);
    assert.deepEqual(JSON.parse(called.content[0].text), {
      probability: 0.93,
      likely: true,
    });
  } finally {
    await client.close();
    await server.close();
  }
});
