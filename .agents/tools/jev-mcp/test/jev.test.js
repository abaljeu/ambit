import assert from "node:assert/strict";
import test from "node:test";
import { callJev, normalizeJevResponse } from "../src/jev.js";

const answer = { test: { type: "noul", noul: 0.93 } };

test("normalizes the thejevai.com response envelope", () => {
  const normalized = normalizeJevResponse({
    code: 0,
    message: "ok",
    data: { result: { answers: answer, usage: { input_tokens: 1 } } },
  });

  assert.deepEqual(normalized.answers, answer);
});

test("accepts an unwrapped Jev response", () => {
  assert.deepEqual(normalizeJevResponse({ answers: answer }).answers, answer);
});

test("rejects a response without answers", () => {
  assert.throws(
    () => normalizeJevResponse({ code: 0, message: "ok" }),
    /did not contain answers: ok/,
  );
});

test("posts the configured model and returns normalized answers", async () => {
  let request;
  const result = await callJev(
    "Connection test",
    {
      test: { type: "noul", instructions: "Is this a connection test?" },
    },
    {
      env: {
        TYPESAFE_API_KEY: "secret",
        JEV_BASE_URL: "https://example.test/v1/systemone",
        JEV_MODEL: "typesafe/jev-test",
      },
      fetchImpl: async (url, options) => {
        request = { url, options };
        return new Response(
          JSON.stringify({
            code: 0,
            message: "ok",
            data: { result: { answers: answer } },
          }),
          { status: 200 },
        );
      },
    },
  );

  assert.equal(request.url, "https://example.test/v1/systemone");
  assert.equal(request.options.headers.Authorization, "Bearer secret");
  assert.deepEqual(JSON.parse(request.options.body), {
    model: "typesafe/jev-test",
    state: "Connection test",
    questions: {
      test: { type: "noul", instructions: "Is this a connection test?" },
    },
  });
  assert.deepEqual(result.answers, answer);
});
