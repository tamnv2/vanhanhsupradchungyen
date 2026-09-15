import { changePermanentPassword, loginWithPassword } from './auth-service.js';
import {
  EMAIL_OTP_REQUEST_PATH,
  EMAIL_OTP_USE_PATH,
  createRuntimeEmailOtpDelivery,
  requestEmailOtpLogin,
  useEmailOtpLogin
} from './email-otp-auth.js';
import { authenticateRequest } from './session.js';
import { permissionSummary } from './permission-store.js';
import { handleReconciliationIngestRoute } from './reconciliation-route.js';
import { handleOperationalSnapshotRoute, OPERATIONAL_SNAPSHOT_PATH } from './operational-snapshot-route.js';
import {
  CloudSlice1Error,
  executeCloudSlice1Command,
  SLICE1_CLOUD_COMMAND_PATH,
  SLICE1_LOGICAL_MODULE
} from './slice1-business.js';

const API_VERSION = "v1";
const CORE_SCHEMA_VERSION = "business_core_v3";
const RUNTIME_STATE = "BUSINESS_CORE_V3";
const MAX_AUTH_BODY_BYTES = 16 * 1024;

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

async function readJsonObject(request, requestId) {
  const declaredLength = Number(request.headers.get('content-length') || 0);
  if (Number.isFinite(declaredLength) && declaredLength > MAX_AUTH_BODY_BYTES) {
    return {
      response: error('REQUEST_BODY_TOO_LARGE', 'Request body is too large.', 413, requestId),
      value: null
    };
  }

  let text;
  try {
    text = await request.text();
  } catch {
    return {
      response: error('REQUEST_BODY_INVALID', 'Request body could not be read.', 400, requestId),
      value: null
    };
  }

  if (new TextEncoder().encode(text).byteLength > MAX_AUTH_BODY_BYTES) {
    return {
      response: error('REQUEST_BODY_TOO_LARGE', 'Request body is too large.', 413, requestId),
      value: null
    };
  }

  let value;
  try {
    value = JSON.parse(text || '{}');
  } catch {
    return {
      response: error('REQUEST_JSON_INVALID', 'Request body must be valid JSON.', 400, requestId),
      value: null
    };
  }

  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    return {
      response: error('REQUEST_JSON_OBJECT_REQUIRED', 'Request body must be a JSON object.', 400, requestId),
      value: null
    };
  }

  return { response: null, value };
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
    runtime: "CLOUD",
    runtimeState: RUNTIME_STATE,
    authority: "D1",
    mutationModel: "IMMUTABLE_EVENT_IDEMPOTENT",
    correctionModel: "NEW_EVENT_NO_RAW_REWRITE",
    googleSheetsRole: "PROJECTION_ONLY",
    projectionTransport: "D1_OUTBOX_BATCH",
    projectionAvailabilityModel: "ASYNC_NON_BLOCKING",
    protectedBusinessData: true,
    anonymousMutationAllowed: false,
    businessMutationEnabled: true,
    businessMutationPath: SLICE1_CLOUD_COMMAND_PATH,
    businessSlice: SLICE1_LOGICAL_MODULE,
    blockedBusinessCommands: ["EMPLOYEE_PORTRAIT_REPLACE"]
  };
}

function authError(result, requestId) {
  const code = result?.code || 'AUTH_FAILED';
  return error(code, 'Authentication failed.', 401, requestId);
}

function passwordChangeGate(principal, requestId) {
  if (principal?.mustChangePassword !== true) return null;
  return error(
    'PASSWORD_CHANGE_REQUIRED',
    'A new permanent password must be established before ordinary product functions are available.',
    403,
    requestId
  );
}

async function requirePrincipal(request, env, requestId) {
  const auth = await authenticateRequest(request, env);
  if (!auth.ok) return { response: authError(auth, requestId), principal: null };
  return { response: null, principal: auth.principal };
}

async function handlePasswordLogin(request, env, requestId) {
  if (!env?.DB) return error('AUTH_DB_UNAVAILABLE', 'Authentication is unavailable.', 503, requestId);
  const body = await readJsonObject(request, requestId);
  if (body.response) return body.response;

  const username = typeof body.value.username === 'string' ? body.value.username.trim() : '';
  const password = typeof body.value.password === 'string' ? body.value.password : '';
  if (!username) {
    return error('LOGIN_INPUT_REQUIRED', 'Username is required.', 422, requestId);
  }

  const result = await loginWithPassword(env.DB, username, password);
  if (!result.ok) {
    if (result.code === 'ROOT_EMAIL_OTP_REQUIRED') {
      return error(
        'ROOT_EMAIL_OTP_REQUIRED',
        'ROOT authentication requires the reviewed email one-time-password flow.',
        401,
        requestId,
        { requiredMethod: 'EMAIL_OTP' }
      );
    }
    return authError(result, requestId);
  }

  return json({
    ok: true,
    principal: {
      userId: result.user.userId,
      username: result.user.username,
      employeeId: result.user.employeeId,
      displayName: result.user.displayName,
      securityLevel: result.user.securityLevel,
      mustChangePassword: result.session.mustChangePassword,
      authMethodCode: 'PASSWORD'
    },
    session: {
      token: result.session.token,
      issuedAt: result.session.issuedAt,
      expiresAt: result.session.expiresAt,
      mustChangePassword: result.session.mustChangePassword
    }
  }, 200, requestId);
}

