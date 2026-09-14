const byId = (id) => document.getElementById(id);

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
    ? 'Các màn hình nghiệp vụ vẫn fail-closed cho tới khi auth, permission và domain slice tương ứng PASS.'
    : `${viewTitles[view] || 'Chức năng'}: shell giao diện đã sẵn sàng; route nghiệp vụ chỉ mở khi Service và permission tương ứng PASS.`;
  byId('sidebar').classList.remove('open');
}

async function refreshPrincipal() {
  try {
    const result = await getJson('/api/v1/auth/me');
    const principal = result.principal || {};
    const name = principal.displayName || principal.username || 'Tài khoản';
    byId('accountName').textContent = name;
    byId('accountRole').textContent = principal.securityLevel || 'Đã xác thực';
    byId('accountInitial').textContent = name.trim().slice(0, 1).toUpperCase() || '?';
  } catch (error) {
    if (error.status === 401) {
      byId('accountName').textContent = 'Chưa đăng nhập';
      byId('accountRole').textContent = 'Auth route đang fail-closed';
      byId('accountInitial').textContent = '?';
      return;
    }
    byId('accountName').textContent = 'Không xác định';
    byId('accountRole').textContent = 'Không đọc được phiên';
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
    const runtime = meta.runtime || (meta.service === 'VHDCHY_WORKER' ? 'CLOUD' : 'UNKNOWN');
    setEnvironment(meta.environment || 'UNKNOWN', meta.ok !== false);
    byId('runtimeStatus').textContent = `Runtime: ${runtime}`;
    byId('runtimeDetail').textContent = runtime;
    byId('serviceHeadline').textContent = meta.ok === false ? 'Suy giảm' : 'Sẵn sàng';
    byId('serviceSummary').textContent = `${meta.service || 'Service'} · ${meta.runtimeState || meta.readiness || 'ready'} · ${meta.build || meta.version || ''}`;
  } else {
    setEnvironment('DEGRADED', false);
    byId('runtimeStatus').textContent = 'Runtime: không truy cập được';
    byId('runtimeDetail').textContent = 'Không truy cập được';
    byId('serviceHeadline').textContent = 'Mất kết nối';
    byId('serviceSummary').textContent = metaResult.reason.message;
  }

  if (capabilitiesResult.status === 'fulfilled') {
    const capabilities = capabilitiesResult.value;
    if (capabilities.businessMutationEnabled === false || capabilities.anonymousMutationAllowed === false) {
      byId('message').textContent = 'Service đang bảo vệ nghiệp vụ. Chỉ route đã xác thực, đủ permission và readiness mới được phép mutation.';
    }
  }

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

document.querySelectorAll('[data-view]').forEach((button) => {
  button.addEventListener('click', () => setView(button.dataset.view));
});

document.querySelectorAll('[data-view-jump]').forEach((button) => {
  button.addEventListener('click', () => setView(button.dataset.viewJump));
});

byId('menuButton').addEventListener('click', () => byId('sidebar').classList.toggle('open'));

window.VHDCHY = Object.freeze({ commitLabels });
Promise.allSettled([refreshRuntime(), refreshPrincipal()]);
