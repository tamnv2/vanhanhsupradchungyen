const API_VERSION = "v1";
const CORE_SCHEMA_VERSION = "business_core_v2";
const RUNTIME_STATE = "BUSINESS_CORE_V2";

function json(payload, status = 200, requestId = null) {
  const body = requestId ? { ...payload, requestId } : payload;
  return Response.json(body, {
    status,
    headers: {
      "cache-control": "no-store",
      "x-content-type-options": "nosniff"
    }
  });
}

function error(code, message, status, requestId, details = undefined) {
  const payload = { ok: false, error: { code, message } };
  if (details !== undefined) payload.error.details = details;
  return json(payload, status, requestId);
}

async function readCoreSchema(env) {
  try {
    const row = await env.DB.prepare(
      "SELECT value FROM vhdchy_meta WHERE key = 'schema_version'"
    ).first();
    return row?.value || null;
  } catch {
    return null;
  }
}

async function checkD1(env) {
  try {
    const row = await env.DB.prepare(
      "SELECT value AS schema_version FROM vhdchy_meta WHERE key = 'schema_version'"
    ).first();
    const schemaVersion = row?.schema_version || null;
    return {
      ok: schemaVersion === CORE_SCHEMA_VERSION,
      schemaVersion,
      expectedSchemaVersion: CORE_SCHEMA_VERSION
    };
  } catch {
    return {
      ok: false,
      error: "D1_CORE_SCHEMA_UNAVAILABLE",
      expectedSchemaVersion: CORE_SCHEMA_VERSION
    };
  }
}

async function checkGoogleGateway(env) {
  if (!env.GAS_EXEC_URL) return { ok: false, error: "GAS_EXEC_URL_MISSING", critical: false };

  try {
    const response = await fetch(env.GAS_EXEC_URL, {
      redirect: "follow",
      cache: "no-store"
    });
    if (!response.ok) {
      return { ok: false, error: "GATEWAY_HTTP_ERROR", status: response.status, critical: false };
    }

    const payload = await response.json();
    const expected = String(env.APP_ENV || "").toUpperCase();
    const ok = payload?.ok === true &&
      payload?.service === "VHDCHY_GOOGLE_GATEWAY" &&
      String(payload?.environment || "").toUpperCase() === expected;

    return ok
      ? { ok: true, critical: false }
      : { ok: false, error: "GATEWAY_IDENTITY_MISMATCH", critical: false };
  } catch {
    return { ok: false, error: "GATEWAY_UNAVAILABLE", critical: false };
  }
}

function publicCapabilities() {
  return {
    ok: true,
    apiVersion: API_VERSION,
    runtime: RUNTIME_STATE,
    authority: "D1",
    mutationModel: "IMMUTABLE_EVENT_IDEMPOTENT",
    correctionModel: "NEW_EVENT_NO_RAW_REWRITE",
    googleSheetsRole: "PROJECTION_ONLY",
    projectionTransport: "D1_OUTBOX_BATCH",
    projectionAvailabilityModel: "ASYNC_NON_BLOCKING",
    protectedBusinessData: true,
    anonymousMutationAllowed: false
  };
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    const requestId = crypto.randomUUID();

    if (url.pathname === "/health" && request.method === "GET") {
      const d1 = await checkD1(env);
      return json({
        ok: d1.ok,
        service: "VHDCHY_WORKER",
        environment: env.APP_ENV || "unknown",
        build: env.BUILD_SHA || "unknown",
        d1
      }, d1.ok ? 200 : 503, requestId);
    }

    if (url.pathname === "/health/deep" && request.method === "GET") {
      const [d1, googleGateway] = await Promise.all([
        checkD1(env),
        checkGoogleGateway(env)
      ]);
      return json({
        ok: d1.ok,
        degraded: !googleGateway.ok,
        service: "VHDCHY_WORKER",
        environment: env.APP_ENV || "unknown",
        build: env.BUILD_SHA || "unknown",
        d1,
        googleGateway
      }, d1.ok ? 200 : 503, requestId);
    }

    if (url.pathname === "/health/integrations" && request.method === "GET") {
      const googleGateway = await checkGoogleGateway(env);
      return json({
        ok: googleGateway.ok,
        service: "VHDCHY_WORKER",
        environment: env.APP_ENV || "unknown",
        googleGateway
      }, googleGateway.ok ? 200 : 207, requestId);
    }

    if (url.pathname === "/api/v1/meta" && request.method === "GET") {
      const schemaVersion = await readCoreSchema(env);
      const ok = schemaVersion === CORE_SCHEMA_VERSION;
      return json({
        ok,
        service: "VHDCHY_WORKER",
        apiVersion: API_VERSION,
        environment: env.APP_ENV || "unknown",
        build: env.BUILD_SHA || "unknown",
        runtimeState: RUNTIME_STATE,
        schemaVersion,
        expectedSchemaVersion: CORE_SCHEMA_VERSION
      }, ok ? 200 : 503, requestId);
    }

    if (url.pathname === "/api/v1/capabilities" && request.method === "GET") {
      return json(publicCapabilities(), 200, requestId);
    }

    if (url.pathname.startsWith("/api/v1/data/") || url.pathname.startsWith("/api/v1/admin/")) {
      return error(
        "AUTH_REQUIRED",
        "Business data APIs remain closed until authenticated session and permission enforcement are active.",
        401,
        requestId
      );
    }

    if (url.pathname.startsWith("/api/") && request.method === "OPTIONS") {
      return error(
        "CORS_NOT_ENABLED",
        "Cross-origin API access is not enabled before the client security contract is active.",
        403,
        requestId
      );
    }

    return error("NOT_FOUND", "Route not found.", 404, requestId);
  }
};
