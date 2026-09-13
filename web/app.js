const byId = (id) => document.getElementById(id);

const commitLabels = Object.freeze({
  CLOUD_COMMITTED: 'Đã chốt Cloud',
  LAN_ACCEPTED_PENDING_SYNC: 'LAN đã nhận, chờ Cloud',
  LAN_RECONCILED_CLOUD_COMMITTED: 'LAN đã đồng bộ Cloud',
  QUEUED_CLIENT_LOCAL: 'Chỉ đang chờ trên thiết bị',
  SYNC_CONFLICT: 'Có xung đột đồng bộ'
});

async function getJson(path) {
  const response = await fetch(path, { cache: 'no-store', credentials: 'same-origin' });
  const text = await response.text();
  let payload;
  try { payload = JSON.parse(text); }
  catch { throw new Error(`${path}: phản hồi không phải JSON (${response.status})`); }
  if (!response.ok) throw new Error(`${path}: HTTP ${response.status} ${payload?.error?.code || ''}`.trim());
  return payload;
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
    byId('environmentBadge').textContent = meta.environment || 'UNKNOWN';
    byId('runtimeStatus').textContent = `Runtime: ${runtime}`;
    byId('serviceSummary').textContent = `${meta.service || 'Service'} · ${meta.runtimeState || meta.readiness || 'ready'} · ${meta.build || meta.version || ''}`;
  } else {
    byId('environmentBadge').textContent = 'DEGRADED';
    byId('runtimeStatus').textContent = 'Runtime: không truy cập được';
    byId('serviceSummary').textContent = metaResult.reason.message;
  }

  if (capabilitiesResult.status === 'fulfilled') {
    const capabilities = capabilitiesResult.value;
    if (capabilities.businessMutationEnabled === false || capabilities.anonymousMutationAllowed === false) {
      byId('message').textContent = 'Service đang bảo vệ nghiệp vụ theo contract. Chỉ các route đã xác thực/được kích hoạt mới được phép mutation.';
    }
  }

  if (syncResult.status === 'fulfilled') {
    const sync = syncResult.value;
    byId('cloudSyncSummary').textContent = sync.lastCloudSyncAt
      ? `Lần gần nhất: ${sync.lastCloudSyncAt}; đang chờ: ${sync.pendingCloudSync ?? 0}`
      : `Chưa có checkpoint; đang chờ: ${sync.pendingCloudSync ?? 0}`;
    byId('googleSummary').textContent = `Công việc Google đang chờ: ${sync.pendingGoogleWork ?? 0}`;
    byId('conflictSummary').textContent = `Xung đột chưa xử lý: ${sync.conflictCount ?? 0}`;
  } else {
    byId('cloudSyncSummary').textContent = 'Cloud runtime hiện chưa công bố endpoint sync status.';
    byId('googleSummary').textContent = 'Theo dõi qua Service/Gateway; không đọc Sheet làm authority.';
    byId('conflictSummary').textContent = 'Chưa có conflict endpoint hoạt động.';
  }
}

document.querySelectorAll('nav button').forEach((button) => {
  button.addEventListener('click', () => {
    document.querySelectorAll('nav button').forEach((item) => item.classList.remove('active'));
    button.classList.add('active');
    byId('message').textContent = `${button.textContent}: shell đã sẵn sàng; feature route sẽ mở theo permission/domain slice khi Service PASS.`;
  });
});

window.VHDCHY = Object.freeze({ commitLabels });
refreshRuntime().catch((error) => {
  byId('serviceSummary').textContent = error.message;
});
