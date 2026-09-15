import { createAuthClient } from './auth.js';
import { createBusinessClient } from './business.js';

const SLICE_VIEWS = new Set(['business', 'people', 'attendance']);
const STATUS_CACHE_MS = 5000;
const sliceAuthClient = createAuthClient();
const sliceBusinessClient = createBusinessClient(sliceAuthClient);
let lastEmployeeCreateResult = null;

export function deriveSliceSurfaceState(input = {}) {
  const capabilities = input.capabilities || null;
  const sync = input.sync || null;
  const loadError = input.loadError || null;
  const authenticated = input.authenticated === true;

  if (loadError) {
    return Object.freeze({
      phase: 'error',
      label: 'Không đọc được trạng thái',
      runtime: 'UNKNOWN',
      mutationReady: false,
      authenticated,
      conflictCount: 0,
      pendingGoogle: null,
      detail: 'Không thể xác nhận capability của Service; mọi thao tác ghi tiếp tục fail-closed.'
    });
  }
  if (!capabilities) {
    return Object.freeze({
      phase: 'loading',
      label: 'Đang kiểm tra',
      runtime: 'UNKNOWN',
      mutationReady: false,
      authenticated,
      conflictCount: 0,
      pendingGoogle: null,
      detail: 'Đang xác định runtime và readiness của Slice-1.'
    });
  }

  const runtimeRaw = String(capabilities.runtime || '').toUpperCase();
  const runtime = runtimeRaw === 'CLOUD' || runtimeRaw === 'LAN' ? runtimeRaw : 'UNKNOWN';
  const mutationReady = capabilities.businessMutationEnabled === true;
  const conflictCount = Math.max(0, Number(sync?.conflictCount || 0));
  const pendingGoogleRaw = sync?.pendingGoogleWork;
  const pendingGoogle = Number.isFinite(Number(pendingGoogleRaw)) ? Math.max(0, Number(pendingGoogleRaw)) : null;
  const phase = mutationReady ? 'ready' : 'blocked';
  const label = mutationReady
    ? (authenticated ? 'Slice-1 sẵn sàng' : 'Route sẵn sàng · cần đăng nhập')
    : 'Slice-1 đang khóa ghi';
  const detail = mutationReady
    ? 'Runtime đã công bố businessMutationEnabled=true. UI vẫn tuân theo auth, permission, version và conflict guard của Service.'
    : 'Runtime chưa công bố khả năng mutation Slice-1; giao diện chỉ hiển thị trạng thái và không giả lập thành công.';

  return Object.freeze({ phase, label, runtime, mutationReady, authenticated, conflictCount, pendingGoogle, detail });
}

export function describeSliceView(view) {
  if (view === 'people') {
    return Object.freeze({
      eyebrow: 'Slice-1 · Identity',
      title: 'Nhân sự & MNV',
      description: 'Cùng một contract cho Online và LAN: hồ sơ nhân sự, trạng thái và định danh MNV.',
      commands: ['Tạo nhân sự', 'Cập nhật hồ sơ', 'Đổi trạng thái', 'Gán MNV'],
      ownerGate: 'Thay ảnh chân dung · chờ quyết định Owner'
    });
  }
  if (view === 'attendance') {
    return Object.freeze({
      eyebrow: 'Slice-1 · Attendance',
      title: 'Ra / Vào',
      description: 'Hiển thị cùng trạng thái Cloud/LAN cho Vào, Ra và điều chỉnh hiện diện. Công nhật chưa thuộc Slice-1 hiện hành.',
      commands: ['Ghi nhận Vào', 'Ghi nhận Ra', 'Điều chỉnh hiện diện'],
      ownerGate: null
    });
  }
  return Object.freeze({
    eyebrow: 'Nghiệp vụ đã xác thực',
    title: 'Slice-1 · Nhân sự & Ra/Vào',
    description: 'Web Online và Web LAN dùng chung màn hình, cùng contract và cùng cách hiển thị commit, Google output, xung đột.',
    commands: [],
    ownerGate: null
  });
}

function cleanOptional(value) {
  const text = String(value ?? '').trim();
  return text || null;
}