function otpFailureResponse(result, requestId) {
  const code = result?.code || 'OTP_AUTH_FAILED';
  if (code === 'OTP_RESEND_COOLDOWN') {
    return error(
      code,
      'A new one-time password cannot be requested yet.',
      429,
      requestId,
      { retryAfterMs: Number(result?.retryAfterMs || 0) }
    );
  }
  if (code === 'ROOT_TOTP_REQUIRED') {
    return error(
      code,
      'ROOT authentication also requires the enrolled TOTP factor.',
      401,
      requestId,
      { requiredMethod: 'TOTP', challengeId: result?.challenge?.challengeId || null }
    );
  }
  if (['OTP_INVALID_OR_EXPIRED', 'OTP_RECOVERY_NOT_AVAILABLE'].includes(code)) {
    return error(code, 'One-time-password authentication was not accepted.', 401, requestId);
  }
  if (['OTP_REQUEST_CONTEXT_REQUIRED'].includes(code)) {
    return error(code, 'One-time-password request is invalid.', 422, requestId);
  }
  if ([
    'OTP_RUNTIME_NOT_CONFIGURED',
    'OTP_DELIVERY_NOT_CONFIGURED',
    'OTP_ROOT_DESTINATION_NOT_CONFIGURED',
    'OTP_DELIVERY_FAILED',
    'OTP_REQUEST_FAILED',
    'OTP_REQUEST_IN_PROGRESS',
    'OTP_STATE_COMMIT_FAILED',
    'OTP_FAILURE_STATE_COMMIT_FAILED',
    'OTP_CONSUME_FAILED'
  ].includes(code)) {
    return error(code, 'One-time-password service is temporarily unavailable.', 503, requestId);
  }
  return error(code, 'One-time-password authentication failed.', 401, requestId);
}

async function handleEmailOtpRequest(request, env, requestId) {
  if (!env?.DB) return error('AUTH_DB_UNAVAILABLE', 'Authentication is unavailable.', 503, requestId);
  const body = await readJsonObject(request, requestId);
  if (body.response) return body.response;
  const username = typeof body.value.username === 'string' ? body.value.username.trim() : '';
  if (!username) return error('LOGIN_INPUT_REQUIRED', 'Username is required.', 422, requestId);

  const result = await requestEmailOtpLogin(env.DB, username, {
    pepper: env.VHDCHY_EMAIL_OTP_PEPPER,
    rootRecoveryEmail: env.VHDCHY_ROOT_RECOVERY_EMAIL,
    deliver: createRuntimeEmailOtpDelivery(env),
    requestId
  });
  if (!result.ok) return otpFailureResponse(result, requestId);

  return json({
    ok: true,
    challenge: {
      challengeId: result.challengeId,
      purpose: result.purpose,
      destinationHint: result.destinationHint,
      expiresAt: result.expiresAt,
      resendNotBefore: result.resendNotBefore
    }
  }, 202, requestId);
}

async function handleEmailOtpUse(request, env, requestId) {
  if (!env?.DB) return error('AUTH_DB_UNAVAILABLE', 'Authentication is unavailable.', 503, requestId);
  const body = await readJsonObject(request, requestId);
  if (body.response) return body.response;

  const username = typeof body.value.username === 'string' ? body.value.username.trim() : '';
  const challengeId = typeof body.value.challengeId === 'string' ? body.value.challengeId.trim() : '';
  const code = typeof body.value.code === 'string' ? body.value.code.trim() : '';
  if (!username || !challengeId || !/^\d{4}$/.test(code)) {
    return error('OTP_USE_INPUT_REQUIRED', 'Username, challengeId and four-digit code are required.', 422, requestId);
  }

  // TOTP satisfaction is intentionally not accepted from the request body. A trusted
  // server-side TOTP verifier must compose with useEmailOtpLogin before this gate can pass.
  const result = await useEmailOtpLogin(env.DB, { username, challengeId, code }, {
    pepper: env.VHDCHY_EMAIL_OTP_PEPPER,
    requestId
  });
  if (!result.ok) return otpFailureResponse(result, requestId);

  return json({
    ok: true,
    principal: {
      userId: result.user.userId,
      username: result.user.username,
      employeeId: result.user.employeeId,
      displayName: result.user.displayName,
      securityLevel: result.user.securityLevel,
      mustChangePassword: result.session.mustChangePassword,
      authMethodCode: result.authMethodCode
    },
    session: {
      token: result.session.token,
      issuedAt: result.session.issuedAt,
      expiresAt: result.session.expiresAt,
      mustChangePassword: result.session.mustChangePassword
    }
  }, 200, requestId);
}

