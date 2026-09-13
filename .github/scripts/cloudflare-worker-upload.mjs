import fs from 'node:fs';
import path from 'node:path';

function requiredEnv(name) {
  const value = process.env[name];
  if (!value) throw new Error(`Missing required environment variable ${name}`);
  return value;
}

async function main() {
  const accountId = requiredEnv('CLOUDFLARE_ACCOUNT_ID');
  const apiToken = requiredEnv('CLOUDFLARE_API_TOKEN');
  const specPath = process.argv[2] || 'service/worker/deploy.beta.json';
  const spec = JSON.parse(fs.readFileSync(specPath, 'utf8'));

  if (!spec?.target_worker || !spec?.source || !Array.isArray(spec?.modules) || spec.modules.length < 1) {
    throw new Error('Deployment spec must define target_worker, source and a non-empty modules list');
  }

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

  const metadata = {
    main_module: mainModule,
    compatibility_date: spec.compatibility_date,
    bindings: [
      { type: 'd1', name: spec.d1_binding.name, database_id: spec.d1_binding.database_id },
      { type: 'plain_text', name: 'APP_ENV', text: spec.vars.APP_ENV },
      { type: 'plain_text', name: 'BUILD_SHA', text: process.env.GITHUB_SHA || 'local' },
      { type: 'plain_text', name: 'GAS_EXEC_URL', text: spec.vars.GAS_EXEC_URL }
    ]
  };

  const form = new FormData();
  form.append('metadata', new Blob([JSON.stringify(metadata)], { type: 'application/json' }), 'metadata.json');

  for (const moduleName of modules) {
    const source = fs.readFileSync(path.join(sourceDir, moduleName), 'utf8');
    form.append(moduleName, new Blob([source], { type: 'application/javascript+module' }), moduleName);
  }

  const url = `https://api.cloudflare.com/client/v4/accounts/${encodeURIComponent(accountId)}/workers/scripts/${encodeURIComponent(spec.target_worker)}`;
  const response = await fetch(url, {
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

  console.log(`WORKER_MULTI_MODULE_UPLOAD_PASS worker=${spec.target_worker} modules=${modules.length}`);
  for (const moduleName of modules) console.log(`WORKER_MODULE=${moduleName}`);
}

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