export function buildEmployeeCreateInput(values = {}, randomUUID = () => crypto.randomUUID()) {
  const fullName = String(values.fullName || '').trim();
  if (!fullName) throw new Error('EMPLOYEE_FULL_NAME_REQUIRED');
  const employeeId = String(randomUUID()).trim();
  if (!employeeId) throw new Error('EMPLOYEE_ID_GENERATION_FAILED');
  const status = String(values.status || 'ACTIVE').trim().toUpperCase();
  if (!['ACTIVE', 'INACTIVE', 'LEFT', 'ARCHIVED'].includes(status)) throw new Error('EMPLOYEE_STATUS_INVALID');

  const payload = { employeeId, fullName, status };
  for (const field of ['phone', 'mainPosition', 'vendor', 'department', 'site', 'warehouse', 'startDate', 'permanentLeaveDate', 'note']) {
    const normalized = cleanOptional(values[field]);
    if (normalized !== null) payload[field] = normalized;
  }
  return Object.freeze({
    commandCode: 'EMPLOYEE_CREATE',
    entityId: employeeId,
    expectedEntityVersion: null,
    payload: Object.freeze(payload)
  });
}

export function employeeCreateResultText(result) {
  if (!result?.ok) return 'Không có kết quả tạo nhân sự hợp lệ.';
  const commit = {
    CLOUD_COMMITTED: 'Đã chốt Cloud',
    LAN_ACCEPTED_PENDING_SYNC: 'LAN đã nhận, chờ Cloud',
    LAN_RECONCILED_CLOUD_COMMITTED: 'LAN đã đồng bộ Cloud',
    QUEUED_CLIENT_LOCAL: 'Đang chờ trên thiết bị',
    SYNC_CONFLICT: 'Có xung đột đồng bộ'
  }[result.commitStatus] || result.commitStatus || 'Chưa rõ commit';
  const google = {
    NOT_REQUIRED: 'Google: không yêu cầu',
    PENDING: 'Google: đang chờ',
    COMPLETED: 'Google: hoàn tất',
    FAILED_RETRYABLE: 'Google: sẽ thử lại',
    REVIEW_REQUIRED: 'Google: cần rà soát'
  }[result.googleOutputStatus] || `Google: ${result.googleOutputStatus || 'chưa rõ'}`;
  return `${commit} · ${google}`;
}

function employeeCreateErrorText(error) {
  const code = String(error?.code || error?.payload?.error?.code || error?.message || '');
  const known = {
    EMPLOYEE_FULL_NAME_REQUIRED: 'Cần nhập họ và tên nhân sự.',
    EMPLOYEE_STATUS_INVALID: 'Trạng thái nhân sự không hợp lệ.',
    AUTH_FAILED: 'Phiên đăng nhập không hợp lệ.',
    INVALID_CREDENTIALS: 'Phiên đăng nhập không hợp lệ.',
    PERMISSION_DENIED: 'Tài khoản không có quyền tạo nhân sự trong phạm vi hiện tại.',
    FORBIDDEN: 'Tài khoản không có quyền tạo nhân sự trong phạm vi hiện tại.',
    VERSION_CONFLICT: 'Nhân sự đã tồn tại hoặc dữ liệu vừa thay đổi.',
    LAN_SIGNER_REQUIRED: 'LAN Web chưa có bằng chứng ký của thiết bị đã ghép đôi.',
    LAN_SIGNER_INVALID_PROOF: 'Bộ ký LAN không cung cấp đủ bằng chứng thiết bị.'
  };
  return known[code] || error?.message || 'Không thể tạo nhân sự.';
}

function ensureSurface() {
  let surface = document.getElementById('slice1Surface');
  if (surface) return surface;
  surface = document.createElement('section');
  surface.id = 'slice1Surface';
  surface.className = 'slice1-surface';
  surface.hidden = true;
  surface.setAttribute('aria-live', 'polite');
  document.querySelector('main')?.append(surface);
  return surface;
}

function authenticatedNow() {
  return !document.body.classList.contains('auth-locked');
}

