import assert from 'node:assert/strict';
import { test } from 'node:test';
import { handleRequest } from '../src/index.js';

test('Cloud capabilities identify CLOUD runtime and advertise only implemented Slice-1 mutation path', async () => {
  const response = await handleRequest(new Request('https://beta.supra.cc.cd/api/v1/capabilities'), {});
  assert.equal(response.status, 200);
  const body = await response.json();
  assert.equal(body.ok, true);
  assert.equal(body.runtime, 'CLOUD');
  assert.equal(body.runtimeState, 'BUSINESS_CORE_V3');
  assert.equal(body.businessMutationEnabled, true);
  assert.equal(body.businessMutationPath, '/api/v1/data/commands');
  assert.equal(body.businessSlice, 'IDENTITY_EMPLOYEE_ATTENDANCE');
  assert.deepEqual(body.blockedBusinessCommands, ['EMPLOYEE_PORTRAIT_REPLACE']);
  assert.equal(body.anonymousMutationAllowed, false);
});

console.log('CLOUD_CAPABILITIES_SLICE1_PASS runtime=CLOUD mutationEnabled=PASS portraitBlocked=PASS');
