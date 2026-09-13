using Vhdchy.LanService;

namespace Vhdchy.LanService.Harness;

internal static class AuthorizationVectors
{
    public const string Payload1 = """
        {
          "schemaVersion":"VHDCHY_AUTHORITY_SNAPSHOT_V1",
          "permissionCatalogVersion":"VHDCHY_PERMISSION_CATALOG_V1",
          "users":[
            {"userId":"U_ALLOW","status":"ACTIVE","securityLevel":"NORMAL"},
            {"userId":"U_DENY","status":"ACTIVE","securityLevel":"NORMAL"},
            {"userId":"U_ROLE","status":"ACTIVE","securityLevel":"NORMAL"},
            {"userId":"U_SCOPE","status":"ACTIVE","securityLevel":"NORMAL"},
            {"userId":"U_DISABLED","status":"DISABLED","securityLevel":"NORMAL"},
            {"userId":"U_SUPER","status":"ACTIVE","securityLevel":"SUPERADMIN"}
          ],
          "roles":[
            {"roleId":"ROLE_EMPLOYEE_ALLOW","status":"ACTIVE"},
            {"roleId":"ROLE_EMPLOYEE_DENY","status":"ACTIVE"}
          ],
          "permissions":[
            {"permissionId":"PERM:employee:create","resource":"employee","action":"create","status":"ACTIVE"},
            {"permissionId":"PERM:employee:edit","resource":"employee","action":"edit","status":"ACTIVE"}
          ],
          "rolePermissionGrants":[
            {"roleId":"ROLE_EMPLOYEE_ALLOW","permissionId":"PERM:employee:create","effect":"ALLOW"},
            {"roleId":"ROLE_EMPLOYEE_DENY","permissionId":"PERM:employee:create","effect":"DENY"}
          ],
          "userRoleGrants":[
            {"grantId":"GRANT-ROLE-ALLOW","userId":"U_ROLE","roleId":"ROLE_EMPLOYEE_ALLOW","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","status":"ACTIVE"},
            {"grantId":"GRANT-ROLE-DENY","userId":"U_DENY","roleId":"ROLE_EMPLOYEE_DENY","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","status":"ACTIVE"}
          ],
          "userPermissionGrants":[
            {"grantId":"GRANT-DIRECT-ALLOW","userId":"U_ALLOW","permissionId":"PERM:employee:create","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
            {"grantId":"GRANT-DIRECT-DENY-BASE-ALLOW","userId":"U_DENY","permissionId":"PERM:employee:create","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
            {"grantId":"GRANT-WRONG-SCOPE","userId":"U_SCOPE","permissionId":"PERM:employee:create","clusterId":"OTHER_CLUSTER","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
            {"grantId":"GRANT-DISABLED","userId":"U_DISABLED","permissionId":"PERM:employee:create","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
            {"grantId":"GRANT-SUPER","userId":"U_SUPER","permissionId":"PERM:employee:create","clusterId":null,"moduleId":null,"effect":"ALLOW","status":"ACTIVE"}
          ]
        }
        """;

    public const string Payload2 = """
        {
          "schemaVersion":"VHDCHY_AUTHORITY_SNAPSHOT_V1",
          "permissionCatalogVersion":"VHDCHY_PERMISSION_CATALOG_V1",
          "users":[
            {"userId":"U_ALLOW","status":"ACTIVE","securityLevel":"NORMAL"},
            {"userId":"U_DENY","status":"ACTIVE","securityLevel":"NORMAL"},
            {"userId":"U_ROLE","status":"ACTIVE","securityLevel":"NORMAL"},
            {"userId":"U_SCOPE","status":"ACTIVE","securityLevel":"NORMAL"},
            {"userId":"U_DISABLED","status":"DISABLED","securityLevel":"NORMAL"},
            {"userId":"U_SUPER","status":"ACTIVE","securityLevel":"SUPERADMIN"},
            {"userId":"U_EXTRA","status":"ACTIVE","securityLevel":"NORMAL"}
          ],
          "roles":[
            {"roleId":"ROLE_EMPLOYEE_ALLOW","status":"ACTIVE"},
            {"roleId":"ROLE_EMPLOYEE_DENY","status":"ACTIVE"}
          ],
          "permissions":[
            {"permissionId":"PERM:employee:create","resource":"employee","action":"create","status":"ACTIVE"},
            {"permissionId":"PERM:employee:edit","resource":"employee","action":"edit","status":"ACTIVE"}
          ],
          "rolePermissionGrants":[
            {"roleId":"ROLE_EMPLOYEE_ALLOW","permissionId":"PERM:employee:create","effect":"ALLOW"},
            {"roleId":"ROLE_EMPLOYEE_DENY","permissionId":"PERM:employee:create","effect":"DENY"}
          ],
          "userRoleGrants":[
            {"grantId":"GRANT-ROLE-ALLOW","userId":"U_ROLE","roleId":"ROLE_EMPLOYEE_ALLOW","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","status":"ACTIVE"},
            {"grantId":"GRANT-ROLE-DENY","userId":"U_DENY","roleId":"ROLE_EMPLOYEE_DENY","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","status":"ACTIVE"}
          ],
          "userPermissionGrants":[
            {"grantId":"GRANT-DIRECT-ALLOW","userId":"U_ALLOW","permissionId":"PERM:employee:create","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
            {"grantId":"GRANT-DIRECT-DENY-BASE-ALLOW","userId":"U_DENY","permissionId":"PERM:employee:create","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
            {"grantId":"GRANT-WRONG-SCOPE","userId":"U_SCOPE","permissionId":"PERM:employee:create","clusterId":"OTHER_CLUSTER","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
            {"grantId":"GRANT-DISABLED","userId":"U_DISABLED","permissionId":"PERM:employee:create","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
            {"grantId":"GRANT-SUPER","userId":"U_SUPER","permissionId":"PERM:employee:create","clusterId":null,"moduleId":null,"effect":"ALLOW","status":"ACTIVE"}
          ]
        }
        """;