function employeeCreatePanel(state) {
  const disabled = !(state.authenticated && state.mutationReady && state.runtime !== 'UNKNOWN');
  const resultText = lastEmployeeCreateResult ? employeeCreateResultText(lastEmployeeCreateResult) : '';
  return `
    <article class="slice1-card slice1-form-card">
      <div class="slice1-form-heading">
        <div><p class="eyebrow">Thao tác đã mở</p><h3>Tạo nhân sự</h3><p>Tạo hồ sơ mới bằng ID kỹ thuật tự sinh; người dùng không phải nhập entity version.</p></div>
        <span class="slice1-form-gate ${disabled ? 'blocked' : 'ready'}">${disabled ? 'Chưa đủ điều kiện ghi' : 'Sẵn sàng ghi'}</span>
      </div>
      <form id="employeeCreateForm" class="slice1-form">
        <label>Họ và tên<input name="fullName" maxlength="240" required ${disabled ? 'disabled' : ''}></label>
        <label>Số điện thoại<input name="phone" maxlength="1000" inputmode="tel" ${disabled ? 'disabled' : ''}></label>
        <label>Trạng thái<select name="status" ${disabled ? 'disabled' : ''}><option value="ACTIVE">Hoạt động</option><option value="INACTIVE">Tạm dừng</option><option value="LEFT">Đã nghỉ</option><option value="ARCHIVED">Lưu trữ</option></select></label>
        <label>Vị trí chính<input name="mainPosition" maxlength="1000" ${disabled ? 'disabled' : ''}></label>
        <label>Nhà cung cấp<input name="vendor" maxlength="1000" ${disabled ? 'disabled' : ''}></label>
        <label>Bộ phận<input name="department" maxlength="1000" ${disabled ? 'disabled' : ''}></label>
        <label>Site<input name="site" maxlength="1000" ${disabled ? 'disabled' : ''}></label>
        <label>Kho<input name="warehouse" maxlength="1000" ${disabled ? 'disabled' : ''}></label>
        <label>Ngày bắt đầu làm việc<input name="startDate" type="date" ${disabled ? 'disabled' : ''}></label>
        <label class="slice1-form-wide">Ghi chú<textarea name="note" maxlength="4000" rows="3" ${disabled ? 'disabled' : ''}></textarea></label>
        <div class="slice1-form-actions slice1-form-wide">
          <button id="employeeCreateSubmit" class="primary-button" type="submit" ${disabled ? 'disabled' : ''}>Tạo nhân sự</button>
          <span id="employeeCreateMessage" class="slice1-form-message" aria-live="polite">${resultText}</span>
        </div>
      </form>
    </article>`;
}

function renderSurface(view, state) {
  const surface = ensureSurface();
  const model = describeSliceView(view);
  const pendingGoogle = state.pendingGoogle === null ? 'Chưa công bố' : String(state.pendingGoogle);
  const conflictLabel = state.conflictCount > 0 ? `${state.conflictCount} cần xử lý` : 'Không tồn';
  const noteClass = state.phase === 'error' ? 'error' : (state.conflictCount > 0 ? 'warn' : '');
  const commandTags = model.commands.map(item => `<span>${item}</span>`).join('');
  const ownerGate = model.ownerGate ? `<span class="owner-gate">${model.ownerGate}</span>` : '';

  const overviewCards = view === 'business' ? `
    <div class="slice1-grid">
      <article class="slice1-card">
        <p class="eyebrow">Identity</p><h3>Nhân sự & MNV</h3>
        <p>Hồ sơ, trạng thái và MNV theo cùng command contract ở Cloud/LAN.</p>
        <button class="secondary-button" type="button" data-slice-jump="people">Mở Nhân sự</button>
      </article>
      <article class="slice1-card">
        <p class="eyebrow">Attendance</p><h3>Ra / Vào</h3>
        <p>Hiện diện Vào/Ra/điều chỉnh với commit state và conflict state tách bạch.</p>
        <button class="secondary-button" type="button" data-slice-jump="attendance">Mở Ra/Vào</button>
      </article>
    </div>` : `
    <article class="slice1-card">
      <p class="eyebrow">Command contract</p><h3>Phạm vi đang triển khai</h3>
      <div class="slice1-command-list">${commandTags}${ownerGate}</div>
      <p style="margin-top:12px">Các thao tác ghi chỉ được mở khi runtime, auth, permission và entity-version guard đều có bằng chứng hợp lệ.</p>
    </article>`;

  surface.innerHTML = `
    <article class="slice1-card">
      <div class="slice1-heading">
        <div><p class="eyebrow">${model.eyebrow}</p><h2>${model.title}</h2><p>${model.description}</p></div>
        <span class="slice1-runtime ${state.phase}" id="sliceRuntimeState">${state.label}</span>
      </div>
      <div class="slice1-state-grid">
        <div class="slice1-state"><small>Runtime</small><strong>${state.runtime}</strong></div>
        <div class="slice1-state"><small>Google output đang chờ</small><strong>${pendingGoogle}</strong></div>
        <div class="slice1-state"><small>Xung đột</small><strong>${conflictLabel}</strong></div>
      </div>
    </article>
    ${overviewCards}
    ${view === 'people' ? employeeCreatePanel(state) : ''}
    <div class="slice1-note ${noteClass}" id="sliceStateDetail">${state.detail}${state.conflictCount > 0 ? ' Có xung đột chưa xử lý; UI không được diễn giải thành trạng thái xanh đồng bộ.' : ''}</div>`;

  surface.querySelectorAll('[data-slice-jump]').forEach(button => {
    button.addEventListener('click', () => {
      const target = button.dataset.sliceJump;
      document.querySelector(`[data-view="${target}"]`)?.click();
    });
  });

  if (view === 'people') bindEmployeeCreateForm(surface, state);
}

