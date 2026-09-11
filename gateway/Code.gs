/**
 * VHDCHY Google Gateway foundation.
 * Placeholder được CI thay trước khi update Apps Script project.
 *
 * Least-privilege rule:
 * - current gateway needs the designated projection workbook + owner identity only;
 * - no Drive file-management, outbound UrlFetch or installable-trigger capability
 *   is authorized until an implemented Owner-approved feature needs it.
 */
const BOOTSTRAP = Object.freeze({
  environment: "__ENVIRONMENT__",
  ownerEmail: "__OWNER_EMAIL__",
  projectionSpreadsheetId: "__PROJECTION_SPREADSHEET_ID__",
  projectName: "VẬN HÀNH DC HƯNG YÊN"
});

function bootstrapAuthorize() {
  ScriptApp.requireAllScopes(ScriptApp.AuthMode.FULL);

  const activeEmail = Session.getActiveUser().getEmail() || Session.getEffectiveUser().getEmail();
  if (activeEmail && activeEmail.toLowerCase() !== BOOTSTRAP.ownerEmail.toLowerCase()) {
    throw new Error("OWNER_ACCOUNT_MISMATCH: " + activeEmail);
  }

  // Read-only probe against the explicitly designated projection workbook.
  // This verifies that the current owner can open the intended workbook without
  // requesting broad Drive access or creating/trashing temporary files.
  const workbook = SpreadsheetApp.openById(BOOTSTRAP.projectionSpreadsheetId);
  const workbookTitle = workbook.getName();
  const sheetCount = workbook.getSheets().length;

  PropertiesService.getScriptProperties().setProperties({
    VHDCHY_ENVIRONMENT: BOOTSTRAP.environment,
    VHDCHY_OWNER_EMAIL: BOOTSTRAP.ownerEmail,
    VHDCHY_PROJECTION_SPREADSHEET_ID: BOOTSTRAP.projectionSpreadsheetId,
    VHDCHY_BOOTSTRAP_AUTH_AT: new Date().toISOString()
  }, false);

  return {
    ok: true,
    environment: BOOTSTRAP.environment,
    scriptId: ScriptApp.getScriptId(),
    projectionSpreadsheetId: BOOTSTRAP.projectionSpreadsheetId,
    workbookTitle: workbookTitle,
    sheetCount: sheetCount
  };
}

function doGet() {
  return json_({
    ok: true,
    service: "VHDCHY_GOOGLE_GATEWAY",
    environment: BOOTSTRAP.environment,
    scriptId: ScriptApp.getScriptId(),
    deployedUrl: ScriptApp.getService().getUrl() || null
  });
}

function doPost() {
  return json_({
    ok: false,
    code: "FOUNDATION_ONLY",
    environment: BOOTSTRAP.environment,
    message: "Business runtime chưa được kích hoạt."
  }, 503);
}

function json_(obj, statusCode) {
  if (statusCode) obj.http_status = statusCode;
  return ContentService.createTextOutput(JSON.stringify(obj)).setMimeType(ContentService.MimeType.JSON);
}
