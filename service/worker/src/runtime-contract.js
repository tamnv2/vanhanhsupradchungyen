export const DOMAIN_CONTRACT_VERSION = 'VHDCHY_DOMAIN_V1';

export const COMMIT_STATUS = Object.freeze({
  CLOUD_COMMITTED: 'CLOUD_COMMITTED',
  LAN_ACCEPTED_PENDING_SYNC: 'LAN_ACCEPTED_PENDING_SYNC',
  LAN_RECONCILED_CLOUD_COMMITTED: 'LAN_RECONCILED_CLOUD_COMMITTED',
  QUEUED_CLIENT_LOCAL: 'QUEUED_CLIENT_LOCAL',
  SYNC_CONFLICT: 'SYNC_CONFLICT'
});

export const GOOGLE_OUTPUT_STATUS = Object.freeze({
  NOT_REQUIRED: 'NOT_REQUIRED',
  PENDING: 'PENDING',
  COMPLETED: 'COMPLETED',
  FAILED_RETRYABLE: 'FAILED_RETRYABLE',
  REVIEW_REQUIRED: 'REVIEW_REQUIRED'
});

export const MACHINE_ERROR = Object.freeze({
  AUTH_REQUIRED: 'AUTH_REQUIRED',
  AUTH_INVALID: 'AUTH_INVALID',
  AUTH_EXPIRED: 'AUTH_EXPIRED',
  AUTH_REVOKED: 'AUTH_REVOKED',
  PERMISSION_DENIED: 'PERMISSION_DENIED',
  ROOT_MFA_REQUIRED: 'ROOT_MFA_REQUIRED',
  INVALID_INPUT: 'INVALID_INPUT',
  NOT_FOUND: 'NOT_FOUND',
  ENTITY_VERSION_CONFLICT: 'ENTITY_VERSION_CONFLICT',
  IDEMPOTENCY_PAYLOAD_CONFLICT: 'IDEMPOTENCY_PAYLOAD_CONFLICT',
  DEVICE_SEQUENCE_COLLISION: 'DEVICE_SEQUENCE_COLLISION',
  RESOURCE_NOT_AVAILABLE: 'RESOURCE_NOT_AVAILABLE',
  USED_LOCKED: 'USED_LOCKED',
  INVALID_PACK_MAPPING: 'INVALID_PACK_MAPPING',
  MAIN_SESSION_EXISTS: 'MAIN_SESSION_EXISTS',
  LABOR_ALREADY_OPEN: 'LABOR_ALREADY_OPEN',
  SYNC_CONFLICT: 'SYNC_CONFLICT',
  RUNTIME_DEPENDENCY_UNAVAILABLE: 'RUNTIME_DEPENDENCY_UNAVAILABLE',
  SCHEMA_INCOMPATIBLE: 'SCHEMA_INCOMPATIBLE'
});

const REQUIRED_MUTATION_FIELDS = Object.freeze([
  'requestId',
  'idempotencyKey',
  'environment',
  'clusterId',
  'commandCode',
  'entityType',
  'entityId',
  'payloadHash'
]);

function nonBlank(value) {
  return typeof value === 'string' && value.trim().length > 0;
}

export function validateMutationIdentity(identity) {
  if (!identity || typeof identity !== 'object') {
    return { ok: false, code: MACHINE_ERROR.INVALID_INPUT, missing: [...REQUIRED_MUTATION_FIELDS] };
  }

  const missing = REQUIRED_MUTATION_FIELDS.filter((field) => !nonBlank(identity[field]));
  if (missing.length > 0) {
    return { ok: false, code: MACHINE_ERROR.INVALID_INPUT, missing };
  }

  if (identity.deviceSeq != null && (!Number.isSafeInteger(identity.deviceSeq) || identity.deviceSeq <= 0)) {
    return { ok: false, code: MACHINE_ERROR.INVALID_INPUT, invalid: ['deviceSeq'] };
  }

  if (identity.expectedEntityVersion != null && (!Number.isSafeInteger(identity.expectedEntityVersion) || identity.expectedEntityVersion < 1)) {
    return { ok: false, code: MACHINE_ERROR.INVALID_INPUT, invalid: ['expectedEntityVersion'] };
  }

  if (identity.deviceSeq != null && !nonBlank(identity.deviceId)) {
    return { ok: false, code: MACHINE_ERROR.INVALID_INPUT, missing: ['deviceId'] };
  }

  return { ok: true };
}

export function validateLanAcceptanceIdentity(identity) {
  const base = validateMutationIdentity(identity);
  if (!base.ok) return base;

  const missing = ['edgeInstanceId', 'edgeEpoch', 'authoritySnapshotVersion']
    .filter((field) => !nonBlank(identity[field]));
  if (missing.length > 0) {
    return { ok: false, code: MACHINE_ERROR.INVALID_INPUT, missing };
  }

  return { ok: true };
}

export function successEnvelope({ requestId, runtime, commitStatus, googleOutputStatus = GOOGLE_OUTPUT_STATUS.NOT_REQUIRED, data = {} }) {
  if (!nonBlank(requestId)) throw new TypeError('requestId is required');
  if (!['CLOUD', 'LAN'].includes(runtime)) throw new TypeError('runtime must be CLOUD or LAN');
  if (!Object.values(COMMIT_STATUS).includes(commitStatus)) throw new TypeError('invalid commitStatus');
  if (!Object.values(GOOGLE_OUTPUT_STATUS).includes(googleOutputStatus)) throw new TypeError('invalid googleOutputStatus');

  return {
    ok: true,
    requestId,
    runtime,
    commitStatus,
    googleOutputStatus,
    data
  };
}

export function errorEnvelope({ requestId, runtime, code, message, details }) {
  if (!nonBlank(requestId)) throw new TypeError('requestId is required');
  if (!['CLOUD', 'LAN'].includes(runtime)) throw new TypeError('runtime must be CLOUD or LAN');
  if (!Object.values(MACHINE_ERROR).includes(code)) throw new TypeError('invalid machine error code');

  const error = { code, message: nonBlank(message) ? message : code };
  if (details && typeof details === 'object') error.details = details;

  return {
    ok: false,
    requestId,
    runtime,
    error
  };
}
