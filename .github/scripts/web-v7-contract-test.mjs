import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const [html, css, js] = await Promise.all([
  readFile('web/index.html', 'utf8'),
  readFile('web/styles.css', 'utf8'),
  readFile('web/app.js', 'utf8')
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

for (const commitState of [
  'CLOUD_COMMITTED', 'LAN_ACCEPTED_PENDING_SYNC', 'LAN_RECONCILED_CLOUD_COMMITTED',
  'QUEUED_CLIENT_LOCAL', 'SYNC_CONFLICT'
]) {
  assert.ok(js.includes(commitState), `WEB_COMMIT_STATE_MISSING:${commitState}`);
}

for (const endpoint of ['/api/v1/meta', '/api/v1/capabilities', '/api/v1/sync/status', '/api/v1/auth/me']) {
  assert.ok(js.includes(endpoint), `WEB_RUNTIME_ENDPOINT_MISSING:${endpoint}`);
}

const externalAssetPatterns = [
  /<(?:script|link|img)[^>]+(?:src|href)=["']https?:\/\//i,
  /@import\s+(?:url\()?\s*["']?https?:\/\//i,
  /url\(\s*["']?https?:\/\//i
];
for (const pattern of externalAssetPatterns) {
  assert.equal(pattern.test(`${html}\n${css}`), false, 'WEB_LAN_CRITICAL_ASSET_EXTERNAL');
}

assert.equal(/DNSHE/i.test(`${html}\n${css}\n${js}`), false, 'WEB_DNSHE_BRANDING_COPIED');
assert.equal(/lang(?:uage)?\s*(?:selector|switch)|English|中文|简体|繁體/i.test(`${html}\n${js}`), false, 'WEB_MULTILINGUAL_UI_NOT_DEFERRED');
assert.equal(/docs\.google\.com|drive\.google\.com|sheets\.google/i.test(js), false, 'WEB_DIRECT_GOOGLE_BYPASS');

assert.match(css, /--navy:/, 'WEB_DESIGN_TOKEN_NAVY_MISSING');
assert.match(css, /--blue:/, 'WEB_DESIGN_TOKEN_BLUE_MISSING');
assert.match(css, /--panel:/, 'WEB_DESIGN_TOKEN_PANEL_MISSING');
assert.match(css, /@media\s*\(max-width:/, 'WEB_RESPONSIVE_BREAKPOINT_MISSING');

console.log('WEB_V7_CONTRACT_PASS language=vi offlineAssets=PASS sharedRuntimeStates=PASS responsive=PASS noDnsheBranding=PASS');
