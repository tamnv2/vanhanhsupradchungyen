import httpWorker from './index.js';
import { processProjectionOutbox } from './projection-processor.js';

export const PROJECTION_SCHEDULE_LIMIT = 25;

function projectionRuntimeConfig(env) {
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

export async function runProjectionSchedule(env, options = {}) {
  const config = projectionRuntimeConfig(env);
  if (!config.ok) {
    return { ok: false, code: config.code, loaded: 0, claimed: 0, acked: 0, failed: 0 };
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
  const task = runProjectionSchedule(env)
    .then(result => {
      if (result?.code !== 'PROJECTION_IDLE' && result?.code !== 'PROJECTION_BATCH_ACKED') {
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
    .catch(error => {
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
