import fs from 'node:fs';
import path from 'node:path';

function requiredEnv(name) {
  const value = process.env[name];
  if (!value) throw new Error(`Missing required environment variable ${name}`);
  return value;
}

function bindingName(value, context) {
  const normalized = String(value || '').trim();
  if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(normalized)) {
    throw new Error(`Invalid Worker binding name ${context}: ${normalized}`);
  }
  return normalized;
}

function cronSchedules(value) {
  if (value === undefined) return null;
  if (!Array.isArray(value)) throw new Error('cron_schedules must be an array when provided');
  const normalized = value.map((entry, index) => {
    const cron = String(entry || '').trim();
    if (!cron || cron.length > 128) throw new Error(`Invalid cron_schedules entry at index ${index}`);
    return cron;
  });
  if (new Set(normalized).size !== normalized.length) throw new Error('Duplicate cron_schedules entry');
  return normalized;
}

async function cfJson(url, apiToken, init = {}) {
  const response = await fetch(url, {
    ...init,
    headers: {
      authorization: `Bearer ${apiToken}`,
      ...(init.body ? { 'content-type': 'application/json' } : {}),
      ...(init.headers || {})
    }
  });
  let payload;
  try { payload = await response.json(); }
  catch { throw new Error(`Cloudflare returned non-JSON HTTP ${response.status}`); }
  if (!response.ok || payload?.success !== true) {
    throw new Error(`Cloudflare API failed HTTP ${response.status}: ${JSON.stringify(payload?.errors || []).slice(0, 1500)}`);
  }
  return payload;
}

function sortedCronList(payload) {
  return Array.isArray(payload?.result?.schedules)
    ? payload.result.schedules.map(item => String(item?.cron || '')).filter(Boolean).sort()
    : [];
}

