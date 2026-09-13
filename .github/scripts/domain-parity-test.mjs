import fs from 'node:fs';

const domain = JSON.parse(fs.readFileSync('contracts/domain.v1.json', 'utf8'));
const slice = JSON.parse(fs.readFileSync('contracts/commands.slice1.v1.json', 'utf8'));
const acceptance = JSON.parse(fs.readFileSync('contracts/acceptance.v1.json', 'utf8'));
const mutationResult = JSON.parse(fs.readFileSync('contracts/mutation-result.v1.schema.json', 'utf8'));
const permissionCatalog = JSON.parse(fs.readFileSync('config/permissions.v1.json', 'utf8'));

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function sameSet(left, right) {
  return JSON.stringify([...left].sort()) === JSON.stringify([...right].sort());
}

assert(domain.contractVersion === 'VHDCHY_DOMAIN_V1', 'Unexpected domain contract version');
assert(slice.domainContractVersion === domain.contractVersion, 'Slice/domain version mismatch');
assert(acceptance.domainContractVersion === domain.contractVersion, 'Acceptance/domain version mismatch');
assert(mutationResult.$id === 'VHDCHY_MUTATION_RESULT_V1', 'Unexpected mutation result schema id');
assert(permissionCatalog.schemaVersion === 'VHDCHY_PERMISSION_CATALOG_V1', 'Unexpected permission catalog version');

const commandCodes = new Set(Object.values(domain.commandFamilies || {}).flat());
const eventCodes = new Set(Object.values(domain.eventFamilies || {}).flat());
const permissionKeys = new Set(
  (permissionCatalog.permissions || []).map(item => `${item.resource}:${item.action}`)
);
const commitStatuses = new Set(domain.commitStatuses || []);
const googleStatuses = new Set(domain.googleOutputStatuses || []);
const stableErrors = new Set(domain.stableErrors || []);

const resultCommitStatuses = new Set(mutationResult.properties?.commitStatus?.enum || []);
const resultGoogleStatuses = new Set(mutationResult.properties?.googleOutputStatus?.enum || []);
assert(sameSet(commitStatuses, resultCommitStatuses), 'Mutation result commitStatus enum differs from domain contract');
assert(sameSet(googleStatuses, resultGoogleStatuses), 'Mutation result googleOutputStatus enum differs from domain contract');
for (const required of ['ok', 'requestId', 'commitStatus', 'googleOutputStatus']) {
  assert((mutationResult.required || []).includes(required), `Mutation result required field missing: ${required}`);
}

assert(Array.isArray(slice.commands) && slice.commands.length > 0, 'Slice command list is empty');
for (const command of slice.commands) {
  assert(commandCodes.has(command.code), `Unknown command code: ${command.code}`);
  assert(eventCodes.has(command.eventIntent), `Unknown event intent: ${command.eventIntent}`);
  const permissionKey = `${command.permission?.resource}:${command.permission?.action}`;
  assert(permissionKeys.has(permissionKey), `Unknown permission reference for ${command.code}: ${permissionKey}`);
  assert(typeof command.entityType === 'string' && command.entityType.length > 0, `Missing entityType for ${command.code}`);
  assert(typeof command.expectedEntityVersion === 'string' && command.expectedEntityVersion.length > 0, `Missing version semantics for ${command.code}`);
  assert(typeof command.lanAutonomousSemantics === 'string' && command.lanAutonomousSemantics.length > 0, `Missing LAN semantics for ${command.code}`);
}

assert(Array.isArray(acceptance.vectors) && acceptance.vectors.length > 0, 'Acceptance vector list is empty');
const ids = new Set();
for (const vector of acceptance.vectors) {
  assert(typeof vector.id === 'string' && vector.id.length > 0, 'Acceptance vector id missing');
  assert(!ids.has(vector.id), `Duplicate acceptance vector id: ${vector.id}`);
  ids.add(vector.id);
  const expect = vector.expect || {};
  if (expect.commitStatus !== undefined) {
    assert(commitStatuses.has(expect.commitStatus), `Unknown commit status in ${vector.id}: ${expect.commitStatus}`);
  }
  if (expect.googleOutputStatus !== undefined) {
    assert(googleStatuses.has(expect.googleOutputStatus), `Unknown Google status in ${vector.id}: ${expect.googleOutputStatus}`);
  }
  if (expect.error !== undefined) {
    assert(stableErrors.has(expect.error), `Unknown stable error in ${vector.id}: ${expect.error}`);
  }
}

console.log(`DOMAIN_PARITY_PASS commands=${slice.commands.length} vectors=${acceptance.vectors.length} resultSchema=PASS`);