async function handlePasswordChange(request, env, requestId) {
  const auth = await requirePrincipal(request, env, requestId);
  if (auth.response) return auth.response;

  const body = await readJsonObject(request, requestId);
  if (body.response) return body.response;
  const newPassword = typeof body.value.newPassword === 'string' ? body.value.newPassword : '';
  if (!newPassword) {
    return error('PASSWORD_REQUIRED', 'A new permanent password is required.', 422, requestId);
  }

  const result = await changePermanentPassword(env.DB, auth.principal, newPassword);
  if (!result.ok) {
    const status = result.code === 'ROOT_PASSWORD_NOT_APPLICABLE' ? 403 : 422;
    return error(result.code, 'Password change was not accepted.', status, requestId);
  }

  return json({
    ok: true,
    changedAt: result.changedAt,
    mustChangePassword: false
  }, 200, requestId);
}

async function handleSlice1Command(request, env, requestId) {
  const auth = await requirePrincipal(request, env, requestId);
  if (auth.response) return auth.response;
  const gate = passwordChangeGate(auth.principal, requestId);
  if (gate) return gate;
  const body = await readJsonObject(request, requestId);
  if (body.response) return body.response;
  const logicalRequestId = typeof body.value.requestId === 'string' && body.value.requestId.trim()
    ? body.value.requestId.trim()
    : requestId;

  try {
    const result = await executeCloudSlice1Command({
      db: env.DB,
      principal: auth.principal,
      envelope: body.value
    });
    return json(result, 200, null);
  } catch (failure) {
    if (failure instanceof CloudSlice1Error) {
      return error(
        failure.code,
        failure.message,
        failure.status,
        logicalRequestId,
        failure.details
      );
    }
    console.error('SLICE1_COMMAND_UNEXPECTED', failure?.stack || failure);
    return error('RUNTIME_DEPENDENCY_UNAVAILABLE', 'Business mutation could not be committed.', 503, logicalRequestId);
  }
}

export async function handleRequest(request, env) {
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

  if (url.pathname === "/api/v1/auth/login" && request.method === "POST") {
    return handlePasswordLogin(request, env, requestId);
  }

  if (url.pathname === EMAIL_OTP_REQUEST_PATH && request.method === 'POST') {
    return handleEmailOtpRequest(request, env, requestId);
  }

  if (url.pathname === EMAIL_OTP_USE_PATH && request.method === 'POST') {
    return handleEmailOtpUse(request, env, requestId);
  }

  if (url.pathname === "/api/v1/auth/change-password" && request.method === "POST") {
    return handlePasswordChange(request, env, requestId);
  }

  if (url.pathname === "/api/v1/auth/me" && request.method === "GET") {
    const auth = await requirePrincipal(request, env, requestId);
    if (auth.response) return auth.response;
    const permissions = await permissionSummary(env.DB, auth.principal);
    return json({
      ok: true,
      principal: {
        userId: auth.principal.userId,
        username: auth.principal.username,
        employeeId: auth.principal.employeeId,
        displayName: auth.principal.displayName,
        securityLevel: auth.principal.securityLevel,
        deviceId: auth.principal.deviceId,
        expiresAt: auth.principal.expiresAt,
        mustChangePassword: auth.principal.mustChangePassword,
        authMethodCode: auth.principal.authMethodCode
      },
      permissions
    }, 200, requestId);
  }

  if (url.pathname === SLICE1_CLOUD_COMMAND_PATH && request.method === "POST") {
    return handleSlice1Command(request, env, requestId);
  }

  if (url.pathname === "/api/v1/reconciliation/events" && request.method === "POST") {
    return handleReconciliationIngestRoute(request, env, requestId);
  }

  if (url.pathname === OPERATIONAL_SNAPSHOT_PATH && request.method === "POST") {
    return handleOperationalSnapshotRoute(request, env, requestId);
  }

  if (url.pathname.startsWith("/api/v1/data/") || url.pathname.startsWith("/api/v1/admin/")) {
    const auth = await requirePrincipal(request, env, requestId);
    if (auth.response) return auth.response;
    const gate = passwordChangeGate(auth.principal, requestId);
    if (gate) return gate;
    return error(
      "ROUTE_NOT_IMPLEMENTED",
      "Authenticated business route is not implemented yet.",
      404,
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

export default {
  fetch: handleRequest
};
