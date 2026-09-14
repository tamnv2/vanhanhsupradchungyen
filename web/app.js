import { createAuthClient, WebAuthError } from './auth.js';

const byId = (id) => document.getElementById(id);
const authClient = createAuthClient();

const commitLabels = Object.freeze({
  CLOUD_COMMITTED: 'Đã chốt Cloud',
  LAN_ACCEPTED_PENDING_SYNC: 'LAN đã nhận, chờ Cloud',
  LAN_RECONCILED_CLOUD_COMMITTED: 'LAN đã đồng bộ Cloud',
  QUEUED_CLIENT_LOCAL: 'Chỉ đang chờ trên thiết bị',
  SYNC_CONFLICT: 'Có xung đột đồng bộ'
});

const viewTitles = Object.freeze({
  dashboard: 'Tổng quan',
  business: 'Nghiệp vụ',
  people: 'Nhân sự',
  attendance: 'Ra/Vào & Công nhật',
  resources: 'Tài nguyên',
  documents: 'Biên bản',
  history: 'Lịch sử',
  sync: 'Đồng bộ',
  admin: 'Quản trị',
  settings: 'Cài đặt'
});

let currentRuntime = 'UNKNOWN';
let currentCapabilities = null;

async function getJson(path) {
  const response = await fetch(path, { cache: 'no-store', credentials: 'same-origin' });
  const text = await response.text();
  let payload;
  try { payload = JSON.parse(text); }
  catch { throw new Error(`${path}: phản hồi không phải JSON (${response.status})`); }
  if (!response.ok) {
    const error = new Error(`${path}: HTTP ${response.status} ${payload?.error?.code || ''}`.trim());
    error.status = response.status;
    error.payload = payload;
    throw error;
  }
  return payload;
}

function setEnvironment(value, ok = true) {
  const badge = byId('environmentBadge');
  badge.textContent = value || 'UNKNOWN';
  badge.className = `pill ${ok ? 'pill-ok' : 'pill-warn'}`;
}

function setView(view) {
  document.querySelectorAll('[data-view]').forEach((button) => {
    button.classList.toggle('active', button.dataset.view === view);
  });
  byId('pageTitle').textContent = viewTitles[view] || 'VHDCHY';
  byId('message').textContent = view === 'dashboard'
    ? 'Phiên đã xác thực. Các màn hình nghiệp vụ vẫn fail-closed cho tới khi permission và domain slice tương ứng PASS.'
    : `${viewTitles[view] || 'Chức năng'}: shell giao diện đã sẵn sàng; route nghiệp vụ chỉ mở khi Service và permission tương ứng PASS.`;
  byId('sidebar').classList.remove('open');
}

function setAuthMessage(message, tone = '') {
  const element = byId('authMessage');
  element.textContent = message;
  element.className = `auth-message${tone ? ` ${tone}` : ''}`;
}

function resetPrincipal() {
  byId('accountName').textContent = 'Chưa đăng nhập';
  byId('accountRole').textContent = 'Phiên xác thực chưa có';
  byId('accountInitial').textContent = '?';
  byId('logoutButton').hidden = true;
}

function renderPrincipal(principal, session = null) {
  const name = principal?.displayName || principal?.username || 'Tài khoản';
  byId('accountName').textContent = name;
  byId('accountRole').textContent = principal?.securityLevel || 'Đã xác thực';
  byId('accountInitial').textContent = name.trim().slice(0, 1).toUpperCase() || '?';
  byId('logoutButton').hidden = false;
  return principal?.mustChangePassword === true || session?.mustChangePassword === true;
}

function lockShell() {
  document.body.classList.add('auth-locked');
}

function unlockShell() {
  byId('passwordChangePanel').hidden = true;
  byId('loginForm').hidden = false;
  document.body.classList.remove('auth-locked');
}

function requirePasswordChange(principal, session) {
  lockShell();
  renderPrincipal(principal, session);
  byId('loginForm').hidden = true;
  byId('passwordChangePanel').hidden = false;
  setAuthMessage('Tài khoản phải thiết lập mật khẩu mới trước khi dùng chức năng vận hành.', 'error');
}