async function main() {
  const accountId = requiredEnv('CLOUDFLARE_ACCOUNT_ID');
  const apiToken = requiredEnv('CLOUDFLARE_API_TOKEN');
  const specPath = process.argv[2] || 'service/worker/deploy.beta.json';
  const spec = JSON.parse(fs.readFileSync(specPath, 'utf8'));

  if (!spec?.target_worker || !spec?.source || !Array.isArray(spec?.modules) || spec.modules.length < 1) {
    throw new Error('Deployment spec must define target_worker, source and a non-empty modules list');
  }
  const schedules = cronSchedules(spec.cron_schedules);

  const mainModule = path.basename(spec.source);
  const sourceDir = path.dirname(path.resolve(spec.source));
  const modules = [...new Set(spec.modules)].sort();

  if (!modules.includes(mainModule)) {
    throw new Error(`Main module ${mainModule} is not listed in deployment modules`);
  }

  for (const moduleName of modules) {
    if (path.basename(moduleName) !== moduleName || !moduleName.endsWith('.js')) {
      throw new Error(`Invalid Worker module name ${moduleName}`);
    }
    const modulePath = path.join(sourceDir, moduleName);
    if (!fs.existsSync(modulePath)) throw new Error(`Missing Worker module ${modulePath}`);
  }

  if (!spec?.d1_binding?.name || !spec?.d1_binding?.database_id) {
    throw new Error('Deployment spec must define d1_binding.name and d1_binding.database_id');
  }

  const plainTextBindings = Object.entries(spec.vars || {}).map(([name, value]) => {
    const safeName = bindingName(name, 'vars');
    if (safeName === 'BUILD_SHA') throw new Error('BUILD_SHA is reserved by the deployment uploader');
    if (typeof value !== 'string') throw new Error(`Worker plain-text binding ${safeName} must be a string`);
    return { type: 'plain_text', name: safeName, text: value };
  });

  const secretEnvBindings = (Array.isArray(spec.secret_env_bindings) ? spec.secret_env_bindings : []).map((entry, index) => {
    if (!entry || typeof entry !== 'object' || Array.isArray(entry)) {
      throw new Error(`Invalid secret_env_bindings entry at index ${index}`);
    }
    const name = bindingName(entry.name, `secret_env_bindings[${index}].name`);
    const envName = bindingName(entry.env, `secret_env_bindings[${index}].env`);
    const minLength = Number.isSafeInteger(entry.min_length) && entry.min_length > 0 ? entry.min_length : 1;
    const value = requiredEnv(envName);
    if (value.length < minLength) {
      throw new Error(`Worker secret binding ${name} from ${envName} is shorter than required minimum ${minLength}`);
    }
    return { type: 'secret_text', name, text: value, envName };
  });

  const inheritBindings = [...new Set(Array.isArray(spec.inherit_bindings) ? spec.inherit_bindings : [])]
    .map(name => bindingName(name, 'inherit_bindings'));
  const explicitNames = new Set([
    spec.d1_binding.name,
    'BUILD_SHA',
    ...plainTextBindings.map(item => item.name),
    ...secretEnvBindings.map(item => item.name)
  ]);
  if (explicitNames.size !== 2 + plainTextBindings.length + secretEnvBindings.length) {
    throw new Error('Duplicate explicit Worker binding name in deployment spec');
  }
  for (const name of inheritBindings) {
    if (explicitNames.has(name)) throw new Error(`Inherited binding ${name} collides with an explicit deployment binding`);
  }

  const metadata = {
    main_module: mainModule,
    compatibility_date: spec.compatibility_date,
    bindings: [
      { type: 'd1', name: spec.d1_binding.name, database_id: spec.d1_binding.database_id },
      ...plainTextBindings,
      { type: 'plain_text', name: 'BUILD_SHA', text: process.env.GITHUB_SHA || 'local' },
      ...secretEnvBindings.map(({ type, name, text }) => ({ type, name, text })),
      ...inheritBindings.map(name => ({ type: 'inherit', name }))
    ]
  };

  const form = new FormData();
  form.append('metadata', new Blob([JSON.stringify(metadata)], { type: 'application/json' }), 'metadata.json');

  for (const moduleName of modules) {
    const source = fs.readFileSync(path.join(sourceDir, moduleName), 'utf8');
    form.append(moduleName, new Blob([source], { type: 'application/javascript+module' }), moduleName);
  }

  const strictInheritance = inheritBindings.length ? '?bindings_inherit=strict' : '';
  const scriptBaseUrl = `https://api.cloudflare.com/client/v4/accounts/${encodeURIComponent(accountId)}/workers/scripts/${encodeURIComponent(spec.target_worker)}`;
  const response = await fetch(`${scriptBaseUrl}${strictInheritance}`, {
    method: 'PUT',
    headers: { authorization: `Bearer ${apiToken}` },
    body: form
  });

  let payload;
  try { payload = await response.json(); }
  catch { throw new Error(`Worker upload returned non-JSON HTTP ${response.status}`); }

  if (!response.ok || payload?.success !== true) {
    throw new Error(`Worker upload failed HTTP ${response.status}: ${JSON.stringify(payload?.errors || []).slice(0, 1500)}`);
  }

  if (schedules !== null) {
    const expected = [...schedules].sort();
    const beforePayload = await cfJson(`${scriptBaseUrl}/schedules`, apiToken);
    const before = sortedCronList(beforePayload);
    let applied = before;
    if (JSON.stringify(before) !== JSON.stringify(expected)) {
      const schedulePayload = await cfJson(`${scriptBaseUrl}/schedules`, apiToken, {
        method: 'PUT',
        body: JSON.stringify(schedules.map(cron => ({ cron })))
      });
      applied = sortedCronList(schedulePayload);
      console.log(`WORKER_CRON_SCHEDULES_UPDATED before=${JSON.stringify(before)} after=${JSON.stringify(applied)}`);
    } else {
      console.log(`WORKER_CRON_SCHEDULES_UNCHANGED schedules=${JSON.stringify(applied)}`);
    }
    if (JSON.stringify(applied) !== JSON.stringify(expected)) {
      throw new Error(`Worker cron schedule readback mismatch expected=${JSON.stringify(expected)} actual=${JSON.stringify(applied)}`);
    }
  }

  console.log(`WORKER_MULTI_MODULE_UPLOAD_PASS worker=${spec.target_worker} modules=${modules.length}`);
  for (const moduleName of modules) console.log(`WORKER_MODULE=${moduleName}`);
  for (const item of secretEnvBindings) console.log(`WORKER_SECRET_BINDING_PROVISIONED=${item.name}`);
  for (const name of inheritBindings) console.log(`WORKER_INHERITED_BINDING=${name}`);
  if (schedules !== null) console.log(`WORKER_CRON_SCHEDULES_PASS count=${schedules.length}`);
}

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
