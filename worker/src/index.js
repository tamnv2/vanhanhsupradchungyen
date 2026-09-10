export default {
  async fetch(request, env) {
    const url = new URL(request.url);

    if (url.pathname === "/health") {
      let d1 = { ok: false };
      try {
        const row = await env.DB.prepare("SELECT 1 AS ok").first();
        d1 = { ok: row?.ok === 1 };
      } catch (error) {
        d1 = { ok: false, error: "D1_UNAVAILABLE" };
      }

      return Response.json({
        ok: d1.ok,
        service: "VHDCHY_WORKER",
        environment: env.APP_ENV || "unknown",
        d1
      }, { status: d1.ok ? 200 : 503 });
    }

    return Response.json({ ok: false, code: "NOT_FOUND" }, { status: 404 });
  }
};
