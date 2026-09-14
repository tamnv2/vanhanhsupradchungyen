import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const [html, css, authCss, js, authJs] = await Promise.all([
  readFile('web/index.html', 'utf8'),
  readFile('web/styles.css', 'utf8'),
  readFile('web/auth.css', 'utf8'),
  readFile('web/app.js', 'utf8'),
  readFile('web/auth.js', 'utf8')
]);

assert.match(html, /<html\s+lang="vi">/i, 'WEB_LANGUAGE_NOT_VI');
assert.match(html, /VẬN HÀNH DC HƯNG YÊN/, 'WEB_PRODUCT_BRAND_MISSING');

for (const label of [
  'Tổng quan', 'Nghiệp vụ', 'Nhân sự', 'Ra/Vào & Công nhật', 'Tài nguyên',
  'Biên bản', 'Lịch sử', 'Đồng bộ', 'Quản trị', 'Cài đặt'
]) {
  assert.ok(html.includes(label), `WEB_NAV_LABEL_MISSING:${label}`);
}

for (const id of [
  'environmentBadge', 'runtimeStatus', 'serviceHeadline', 'cloudSyncHeadline',
  'googleHeadline', 'conflictHeadline', 'runtimeDetail', 'cloudSyncDetail',
  'googleDetail', 'conflictDetail', 'message'
]) {
  assert.ok(html.includes(`id="${id}"`), `WEB_STATUS_SURFACE_MISSING:${id}`);
}

for (const id of [
  'authGate', 'loginForm', 'usernameInput', 'passwordInput', 'loginButton',
  'recoveryButton', 'authMessage', 'passwordChangePanel', 'passwordChangeForm',
  'newPasswordInput', 'confirmPasswordInput', 'passwordChangeButton', 'logoutButton'
]) {
  assert.ok(html.includes(`id="${id}"`), `WEB_AUTH_SURFACE_MISSING:${id}`);
}

assert.ok(html.includes('Lấy lại mật khẩu'), 'WEB_V6_RECOVERY_ENTRY_MISSING');
assert.ok(html.includes('Thiết lập mật khẩu mới'), 'WEB_MUST_CHANGE_PASSWORD_SURFACE_MISSING');
assert.ok(html.includes('Rời Web'), 'WEB_LOCAL_SESSION_EXIT_LABEL_MISSING');
assert.equal(/>Đăng xuất</.test(html), false, 'WEB_SERVER_LOGOUT_OVERCLAIM');
assert.ok(html.includes('/auth.css'), 'WEB_AUTH_STYLE_NOT_LOCAL');
assert.ok(js.includes("from './auth.js'"), 'WEB_AUTH_CLIENT_NOT_WIRED');

for (const commitState of [
  'CLOUD_COMMITTED', 'LAN_ACCEPTED_PENDING_SYNC', 'LAN_RECONCILED_CLOUD_COMMITTED',
  'QUEUED_CLIENT_LOCAL', 'SYNC_CONFLICT'
]) {
  assert.ok(js.includes(commitState), `WEB_COMMIT_STATE_MISSING:${commitState}`);
}

for (const endpoint of [
  '/api/v1/meta', '/api/v1/capabilities', '/api/v1/sync/status',
  '/api/v1/auth/login', '/api/v1/auth/me', '/api/v1/auth/change-password'
]) {
  assert.ok(`${js}\n${authJs}`.includes(endpoint), `WEB_RUNTIME_ENDPOINT_MISSING:${endpoint}`);
}

assert.ok(authJs.includes('sessionStorage'), 'WEB_SESSION_TOKEN_NOT_TAB_SCOPED');
assert.equal(authJs.includes('localStorage'), false, 'WEB_SESSION_TOKEN_PERSISTED_TO_LOCAL_STORAGE');
assert.ok(authJs.includes("headers.set('Authorization', `Bearer ${authToken}`)"), 'WEB_BEARER_SESSION_HEADER_MISSING');
assert.ok(authJs.includes("runtime === 'LAN'"), 'WEB_LAN_AUTH_BRANCH_MISSING');
assert.ok(authJs.includes('LAN_SIGNER_REQUIRED'), 'WEB_LAN_UNSIGNED_LOGIN_NOT_FAIL_CLOSED');
assert.ok(authJs.includes('LAN_SIGNER_INVALID_PROOF'), 'WEB_LAN_SIGNER_PROOF_VALIDATION_MISSING');
assert.ok(authJs.includes('validSessionEvidence'), 'WEB_LAN_SESSION_EXPIRY_GATE_MISSING');
for (const header of [
  'X-VHDCHY-Device-Id', 'X-VHDCHY-Security-Epoch', 'X-VHDCHY-Timestamp-Ms',
  'X-VHDCHY-Nonce', 'X-VHDCHY-Signature'
]) {
  assert.ok(authJs.includes(header), `WEB_LAN_PROOF_HEADER_MISSING:${header}`);
}
assert.ok(js.includes('ROOT_EMAIL_OTP_REQUIRED'), 'WEB_ROOT_OTP_FAIL_CLOSED_MESSAGE_MISSING');
assert.ok(js.includes('mustChangePassword'), 'WEB_MUST_CHANGE_PASSWORD_GATE_MISSING');
assert.ok(js.includes('public email-OTP delivery route'), 'WEB_RECOVERY_DEPENDENCY_DISCLOSURE_MISSING');
assert.ok(js.includes('Server-side logout chưa được công bố'), 'WEB_LOCAL_EXIT_DISCLOSURE_MISSING');

const externalAssetPatterns = [
  /<(?:script|link|img)[^>]+(?:src|href)=["']https?:\/\//i,
  /@import\s+(?:url\()?\s*["']?https?:\/\//i,
  /url\(\s*["']?https?:\/\//i
];
for (const pattern of externalAssetPatterns) {
  assert.equal(pattern.test(`${html}\n${css}\n${authCss}`), false, 'WEB_LAN_CRITICAL_ASSET_EXTERNAL');
}

assert.equal(/DNSHE/i.test(`${html}\n${css}\n${authCss}\n${js}\n${authJs}`), false, 'WEB_DNSHE_BRANDING_COPIED');
assert.equal(/lang(?:uage)?\s*(?:selector|switch)|English|中文|简体|繁體/i.test(`${html}\n${js}`), false, 'WEB_MULTILINGUAL_UI_NOT_DEFERRED');
assert.equal(/docs\.google\.com|drive\.google\.com|sheets\.google/i.test(`${js}\n${authJs}`), false, 'WEB_DIRECT_GOOGLE_BYPASS');

assert.match(css, /--navy:/, 'WEB_DESIGN_TOKEN_NAVY_MISSING');
assert.match(css, /--blue:/, 'WEB_DESIGN_TOKEN_BLUE_MISSING');
assert.match(css, /--panel:/, 'WEB_DESIGN_TOKEN_PANEL_MISSING');
assert.match(`${css}\n${authCss}`, /@media\s*\(max-width:/, 'WEB_RESPONSIVE_BREAKPOINT_MISSING');

await import('../../web/auth-client.test.mjs');

console.log('WEB_V7_CONTRACT_PASS language=vi authGate=PASS cloudBearer=PASS lanSignedFailClosed=PASS lanExpiry=PASS mustChangePassword=PASS recoveryFailClosed=PASS localExit=PASS offlineAssets=PASS responsive=PASS noDnsheBranding=PASS');