    public static async Task RunAsync(
        string databasePath,
        string environment,
        string clusterId,
        string moduleId,
        string compatibility)
    {
        static void Assert(bool condition, string code)
        {
            if (!condition) throw new InvalidOperationException(code);
        }

        var evaluator = new LanAuthorizationEvaluator(databasePath, compatibility);
        var inspection = await evaluator.InspectActiveSnapshotAsync();
        Assert(inspection.Ready, $"AUTHORITY_INSPECTION_NOT_READY_{inspection.Code}");
        Assert(inspection.AuthoritySnapshotVersion == "AUTH-TEST-2", "AUTHORITY_INSPECTION_VERSION_WRONG");
        Assert(inspection.PermissionCatalogVersion == "VHDCHY_PERMISSION_CATALOG_V1", "AUTHORITY_INSPECTION_CATALOG_WRONG");

        var allow = await evaluator.AuthorizeAsync(new LanAuthorizationRequest(
            "U_ALLOW", "employee", "create", clusterId, moduleId));
        Assert(allow.Allowed && allow.Code == "AUTHORIZED" && allow.MatchedEffect == "ALLOW", "DIRECT_ALLOW_FAILED");

        var deny = await evaluator.AuthorizeAsync(new LanAuthorizationRequest(
            "U_DENY", "employee", "create", clusterId, moduleId));
        Assert(!deny.Allowed && deny.Code == "PERMISSION_DENIED" && deny.MatchedEffect == "DENY", "DENY_PRECEDENCE_FAILED");

        var roleAllow = await evaluator.AuthorizeAsync(new LanAuthorizationRequest(
            "U_ROLE", "employee", "create", clusterId, moduleId));
        Assert(roleAllow.Allowed && roleAllow.MatchedSource == "ROLE_PERMISSION", "ROLE_ALLOW_FAILED");

        var wrongCluster = await evaluator.AuthorizeAsync(new LanAuthorizationRequest(
            "U_SCOPE", "employee", "create", clusterId, moduleId));
        Assert(!wrongCluster.Allowed && wrongCluster.Code == "PERMISSION_DENIED", "WRONG_CLUSTER_SCOPE_ALLOWED");

        var wrongModule = await evaluator.AuthorizeAsync(new LanAuthorizationRequest(
            "U_ALLOW", "employee", "create", clusterId, "OTHER_MODULE"));
        Assert(!wrongModule.Allowed && wrongModule.Code == "PERMISSION_DENIED", "WRONG_MODULE_SCOPE_ALLOWED");

        var disabled = await evaluator.AuthorizeAsync(new LanAuthorizationRequest(
            "U_DISABLED", "employee", "create", clusterId, moduleId));
        Assert(!disabled.Allowed && disabled.Code == "ACCOUNT_NOT_ACTIVE", "DISABLED_ACCOUNT_ALLOWED");

        var normalMinimumFailure = await evaluator.AuthorizeAsync(new LanAuthorizationRequest(
            "U_ALLOW", "employee", "create", clusterId, moduleId, MinimumSecurityLevel: "SUPERADMIN"));
        Assert(!normalMinimumFailure.Allowed && normalMinimumFailure.Code == "SECURITY_LEVEL_REQUIRED", "SECURITY_LEVEL_MINIMUM_NOT_ENFORCED");

        var superadmin = await evaluator.AuthorizeAsync(new LanAuthorizationRequest(
            "U_SUPER", "employee", "create", clusterId, moduleId, MinimumSecurityLevel: "SUPERADMIN"));
        Assert(superadmin.Allowed && superadmin.SecurityLevel == "SUPERADMIN", "SUPERADMIN_MINIMUM_FAILED");

        var uncataloged = await evaluator.AuthorizeAsync(new LanAuthorizationRequest(
            "U_ALLOW", "employee", "status", clusterId, moduleId));
        Assert(!uncataloged.Allowed && uncataloged.Code == "PERMISSION_NOT_CATALOGED", "UNCATALOGED_PERMISSION_ALLOWED");

        Console.WriteLine("LAN_AUTHORIZATION_HARNESS_PASS directAllow=PASS denyPrecedence=PASS roleAllow=PASS clusterScope=PASS moduleScope=PASS disabled=PASS minimumSecurity=PASS uncataloged=PASS");
    }
}