function formValues(form) {
  const data = new FormData(form);
  return Object.fromEntries(data.entries());
}

function setEmployeeCreateMessage(surface, text, tone = '') {
  const message = surface.querySelector('#employeeCreateMessage');
  if (!message) return;
  message.textContent = text;
  message.className = `slice1-form-message${tone ? ` ${tone}` : ''}`;
}

function bindEmployeeCreateForm(surface, state) {
  const form = surface.querySelector('#employeeCreateForm');
  if (!form) return;
  form.addEventListener('submit', async event => {
    event.preventDefault();
    if (!(state.authenticated && state.mutationReady) || state.runtime === 'UNKNOWN') {
      setEmployeeCreateMessage(surface, 'Chưa đủ điều kiện xác thực/runtime để gửi thao tác.', 'error');
      return;
    }

    const submit = surface.querySelector('#employeeCreateSubmit');
    submit.disabled = true;
    setEmployeeCreateMessage(surface, 'Đang tạo nhân sự…');
    try {
      sliceAuthClient.configure({ runtime: state.runtime, capabilities: cachedStatus?.capabilities || null });
      const input = buildEmployeeCreateInput(formValues(form));
      const result = await sliceBusinessClient.submitCommand(input);
      lastEmployeeCreateResult = result;
      form.reset();
      setEmployeeCreateMessage(surface, employeeCreateResultText(result), 'ok');
      const refreshed = await readRuntimeState(true);
      if (activeView === 'people') renderSurface('people', refreshed);
    } catch (error) {
      setEmployeeCreateMessage(surface, employeeCreateErrorText(error), 'error');
      submit.disabled = false;
    }
  });
}

let cachedStatus = null;
let cachedAt = 0;
let activeView = 'dashboard';

async function readRuntimeState(force = false) {
  const now = Date.now();
  if (!force && cachedStatus && now - cachedAt < STATUS_CACHE_MS) {
    return deriveSliceSurfaceState({ ...cachedStatus, authenticated: authenticatedNow() });
  }

  const [capabilitiesResult, syncResult] = await Promise.allSettled([
    fetch('/api/v1/capabilities', { cache: 'no-store', credentials: 'same-origin' }).then(async response => {
      if (!response.ok) throw new Error(`CAPABILITIES_HTTP_${response.status}`);
      return response.json();
    }),
    fetch('/api/v1/sync/status', { cache: 'no-store', credentials: 'same-origin' }).then(async response => {
      if (!response.ok) throw new Error(`SYNC_HTTP_${response.status}`);
      return response.json();
    })
  ]);

  if (capabilitiesResult.status === 'rejected') {
    cachedStatus = { loadError: capabilitiesResult.reason };
  } else {
    cachedStatus = {
      capabilities: capabilitiesResult.value,
      sync: syncResult.status === 'fulfilled' ? syncResult.value : null
    };
  }
  cachedAt = now;
  return deriveSliceSurfaceState({ ...cachedStatus, authenticated: authenticatedNow() });
}

async function showView(view) {
  activeView = view;
  const isSlice = SLICE_VIEWS.has(view);
  document.body.classList.toggle('slice1-view', isSlice);
  const surface = ensureSurface();
  surface.hidden = !isSlice;
  if (!isSlice) return;

  renderSurface(view, deriveSliceSurfaceState({ authenticated: authenticatedNow() }));
  const state = await readRuntimeState(false);
  if (activeView === view) renderSurface(view, state);
}

export function initSlice1Ui() {
  ensureSurface();
  document.querySelectorAll('[data-view]').forEach(button => {
    button.addEventListener('click', () => showView(button.dataset.view));
  });
  document.querySelectorAll('[data-view-jump]').forEach(button => {
    button.addEventListener('click', () => showView(button.dataset.viewJump));
  });
  showView('dashboard');
}

if (typeof document !== 'undefined') initSlice1Ui();