function authErrorText(error) {
  const code = error?.code || error?.payload?.error?.code || '';
  const known = {
    AUTH_FAILED: 'Sai thông tin đăng nhập hoặc phiên không hợp lệ.',
    ACCOUNT_NOT_FOUND: 'Tài khoản không hợp lệ.',
    ACCOUNT_NOT_ACTIVE: 'Tài khoản không ở trạng thái hoạt động.',
    ROOT_EMAIL_OTP_REQUIRED: 'ROOT phải đăng nhập bằng mật khẩu một lần qua email theo V6. Public email-OTP route chưa được công bố nên Web giữ fail-closed.',
    LAN_SIGNER_REQUIRED: 'LAN Web yêu cầu bộ ký của thiết bị đã ghép đôi. Mật khẩu chưa được gửi đi.',
    LAN_SIGNER_INVALID_PROOF: 'Bộ ký LAN không cung cấp đủ bằng chứng thiết bị.',
    RUNTIME_NOT_READY: 'Chưa xác định được runtime để đăng nhập.',
    PASSWORD_CHANGE_ROUTE_UNAVAILABLE: 'Runtime hiện tại chưa có route đổi mật khẩu Web đã được duyệt.',
    PASSWORD_CHANGE_REQUIRED: 'Tài khoản phải đổi mật khẩu trước khi dùng chức năng vận hành.'
  };
  return known[code] || error?.message || 'Không thể hoàn tất xác thực.';
}

function updateAuthAvailability() {
  byId('authRuntimeBadge').textContent = currentRuntime;
  const loginButton = byId('loginButton');

  if (currentRuntime === 'UNKNOWN') {
    loginButton.disabled = true;
    setAuthMessage('Không xác định được Service runtime; đăng nhập đang fail-closed.', 'error');
    return;
  }

  if (currentRuntime === 'LAN' && currentCapabilities?.secureMutationTransportEnabled !== true) {
    loginButton.disabled = true;
    setAuthMessage('LAN đang ở chế độ read-only hoặc chưa có HTTPS hợp lệ; không gửi thông tin đăng nhập.', 'error');
    return;
  }

  loginButton.disabled = false;
  if (currentRuntime === 'LAN') {
    setAuthMessage('LAN login chỉ được gửi khi bộ ký thiết bị đã ghép đôi cung cấp đủ P-256 request proof.');
  } else {
    setAuthMessage('Cloud Service sẵn sàng nhận đăng nhập theo contract hiện hành.');
  }
}

async function restoreSession() {
  if (!authClient.token()) {
    lockShell();
    resetPrincipal();
    return;
  }

  try {
    const result = await authClient.me();
    if (!result?.principal) {
      lockShell();
      resetPrincipal();
      return;
    }
    const mustChangePassword = renderPrincipal(result.principal, result.session);
    if (mustChangePassword) {
      requirePasswordChange(result.principal, result.session);
      return;
    }
    unlockShell();
    setAuthMessage('Phiên đăng nhập đã được khôi phục.', 'ok');
  } catch (error) {
    lockShell();
    resetPrincipal();
    setAuthMessage(authErrorText(error), 'error');
  }
}

async function refreshRuntime() {
  const [metaResult, capabilitiesResult, syncResult] = await Promise.allSettled([
    getJson('/api/v1/meta'),
    getJson('/api/v1/capabilities'),
    getJson('/api/v1/sync/status')
  ]);

  if (metaResult.status === 'fulfilled') {
    const meta = metaResult.value;
    currentRuntime = meta.runtime || (meta.service === 'VHDCHY_WORKER' ? 'CLOUD' : 'UNKNOWN');
    setEnvironment(meta.environment || 'UNKNOWN', meta.ok !== false);
    byId('runtimeStatus').textContent = `Runtime: ${currentRuntime}`;
    byId('runtimeDetail').textContent = currentRuntime;
    byId('serviceHeadline').textContent = meta.ok === false ? 'Suy giảm' : 'Sẵn sàng';
    byId('serviceSummary').textContent = `${meta.service || 'Service'} · ${meta.runtimeState || meta.readiness || 'ready'} · ${meta.build || meta.version || ''}`;
  } else {
    currentRuntime = 'UNKNOWN';
    setEnvironment('DEGRADED', false);
    byId('runtimeStatus').textContent = 'Runtime: không truy cập được';
    byId('runtimeDetail').textContent = 'Không truy cập được';
    byId('serviceHeadline').textContent = 'Mất kết nối';
    byId('serviceSummary').textContent = metaResult.reason.message;
  }

  if (capabilitiesResult.status === 'fulfilled') {
    currentCapabilities = capabilitiesResult.value;
    if (currentCapabilities.businessMutationEnabled === false || currentCapabilities.anonymousMutationAllowed === false) {
      byId('message').textContent = 'Service đang bảo vệ nghiệp vụ. Chỉ route đã xác thực, đủ permission và readiness mới được phép mutation.';
    }
  } else {
    currentCapabilities = null;
  }

  authClient.configure({ runtime: currentRuntime, capabilities: currentCapabilities });
  updateAuthAvailability();

  if (syncResult.status === 'fulfilled') {
    const sync = syncResult.value;
    const pendingCloud = sync.pendingCloudSync ?? 0;
    const pendingGoogle = sync.pendingGoogleWork ?? 0;
    const conflicts = sync.conflictCount ?? 0;
    byId('cloudSyncHeadline').textContent = pendingCloud ? `${pendingCloud} đang chờ` : 'Không tồn';
    byId('cloudSyncSummary').textContent = sync.lastCloudSyncAt
      ? `Lần gần nhất: ${sync.lastCloudSyncAt}`
      : 'Chưa có checkpoint Cloud.';
    byId('cloudSyncDetail').textContent = pendingCloud ? `${pendingCloud} công việc đang chờ` : 'Không có công việc chờ';
    byId('googleHeadline').textContent = pendingGoogle ? `${pendingGoogle} đang chờ` : 'Không tồn';
    byId('googleSummary').textContent = `Công việc Google đang chờ: ${pendingGoogle}`;
    byId('googleDetail').textContent = pendingGoogle ? `${pendingGoogle} công việc đang chờ` : 'Không có công việc chờ';
    byId('conflictHeadline').textContent = String(conflicts);
    byId('conflictSummary').textContent = `Xung đột chưa xử lý: ${conflicts}`;
    byId('conflictDetail').textContent = conflicts ? `${conflicts} cần xử lý` : 'Không có xung đột';
  } else {
    byId('cloudSyncHeadline').textContent = 'Chưa công bố';
    byId('cloudSyncSummary').textContent = 'Runtime hiện chưa công bố endpoint sync status.';
    byId('cloudSyncDetail').textContent = 'Endpoint chưa khả dụng';
    byId('googleHeadline').textContent = 'Theo Service';
    byId('googleSummary').textContent = 'Google là downstream; không đọc Sheet làm authority.';
    byId('googleDetail').textContent = 'Theo Service/Gateway';
    byId('conflictHeadline').textContent = '—';
    byId('conflictSummary').textContent = 'Chưa có conflict endpoint hoạt động.';
    byId('conflictDetail').textContent = 'Endpoint chưa khả dụng';
  }
}

