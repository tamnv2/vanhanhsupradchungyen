async function checkD1(env) {
  try {
    const row = await env.DB.prepare("SELECT 1 AS ok").first();
    return { ok: row?.ok === 1 };
  } catch {
    return { ok: false, error: "D1_UNAVAILABLE" };
  }
}

async function checkGoogleGateway(env) {
  if (!env.GAS_EXEC_URL) return { ok: false, error: "GAS_EXEC_URL_MISSING" };

  try {
    const response = await fetch(env.GAS_EXEC_URL, {
      redirect: "follow",
      cf: { cacheTtl: 0, cacheEverything: false }
    });
    if (!response.ok) return { ok: false, error: "GATEWAY_HTTP_ERROR", status: response.status };

    const payload = await response.json();
    const expected = String(env.APP_ENV || "").toUpperCase();
    const ok = payload?.ok === true &&
      payload?.service === "VHDCHY_GOOGLE_GATEWAY" &&
      payload?.environment === expected;

    return ok ? { ok: true } : { ok: false, error: "GATEWAY_IDENTITY_MISMATCH" };
  } catch {
    return { ok: false, error: "GATEWAY_UNAVAILABLE" };
  }
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);

    if (url.pathname === "/health") {
      const d1 = await checkD1(env);
      return Response.json({
        ok: d1.ok,
        service: "VHDCHY_WORKER",
        environment: env.APP_ENV || "unknown",
        build: env.BUILD_SHA || "unknown",
        d1
      }, { status: d1.ok ? 200 : 503 });
    }

    if (url.pathname === "/health/deep") {
      const [d1, googleGateway] = await Promise.all([
        checkD1(env),
        checkGoogleGateway(env)
      ]);
      const ok = d1.ok && googleGateway.ok;
      return Response.json({
        ok,
        service: "VHDCHY_WORKER",
        environment: env.APP_ENV || "unknown",
        build: env.BUILD_SHA || "unknown",
        d1,
        googleGateway
      }, { status: ok ? 200 : 503 });
    }

    if (url.pathname === "/api/v1/meta" && request.method === "GET") {
      return Response.json({
        ok: true,
        service: "VHDCHY_WORKER",
        apiVersion: "v1",
        environment: env.APP_ENV || "unknown",
        build: env.BUILD_SHA || "unknown",
        runtimeState: "FOUNDATION"
      });
    }

    return Response.json({ ok: false, code: "NOT_FOUND" }, { status: 404 });
  }
};
