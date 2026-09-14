using Vhdchy.LanService;

namespace Vhdchy.LanSlice1Business.Harness;

internal static class TestAuthority
{
    internal const string Environment = "BETA";
    internal const string ClusterId = "PICK_PACK_1291";
    internal const string Compatibility = "VHDCHY_DOMAIN_V1";
    internal const string ModuleId = "IDENTITY_EMPLOYEE_ATTENDANCE";

    private const string Scope = "{\"clusterId\":\"PICK_PACK_1291\",\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}";

    private const string Payload = """
    {
      "schemaVersion":"VHDCHY_AUTHORITY_SNAPSHOT_V1",
      "permissionCatalogVersion":"VHDCHY_PERMISSION_CATALOG_V1",
      "users":[
        {"userId":"U_ALL","status":"ACTIVE","securityLevel":"NORMAL"},
        {"userId":"U_ALT","status":"ACTIVE","securityLevel":"NORMAL"},
        {"userId":"U_DENY","status":"ACTIVE","securityLevel":"NORMAL"}
      ],
      "roles":[],
      "permissions":[
        {"permissionId":"PERM:employee:create","resource":"employee","action":"create","status":"ACTIVE"},
        {"permissionId":"PERM:employee:edit","resource":"employee","action":"edit","status":"ACTIVE"},
        {"permissionId":"PERM:employee:status","resource":"employee","action":"status","status":"ACTIVE"},
        {"permissionId":"PERM:employee:portrait","resource":"employee","action":"portrait","status":"ACTIVE"},
        {"permissionId":"PERM:attendance:scan","resource":"attendance","action":"scan","status":"ACTIVE"},
        {"permissionId":"PERM:attendance:correct","resource":"attendance","action":"correct","status":"ACTIVE"}
      ],
      "rolePermissionGrants":[],
      "userRoleGrants":[],
      "userPermissionGrants":[
        {"grantId":"ALL-CREATE","userId":"U_ALL","permissionId":"PERM:employee:create","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
        {"grantId":"ALL-EDIT","userId":"U_ALL","permissionId":"PERM:employee:edit","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
        {"grantId":"ALL-STATUS","userId":"U_ALL","permissionId":"PERM:employee:status","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
        {"grantId":"ALL-PORTRAIT","userId":"U_ALL","permissionId":"PERM:employee:portrait","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
        {"grantId":"ALL-SCAN","userId":"U_ALL","permissionId":"PERM:attendance:scan","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
        {"grantId":"ALL-CORRECT","userId":"U_ALL","permissionId":"PERM:attendance:correct","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
        {"grantId":"ALT-CREATE","userId":"U_ALT","permissionId":"PERM:employee:create","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
        {"grantId":"DENY-ALLOW","userId":"U_DENY","permissionId":"PERM:employee:create","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"ALLOW","status":"ACTIVE"},
        {"grantId":"DENY-BLOCK","userId":"U_DENY","permissionId":"PERM:employee:create","clusterId":"PICK_PACK_1291","moduleId":"IDENTITY_EMPLOYEE_ATTENDANCE","effect":"DENY","status":"ACTIVE"}
      ]
    }
    """;

    internal static async Task SeedAsync(string databasePath)
    {
        var authorityStore = new AuthoritySnapshotStore(databasePath);
        var authority = await authorityStore.ImportAsync(
            new AuthoritySnapshotEnvelope(
                "AUTH-BUSINESS-1",
                Environment,
                ClusterId,
                "business-harness-authority",
                Compatibility,
                Scope,
                Payload),
            Environment,
            ClusterId,
            Compatibility);
        HarnessAssert.That(authority.Activated, "BUSINESS_AUTHORITY_NOT_ACTIVE");

        var operationalStore = new OperationalSnapshotStore(databasePath);
        var operational = await operationalStore.ImportAsync(
            new OperationalSnapshotEnvelope(
                "OP-BUSINESS-1",
                Environment,
                ClusterId,
                "business-harness-operational",
                Compatibility,
                "{\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
                "{\"employees\":[],\"employeeCodes\":[],\"presence\":[]}"),
            Environment,
            ClusterId,
            Compatibility,
            new[] { ModuleId });
        HarnessAssert.That(operational.Activated, "BUSINESS_OPERATIONAL_NOT_ACTIVE");
    }
}
