import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { callJev } from "./jev.js";

function ok(value) {
  return {
    content: [{ type: "text", text: JSON.stringify(value, null, 2) }],
  };
}

function fail(error) {
  return {
    isError: true,
    content: [
      {
        type: "text",
        text: `jev-mcp error: ${error?.message ?? String(error)}`,
      },
    ],
  };
}

function answer(result, name) {
  const value = result.answers[name];

  if (!value) {
    throw new Error(`Jev response did not contain the "${name}" answer.`);
  }

  return value;
}

function register(server, name, definition, handler) {
  server.registerTool(name, definition, async (input) => {
    try {
      return ok(await handler(input));
    } catch (error) {
      return fail(error);
    }
  });
}

export function createJevServer({ runJev = callJev } = {}) {
  const server = new McpServer({
    name: "gambol-jev-mcp",
    version: "1.0.0",
  });

  register(
    server,
    "jev_check",
    {
      description: "Answer a yes/no question as a calibrated probability.",
      inputSchema: {
        state: z.string(),
        instructions: z.string(),
      },
    },
    async ({ state, instructions }) => {
      const result = await runJev(state, {
        result: { type: "noul", instructions },
      });
      const probability = answer(result, "result").noul;
      return { probability, likely: probability >= 0.5 };
    },
  );

  register(
    server,
    "jev_classify",
    {
      description: "Choose one labelled option for the given state.",
      inputSchema: {
        state: z.string(),
        instructions: z.string(),
        options: z.record(z.string(), z.string()),
      },
    },
    async ({ state, instructions, options }) => {
      const result = await runJev(state, {
        result: { type: "choice", instructions, criteria: options },
      });
      return answer(result, "result");
    },
  );

  register(
    server,
    "jev_score",
    {
      description: "Place the state on a defined ordered scale.",
      inputSchema: {
        state: z.string(),
        instructions: z.string(),
        levels: z.array(z.string()).min(2).max(10),
      },
    },
    async ({ state, instructions, levels }) => {
      const result = await runJev(state, {
        result: { type: "score", instructions, criteria: levels },
      });
      return answer(result, "result");
    },
  );

  register(
    server,
    "jev_gate",
    {
      description: "Assess whether an agent action should run automatically.",
      inputSchema: {
        action: z.string(),
        context: z.string().optional(),
      },
    },
    async ({ action, context }) => {
      const state = [
        `Action an autonomous agent is about to perform:\n${action}`,
        context ? `Context:\n${context}` : undefined,
      ]
        .filter(Boolean)
        .join("\n\n");
      const result = await runJev(state, {
        risk: {
          type: "score",
          instructions: "How risky is it to run this action automatically?",
          criteria: [
            "safe",
            "low",
            "needs review",
            "high — could destroy data or affect production",
          ],
        },
        touches_prod: {
          type: "noul",
          instructions:
            "Does this action touch production or delete or overwrite data?",
        },
        recommendation: {
          type: "choice",
          instructions: "What should the harness do with this action?",
          criteria: {
            allow: "safe to run automatically",
            confirm: "pause and ask a human to confirm",
            block: "block and require a safer approach",
          },
        },
      });
      const risk = answer(result, "risk");
      const touchesProd = answer(result, "touches_prod");
      const recommendation = answer(result, "recommendation");

      return {
        recommendation: recommendation.choice,
        confidence: recommendation.confidence,
        risk_score: risk.score,
        touches_prod: touchesProd.noul >= 0.5,
        touches_prod_probability: touchesProd.noul,
      };
    },
  );

  register(
    server,
    "jev_decide",
    {
      description: "Ask multiple typed Jev questions in one request.",
      inputSchema: {
        state: z.string(),
        questions: z.record(z.string(), z.unknown()),
      },
    },
    async ({ state, questions }) => runJev(state, questions),
  );

  return server;
}
