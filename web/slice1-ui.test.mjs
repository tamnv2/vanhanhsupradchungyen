import assert from 'node:assert/strict';
import { deriveSliceSurfaceState, describeSliceView } from './slice1-ui.js';

const loading = deriveSliceSurfaceState({ authenticated: false });
assert.equal(loading.phase, 'loading');
assert.equal(loading.mutationReady, false);

const blocked = deriveSliceSurfaceState({
  authenticated: true,
  capabilities: { runtime: 'CLOUD', businessMutationEnabled: false },
  sync: { conflictCount: 0, pendingGoogleWork: 3 }
});
assert.equal(blocked.phase, 'blocked');
assert.equal(blocked.runtime, 'CLOUD');
assert.equal(blocked.pendingGoogle, 3);

const lanReady = deriveSliceSurfaceState({
  authenticated: true,
  capabilities: { runtime: 'LAN', businessMutationEnabled: true },
  sync: { conflictCount: 2, pendingGoogleWork: 1 }
});
assert.equal(lanReady.phase, 'ready');
assert.equal(lanReady.mutationReady, true);
assert.equal(lanReady.conflictCount, 2);
assert.match(lanReady.label, /sẵn sàng/i);

const unauthenticatedReadyRoute = deriveSliceSurfaceState({
  authenticated: false,
  capabilities: { runtime: 'LAN', businessMutationEnabled: true },
  sync: { conflictCount: 0 }
});
assert.equal(unauthenticatedReadyRoute.phase, 'ready');
assert.match(unauthenticatedReadyRoute.label, /cần đăng nhập/i);

const failed = deriveSliceSurfaceState({ loadError: new Error('network') });
assert.equal(failed.phase, 'error');
assert.equal(failed.mutationReady, false);

const people = describeSliceView('people');
assert.equal(people.title, 'Nhân sự & MNV');
assert.ok(people.commands.includes('Gán MNV'));
assert.match(people.ownerGate, /Owner/);

const attendance = describeSliceView('attendance');
assert.equal(attendance.title, 'Ra / Vào');
assert.ok(attendance.commands.includes('Ghi nhận Ra'));
assert.match(attendance.description, /Công nhật chưa thuộc Slice-1/);

console.log('WEB_SLICE1_SURFACE_PASS onlineLanParity=PASS loading=PASS blocked=PASS conflict=PASS ownerGate=PASS noFakeMutation=PASS');
