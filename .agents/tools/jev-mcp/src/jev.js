const DEFAULT_BASE_URL = "https://thejevai.com/v1/systemone";
const DEFAULT_MODEL = "typesafe/jev-1.14";

export function normalizeJevResponse(body) {
  const result = body?.data?.result ?? body?.result ?? body;

  if (!result?.answers || typeof result.answers !== "object") {
    const message = body?.message ? `: ${body.message}` : "";
    throw new Error(`Jev response did not contain answers${message}`);
  }

  return result;
}

export async function callJev(
  state,
  questions,
  {
    env = process.env,
    fetchImpl = globalThis.fetch,
    timeoutMs = 20_000,
  } = {},
) {
  const apiKey =
    env.TYPESAFE_API_KEY || env.JEV_API_KEY || env.JEV_KEY || "";
  const baseUrl = env.JEV_BASE_URL || DEFAULT_BASE_URL;
  const model = env.JEV_MODEL || DEFAULT_MODEL;

  if (!apiKey) {
    throw new Error(
      "No API key. Set TYPESAFE_API_KEY, JEV_API_KEY, or JEV_KEY.",
    );
  }

  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMs);
  let response;

  try {
    response = await fetchImpl(baseUrl, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${apiKey}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ model, state, questions }),
      signal: controller.signal,
    });
  } catch (error) {
    if (error?.name === "AbortError") {
      throw new Error("Jev request timed out.");
    }

    throw new Error(`Could not reach Jev: ${error?.message ?? String(error)}`);
  } finally {
    clearTimeout(timeout);
  }

  const text = await response.text();

  if (!response.ok) {
    throw new Error(`Jev API error ${response.status}: ${text.slice(0, 400)}`);
  }

  let body;

  try {
    body = JSON.parse(text);
  } catch {
    throw new Error("Jev returned invalid JSON.");
  }

  if (typeof body?.code === "number" && body.code !== 0) {
    throw new Error(`Jev API error ${body.code}: ${body.message ?? "unknown"}`);
  }

  return normalizeJevResponse(body);
}
