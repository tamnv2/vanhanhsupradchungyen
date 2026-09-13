import test from 'node:test';
import assert from 'node:assert/strict';
import { evaluateSessionRecord } from '../src/session.js';
import {
  PROJECTION_MAX_ATTEMPTS,
  buildProjectionEnvelope,
  classifyProjectionFailure,
  retryDelayMs
} from '../src/projection.js';

test('active session with matching device epoch is accepted', () => {
  const now = Date.parse('2026-09-13T10:00:00Z');
  const result = evaluateSessionRecord({
    auth_session_id: 'S1',
    user_id: 'U1',
    username: 'user1',
    employee_id: 'E1',
    display_name: 'User 1',
    session_status: 'ACTIVE',
    user_status: 'ACTIVE',
    security_level: 'NORMAL',
    expires_at: '2026-09-13T12:00:00Z',
    device_id: 'D1',
    device_status: 'ACTIVE',
    session_security_epoch: 3,
    device_security_epoch: 3,
    metadata_json: '{"mustChangePassword":false}'
  }, now);
  assert.equal(result.ok, true);
  assert.equal(result.principal.userId, 'U1');
});

test('expired or epoch-mismatched session is rejected', () => {
  const now = Date.parse('2026-09-13T13:00:00Z');
  assert.equal(evaluateSessionRecord({
    session_status: 'ACTIVE',
    user_status: 'ACTIVE',
    expires_at: '2026-09-13T12:00:00Z'
  }, now).code, 'SESSION_EXPIRED');

  assert.equal(evaluateSessionRecord({
    session_status: 'ACTIVE',
    user_status: 'ACTIVE',
    expires_at: '2026-09-13T14:00:00Z',
    device_id: 'D1',
    device_status: 'ACTIVE',
    session_security_epoch: 2,
    device_security_epoch: 3
  }, now).code, 'SECURITY_EPOCH_MISMATCH');
});

test('projection envelope preserves event identity and structured payload', () => {
  const envelope = buildProjectionEnvelope({
    outbox_id: 9,
    event_id: 'EV9',
    projection_target: 'GOOGLE_SHEETS',
    payload_json: '{"kind":"attendance"}'
  }, 'beta');
  assert.equal(envelope.protocol, 'VHDCHY_PROJECTION_V1');
  assert.equal(envelope.environment, 'BETA');
  assert.equal(envelope.outboxId, 9);
  assert.equal(envelope.eventId, 'EV9');
  assert.equal(envelope.payload.kind, 'attendance');
});

test('projection retry backs off and eventually dead-letters', () => {
  assert.equal(retryDelayMs(1), 30000);
  assert.ok(retryDelayMs(7) <= 3600000);
  const beforeDead = classifyProjectionFailure(PROJECTION_MAX_ATTEMPTS - 2, 0);
  assert.equal(beforeDead.status, 'PENDING');
  const dead = classifyProjectionFailure(PROJECTION_MAX_ATTEMPTS - 1, 0);
  assert.equal(dead.status, 'DEAD');
  assert.equal(dead.nextAttemptAt, null);
});
