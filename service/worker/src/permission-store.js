import { evaluatePermission, summarizeAllowedPermissions } from './authorization.js';

function isoNow(value = Date.now()) {
  return new Date(value).toISOString();
}

export async function loadEffectiveGrants(db, userId, now = Date.now()) {
  if (!db || !userId) return [];
  const nowIso = isoNow(now);
  const result = await db.prepare(`
    SELECT
      p.resource_code,
      p.action_code,
      rpg.effect,
      urg.cluster_id,
      urg.module_id,
      urg.grant_id,
      'ROLE' AS grant_source
    FROM auth_user_role_grants urg
    JOIN auth_roles r ON r.role_id = urg.role_id
    JOIN auth_role_permission_grants rpg ON rpg.role_id = urg.role_id
    JOIN auth_permissions p ON p.permission_id = rpg.permission_id
    WHERE urg.user_id = ?
      AND urg.status = 'ACTIVE'
      AND r.status = 'ACTIVE'
      AND p.status = 'ACTIVE'
      AND urg.effective_from <= ?
      AND (urg.effective_to IS NULL OR urg.effective_to > ?)

    UNION ALL

    SELECT
      p.resource_code,
      p.action_code,
      upg.effect,
      upg.cluster_id,
      upg.module_id,
      upg.grant_id,
      'DIRECT' AS grant_source
    FROM auth_user_permission_grants upg
    JOIN auth_permissions p ON p.permission_id = upg.permission_id
    WHERE upg.user_id = ?
      AND upg.status = 'ACTIVE'
      AND p.status = 'ACTIVE'
      AND upg.effective_from <= ?
      AND (upg.effective_to IS NULL OR upg.effective_to > ?)
  `).bind(userId, nowIso, nowIso, userId, nowIso, nowIso).all();

  return Array.isArray(result?.results) ? result.results : [];
}

export async function authorizePrincipal(db, principal, permission, scope = {}, now = Date.now()) {
  if (!principal?.userId) {
    return { allowed: false, reason: 'AUTH_REQUIRED', grants: [] };
  }
  if (principal.mustChangePassword === true && String(permission?.resourceCode || '').toUpperCase() !== 'AUTH') {
    return { allowed: false, reason: 'PASSWORD_CHANGE_REQUIRED', grants: [] };
  }

  const grants = await loadEffectiveGrants(db, principal.userId, now);
  const decision = evaluatePermission({
    securityLevel: principal.securityLevel,
    grants,
    resourceCode: permission?.resourceCode,
    actionCode: permission?.actionCode,
    clusterId: scope?.clusterId ?? null,
    moduleId: scope?.moduleId ?? null
  });
  return { ...decision, grants };
}

export async function permissionSummary(db, principal, scope = {}, now = Date.now()) {
  if (!principal?.userId) return { authority: 'NONE', unrestricted: false, permissions: [] };
  const grants = await loadEffectiveGrants(db, principal.userId, now);
  return summarizeAllowedPermissions({
    securityLevel: principal.securityLevel,
    grants,
    clusterId: scope?.clusterId ?? null,
    moduleId: scope?.moduleId ?? null
  });
}
