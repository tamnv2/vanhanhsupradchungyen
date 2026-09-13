const ROOT_EXCLUSIVE_RESOURCES = new Set([
  'ROOT_SECURITY',
  'ROOT_RECOVERY',
  'ROOT_POLICY'
]);

function normalizeScope(value) {
  if (value === null || value === undefined || value === '') return null;
  return String(value);
}

export function scopeMatches(grant, clusterId = null, moduleId = null) {
  const grantCluster = normalizeScope(grant?.cluster_id);
  const grantModule = normalizeScope(grant?.module_id);
  const targetCluster = normalizeScope(clusterId);
  const targetModule = normalizeScope(moduleId);

  if (grantCluster !== null && grantCluster !== targetCluster) return false;
  if (grantModule !== null && grantModule !== targetModule) return false;
  return true;
}

export function isRootExclusiveResource(resourceCode) {
  return ROOT_EXCLUSIVE_RESOURCES.has(String(resourceCode || '').toUpperCase());
}

export function evaluatePermission({
  securityLevel = 'NORMAL',
  grants = [],
  resourceCode,
  actionCode,
  clusterId = null,
  moduleId = null
}) {
  const level = String(securityLevel || 'NORMAL').toUpperCase();
  const resource = String(resourceCode || '').toUpperCase();
  const action = String(actionCode || '').toUpperCase();

  if (!resource || !action) {
    return { allowed: false, reason: 'INVALID_PERMISSION_REQUEST' };
  }

  if (level === 'ROOT') {
    return { allowed: true, reason: 'ROOT_AUTHORITY' };
  }

  if (level === 'SUPERADMIN') {
    if (isRootExclusiveResource(resource)) {
      return { allowed: false, reason: 'ROOT_EXCLUSIVE_POLICY' };
    }
    return { allowed: true, reason: 'SUPERADMIN_BUSINESS_AUTHORITY' };
  }

  let allow = false;
  for (const grant of Array.isArray(grants) ? grants : []) {
    if (String(grant?.resource_code || '').toUpperCase() !== resource) continue;
    if (String(grant?.action_code || '').toUpperCase() !== action) continue;
    if (!scopeMatches(grant, clusterId, moduleId)) continue;

    const effect = String(grant?.effect || '').toUpperCase();
    if (effect === 'DENY') {
      return { allowed: false, reason: 'EXPLICIT_DENY' };
    }
    if (effect === 'ALLOW') allow = true;
  }

  return allow
    ? { allowed: true, reason: 'EXPLICIT_ALLOW' }
    : { allowed: false, reason: 'NO_MATCHING_GRANT' };
}

export function summarizeAllowedPermissions({
  securityLevel = 'NORMAL',
  grants = [],
  clusterId = null,
  moduleId = null
}) {
  const level = String(securityLevel || 'NORMAL').toUpperCase();
  if (level === 'ROOT') return { authority: 'ROOT', unrestricted: true, permissions: ['*:*'] };
  if (level === 'SUPERADMIN') {
    return {
      authority: 'SUPERADMIN',
      unrestrictedBusiness: true,
      excludedResources: [...ROOT_EXCLUSIVE_RESOURCES].sort(),
      permissions: ['BUSINESS:*']
    };
  }

  const relevant = (Array.isArray(grants) ? grants : []).filter(grant =>
    scopeMatches(grant, clusterId, moduleId)
  );

  const decisions = new Map();
  for (const grant of relevant) {
    const resource = String(grant?.resource_code || '').toUpperCase();
    const action = String(grant?.action_code || '').toUpperCase();
    const effect = String(grant?.effect || '').toUpperCase();
    if (!resource || !action || !['ALLOW', 'DENY'].includes(effect)) continue;
    const key = `${resource}:${action}`;
    if (effect === 'DENY') decisions.set(key, 'DENY');
    else if (!decisions.has(key)) decisions.set(key, 'ALLOW');
  }

  return {
    authority: 'GRANTS',
    unrestricted: false,
    permissions: [...decisions.entries()]
      .filter(([, effect]) => effect === 'ALLOW')
      .map(([key]) => key)
      .sort()
  };
}
