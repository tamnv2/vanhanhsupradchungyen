export const AUTH_PATHS = Object.freeze({
  login: '/api/v1/auth/login',
  me: '/api/v1/auth/me',
  changePassword: '/api/v1/auth/change-password'
});

const TOKEN_KEY = 'vhdchy.web.session.token';
const EVIDENCE_KEY = 'vhdchy.web.session.evidence';
const REQUIRED_LAN_PROOF_HEADERS = Object.freeze([
  'X-VHDCHY-Device-Id',
  'X-VHDCHY-Security-Epoch',
  'X-VHDCHY-Timestamp-Ms',
  'X-VHDCHY-Nonce',
  'X-VHDCHY-Signature'
]);

export class WebAuthError extends Error {
  constructor(code, message, status = 0, payload = null) {
    super(message || code);
    this.name = 'WebAuthError';
    this.code = code;
    this.status = status;
    this.payload = payload;
  }
}

function parseStoredJson(storage, key) {
  try {
    const raw = storage?.getItem(key);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

async function parseJsonResponse(response, path) {
  const text = await response.text();
  let payload;
  try {
    payload = JSON.parse(text);
  } catch {
    throw new WebAuthError('RESPONSE_NOT_JSON', `${path}: phản hồi không phải JSON.`, response.status);
  }

  if (!response.ok) {
    const code = payload?.error?.code || `HTTP_${response.status}`;
    throw new WebAuthError(code, payload?.error?.message || code, response.status, payload);
  }
  return payload;
}

function normalizeRuntime(value) {
  const runtime = String(value || '').toUpperCase();
  return runtime === 'LAN' || runtime === 'CLOUD' ? runtime : 'UNKNOWN';
}

function normalizeCredentials(username, password) {
  const normalizedUsername = String(username || '').trim();
  const normalizedPassword = typeof password === 'string' ? password : '';
  if (!normalizedUsername || !normalizedPassword) {
    throw new WebAuthError('LOGIN_INPUT_REQUIRED', 'Cần nhập tài khoản và mật khẩu.');
  }
  return { username: normalizedUsername, password: normalizedPassword };
}

function validatedLanProofHeaders(proof) {
  if (!proof || typeof proof !== 'object') {
    throw new WebAuthError('LAN_SIGNER_INVALID_PROOF', 'Bộ ký LAN không trả về bằng chứng hợp lệ.');
  }
  const headers = {};
  for (const name of REQUIRED_LAN_PROOF_HEADERS) {
    const value = proof[name] ?? proof.headers?.[name];
    if (typeof value !== 'string' || !value.trim()) {
      throw new WebAuthError('LAN_SIGNER_INVALID_PROOF', `Thiếu ${name} từ bộ ký LAN.`);
    }
    headers[name] = value.trim();
  }
  return headers;
}

function validSessionEvidence(cached, nowMs = Date.now()) {
  if (!cached?.principal || !cached?.session) return false;
  const expiresAtMs = Date.parse(cached.session.expiresAt || '');
  return Number.isFinite(expiresAtMs) && expiresAtMs > Number(nowMs);
}

export function createAuthClient(options = {}) {
  const fetchImpl = options.fetchImpl || globalThis.fetch?.bind(globalThis);
  const storage = options.storage || globalThis.sessionStorage;
  const getLanSigner = options.getLanSigner || (() => globalThis.VHDCHY_LAN_SIGNER);
  if (typeof fetchImpl !== 'function') throw new Error('FETCH_REQUIRED');

  let runtime = 'UNKNOWN';
  let capabilities = null;

  function configure(next = {}) {
    runtime = normalizeRuntime(next.runtime);
    capabilities = next.capabilities || null;
    return { runtime, capabilities };
  }

  function token() {
    return storage?.getItem(TOKEN_KEY) || '';
  }

  function evidence() {
    return parseStoredJson(storage, EVIDENCE_KEY);
  }

  function clear() {
    storage?.removeItem(TOKEN_KEY);
    storage?.removeItem(EVIDENCE_KEY);
  }

  function persist(sessionToken, principal, session) {
    storage?.setItem(TOKEN_KEY, sessionToken);
    storage?.setItem(EVIDENCE_KEY, JSON.stringify({ principal, session }));
  }

  async function request(path, requestOptions = {}) {
    const headers = new Headers(requestOptions.headers || {});
    const authToken = token();
    if (requestOptions.auth !== false && authToken) {
      headers.set('Authorization', `Bearer ${authToken}`);
    }
    const response = await fetchImpl(path, {
      ...requestOptions,
      headers,
      cache: 'no-store',
      credentials: 'same-origin'
    });
    return parseJsonResponse(response, path);
  }

  async function login(username, password) {
    if (runtime === 'UNKNOWN') {
      throw new WebAuthError('RUNTIME_NOT_READY', 'Chưa xác định được runtime để đăng nhập.');
    }

    const credentials = normalizeCredentials(username, password);
    const rawBody = JSON.stringify(credentials);
    const headers = new Headers({
      'Content-Type': 'application/json',
      'X-Request-Id': crypto.randomUUID()
    });

    if (runtime === 'LAN') {
      const signer = getLanSigner?.();
      if (!signer || typeof signer.signRequest !== 'function') {
        throw new WebAuthError(
          'LAN_SIGNER_REQUIRED',
          'LAN Web yêu cầu bộ ký của thiết bị đã ghép đôi; không gửi mật khẩu khi chưa có bằng chứng ký.'
        );
      }
      const proof = await signer.signRequest({
        method: 'POST',
        target: AUTH_PATHS.login,
        body: rawBody
      });
      const proofHeaders = validatedLanProofHeaders(proof);
      for (const [name, value] of Object.entries(proofHeaders)) headers.set(name, value);
    }

    const payload = await request(AUTH_PATHS.login, {
      method: 'POST',
      headers,
      body: rawBody,
      auth: false
    });
    const sessionToken = payload?.session?.token;
    const principal = payload?.principal || payload?.user;
    if (typeof sessionToken !== 'string' || sessionToken.length < 32 || !principal?.userId) {
      throw new WebAuthError('LOGIN_RESPONSE_INVALID', 'Service không trả về phiên đăng nhập hợp lệ.', 502, payload);
    }

    persist(sessionToken, principal, payload.session);
    return { principal, session: payload.session, runtime };
  }

  async function me() {
    const authToken = token();
    if (!authToken) return null;

    if (runtime === 'LAN') {
      const cached = evidence();
      if (!validSessionEvidence(cached)) {
        clear();
        return null;
      }
      return {
        principal: cached.principal,
        session: cached.session,
        source: 'LAN_LOGIN_EVIDENCE'
      };
    }

    try {
      const payload = await request(AUTH_PATHS.me, { method: 'GET' });
      const cached = evidence();
      persist(authToken, payload.principal, cached?.session || null);
      return { principal: payload.principal, session: cached?.session || null, permissions: payload.permissions };
    } catch (error) {
      if (error?.status === 401) clear();
      throw error;
    }
  }

  async function changePassword(newPassword) {
    if (runtime !== 'CLOUD') {
      throw new WebAuthError(
        'PASSWORD_CHANGE_ROUTE_UNAVAILABLE',
        'Runtime hiện tại chưa công bố route đổi mật khẩu cho Web; thao tác được giữ fail-closed.'
      );
    }
    const password = typeof newPassword === 'string' ? newPassword : '';
    if (!password) throw new WebAuthError('PASSWORD_REQUIRED', 'Cần nhập mật khẩu mới.');
    return request(AUTH_PATHS.changePassword, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ newPassword: password })
    });
  }

  return Object.freeze({
    configure,
    request,
    login,
    me,
    changePassword,
    clear,
    token,
    evidence,
    get runtime() { return runtime; },
    get capabilities() { return capabilities; }
  });
}
