import httpWorker from './index.js';
import { processProjectionOutbox } from './projection-processor.js';

export const PROJECTION_SCHEDULE_LIMIT = 25;
export const PROJECTION_DELIVERY_ENABLED_VALUE = 'true';
export const PROJECTION_DIAGNOSTIC_KEY = 'projection_scheduler_probe';

function projectionRuntimeConfig(env) {
  const deliveryEnabled = String(env?.PROJECTION_DELIVERY_ENABLED || '').trim().toLowerCase() === PROJECTION_DELIVERY_ENABLED_VALUE;
  if (!deliveryEnabled) return { ok: false, code: 'PROJECTION_DELIVERY_DISABLED' };
  if (!env?.DB) return { ok: false, code: 'PROJECTION_DB_UNAVAILABLE' };
  const gatewayUrl = String(env.GAS_EXEC_URL || '').trim();
  if (!gatewayUrl) return { ok: false, code: 'PROJECTION_GATEWAY_URL_NOT_CONFIGURED' };
  const sharedToken = String(env.VHDCHY_PROJECTION_SHARED_TOKEN || '');
  if (sharedToken.length < 32 || sharedToken.length > 256) {
    return { ok: false, code: 'PROJECTION_SHARED_TOKEN_NOT_CONFIGURED' };
  }
  return {
    ok: true,
    gatewayUrl,
    sharedToken,
    environment: String(env.APP_ENV || 'BETA').toUpperCase()
  };
}

function diagnosticsEnabled(env) {
  return String(env?.PROJECTION_SCHEDULE_DIAGNOSTICS_ENABLED || '').trim().toLowerCase() === 'true';
}

async function recordProjectionScheduleDiagnostic(env, phase, detail = {}) {
  if (!diagnosticsEnabled(env) || !env?.DB) return;
  const safe = {
    phase: String(phase || 'UNKNOWN').slice(0, 32),
    at: new Date().toISOString(),
    code: detail?.code ? String(detail.code).slice(0, 128) : null,
    loaded: Number(detail?.loaded || 0),
    claimed: Number(detail?.claimed || 0),
    acked: Number(detail?.acked || 0),
    failed: Number(detail?.failed || 0)
  };
  try {
    await env.DB.prepare(`
      INSERT INTO vhdchy_meta(key, value, updated_at)
      VALUES(?, ?, CURRENT_TIMESTAMP)
      ON CONFLICT(key) DO UPDATE SET value=excluded.value, updated_at=CURRENT_TIMESTAMP
    `).bind(PROJECTION_DIAGNOSTIC_KEY, JSON.stringify(safe)).run();
  } catch (error) {
    console.warn('PROJECTION_SCHEDULE_DIAGNOSTIC_FAILED', String(error?.message || error).slice(0, 192));
  }
}

export async function runProjectionSchedule(env, options = {}) {
  const config = projectionRuntimeConfig(env);
  if (!config.ok) {
    return {
      ok: config.code === 'PROJECTION_DELIVERY_DISABLED',
      code: config.code,
      loaded: 0,
      claimed: 0,
      acked: 0,
      failed: 0
    };
  }

  const processor = options.processor || processProjectionOutbox;
  return processor(env.DB, {
    gatewayUrl: config.gatewayUrl,
    environment: config.environment,
    sharedToken: config.sharedToken,
    limit: PROJECTION_SCHEDULE_LIMIT,
    nowMs: options.nowMs
  });
}

async function scheduled(_controller, env, ctx) {
  await recordProjectionScheduleDiagnostic(env, 'START');
  const task = runProjectionSchedule(env)
    .then(async result => {
      await recordProjectionScheduleDiagnostic(env, 'RESULT', result);
      if (!['PROJECTION_IDLE', 'PROJECTION_BATCH_ACKED', 'PROJECTION_DELIVERY_DISABLED'].includes(result?.code)) {
        console.warn('PROJECTION_SCHEDULE_RESULT', JSON.stringify({
          ok: result?.ok === true,
          code: result?.code || 'UNKNOWN',
          loaded: Number(result?.loaded || 0),
          claimed: Number(result?.claimed || 0),
          acked: Number(result?.acked || 0),
          failed: Number(result?.failed || 0)
        }));
      }
      return result;
    })
    .catch(async error => {
      await recordProjectionScheduleDiagnostic(env, 'ERROR', { code: error?.message || 'UNEXPECTED' });
      console.error('PROJECTION_SCHEDULE_UNEXPECTED', String(error?.message || error).slice(0, 256));
      throw error;
    });

  if (typeof ctx?.waitUntil === 'function') ctx.waitUntil(task.then(() => undefined));
  return task;
}

export default {
  fetch: httpWorker.fetch,
  scheduled
};
