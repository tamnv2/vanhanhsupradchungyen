import { WebAuthError } from './auth.js';

export const BUSINESS_COMMAND_PATH = '/api/v1/data/commands';

const SUPPORTED_COMMANDS = new Set([
  'EMPLOYEE_CREATE',
  'EMPLOYEE_UPDATE',
  'EMPLOYEE_STATUS_CHANGE',
  'EMPLOYEE_CODE_ASSIGN',
  'ATTENDANCE_IN',
  'ATTENDANCE_OUT',
  'ATTENDANCE_CORRECT'
]);

const REQUIRED_LAN_PROOF_HEADERS = Object.freeze([
  'X-VHDCHY-Device-Id',
  'X-VHDCHY-Security-Epoch',
  'X-VHDCHY-Timestamp-Ms',
  'X-VHDCHY-Nonce',
  'X-VHDCHY-Signature'
]);

function requiredText(value, code) {
  const text = String(value || '').trim();
  if (!text) throw new WebAuthError(code, code);
  return text;
}

function normalizePayload(value) {
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    throw new WebAuthError('BUSINESS_PAYLOAD_INVALID', 'Payload nghiệp vụ phải là JSON object.');
  }
  return value;
}

function normalizeExpectedVersion(value) {
  if (value === null || value === undefined) return null;
  const parsed = Number(value);
  if (!Number.isSafeInteger(parsed) || parsed < 1) {
    throw new WebAuthError('EXPECTED_ENTITY_VERSION_INVALID', 'Entity version phải là số nguyên dương.');
  }
  return parsed;
}

function normalizeDeviceSeq(value) {
  if (value === null || value === undefined) return undefined;
  const parsed = Number(value);
  if (!Number.isSafeInteger(parsed) || parsed < 1) {
    throw new WebAuthError('DEVICE_SEQ_INVALID', 'Device sequence phải là số nguyên dương.');
  }
  return parsed;
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

export function createBusinessClient(authClient, options = {}) {
  if (!authClient || typeof authClient.request !== 'function') throw new Error('AUTH_CLIENT_REQUIRED');
  const getLanSigner = options.getLanSigner || (() => globalThis.VHDCHY_LAN_SIGNER);
  const randomUUID = options.randomUUID || (() => crypto.randomUUID());

  async function submitCommand(input = {}) {
    const commandCode = requiredText(input.commandCode, 'COMMAND_CODE_REQUIRED').toUpperCase();
    if (commandCode === 'EMPLOYEE_PORTRAIT_REPLACE') {
      throw new WebAuthError(
        'EMPLOYEE_PORTRAIT_REPLACE_OWNER_DECISION_REQUIRED',
        'Thay ảnh chân dung vẫn đang ở cổng quyết định của Owner.'
      );
    }
    if (!SUPPORTED_COMMANDS.has(commandCode)) {
      throw new WebAuthError('COMMAND_NOT_SUPPORTED', `Command chưa nằm trong Slice-1 Web hiện hành: ${commandCode}`);
    }

    const requestId = requiredText(input.requestId || randomUUID(), 'REQUEST_ID_REQUIRED');
    const idempotencyKey = requiredText(input.idempotencyKey || randomUUID(), 'IDEMPOTENCY_KEY_REQUIRED');
    const entityId = requiredText(input.entityId, 'ENTITY_ID_REQUIRED');
    const payload = normalizePayload(input.payload);
    const expectedEntityVersion = normalizeExpectedVersion(input.expectedEntityVersion);
    const deviceSeq = normalizeDeviceSeq(input.deviceSeq);

    const envelope = {
      requestId,
      idempotencyKey,
      commandCode,
      entityId,
      expectedEntityVersion,
      payload
    };
    if (deviceSeq !== undefined) envelope.deviceSeq = deviceSeq;

    const rawBody = JSON.stringify(envelope);
    const headers = new Headers({
      'Content-Type': 'application/json',
      'X-Request-Id': requestId
    });

    if (String(authClient.runtime || '').toUpperCase() === 'LAN') {
      const signer = getLanSigner?.();
      if (!signer || typeof signer.signRequest !== 'function') {
        throw new WebAuthError(
          'LAN_SIGNER_REQUIRED',
          'LAN Web yêu cầu bộ ký thiết bị đã ghép đôi; command chưa được gửi đi.'
        );
      }
      const proof = await signer.signRequest({ method: 'POST', target: BUSINESS_COMMAND_PATH, body: rawBody });
      for (const [name, value] of Object.entries(validatedLanProofHeaders(proof))) headers.set(name, value);
    }

    const response = await authClient.request(BUSINESS_COMMAND_PATH, {
      method: 'POST',
      headers,
      body: rawBody
    });

    return {
      ...response,
      requestId: response?.requestId || requestId,
      runtime: String(authClient.runtime || 'UNKNOWN').toUpperCase(),
      submittedCommand: Object.freeze({ commandCode, entityId, idempotencyKey, expectedEntityVersion })
    };
  }

  return Object.freeze({ submitCommand });
}