byId('loginForm').addEventListener('submit', async (event) => {
  event.preventDefault();
  const loginButton = byId('loginButton');
  const passwordInput = byId('passwordInput');
  loginButton.disabled = true;
  setAuthMessage('Đang xác thực…');
  try {
    const result = await authClient.login(byId('usernameInput').value, passwordInput.value);
    passwordInput.value = '';
    const mustChangePassword = renderPrincipal(result.principal, result.session);
    if (mustChangePassword) {
      requirePasswordChange(result.principal, result.session);
    } else {
      unlockShell();
      setAuthMessage('Đăng nhập thành công.', 'ok');
    }
  } catch (error) {
    passwordInput.value = '';
    lockShell();
    setAuthMessage(authErrorText(error), 'error');
  } finally {
    loginButton.disabled = false;
    updateAuthAvailability();
  }
});

byId('passwordChangeForm').addEventListener('submit', async (event) => {
  event.preventDefault();
  const button = byId('passwordChangeButton');
  const first = byId('newPasswordInput');
  const second = byId('confirmPasswordInput');
  if (first.value !== second.value) {
    setAuthMessage('Hai lần nhập mật khẩu mới không khớp.', 'error');
    return;
  }

  button.disabled = true;
  try {
    await authClient.changePassword(first.value);
    first.value = '';
    second.value = '';
    const result = await authClient.me();
    if (!result?.principal || renderPrincipal(result.principal, result.session)) {
      throw new WebAuthError('PASSWORD_CHANGE_REQUIRED', 'Service chưa xác nhận kết thúc trạng thái bắt buộc đổi mật khẩu.');
    }
    unlockShell();
    setAuthMessage('Đã thiết lập mật khẩu mới.', 'ok');
  } catch (error) {
    first.value = '';
    second.value = '';
    setAuthMessage(authErrorText(error), 'error');
  } finally {
    button.disabled = false;
  }
});

byId('recoveryButton').addEventListener('click', () => {
  setAuthMessage(
    'V6 yêu cầu luồng mật khẩu một lần qua email cho ROOT và khôi phục tài khoản thường. Service chưa công bố public email-OTP delivery route nên Web không phát sinh yêu cầu giả.',
    'error'
  );
});

byId('logoutButton').addEventListener('click', () => {
  authClient.clear();
  resetPrincipal();
  lockShell();
  byId('loginForm').hidden = false;
  byId('passwordChangePanel').hidden = true;
  setAuthMessage('Đã xóa bearer token khỏi phiên Web hiện tại. Server-side logout chưa được công bố nên không được coi là thu hồi phiên phía Service.', 'ok');
});

document.querySelectorAll('[data-view]').forEach((button) => {
  button.addEventListener('click', () => setView(button.dataset.view));
});

document.querySelectorAll('[data-view-jump]').forEach((button) => {
  button.addEventListener('click', () => setView(button.dataset.viewJump));
});

byId('menuButton').addEventListener('click', () => byId('sidebar').classList.toggle('open'));

window.VHDCHY = Object.freeze({ commitLabels });

(async () => {
  await refreshRuntime();
  await restoreSession();
})();
