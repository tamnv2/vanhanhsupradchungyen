/**
 * VHDCHY Google Gateway foundation.
 * Placeholder được CI thay trước khi update Apps Script project.
 */
const BOOTSTRAP = Object.freeze({
  environment: "__ENVIRONMENT__",
  ownerEmail: "__OWNER_EMAIL__",
  projectRootFolderId: "__ENV_ROOT_FOLDER_ID__",
  projectName: "VẬN HÀNH DC HƯNG YÊN"
});

function bootstrapAuthorize() {
  ScriptApp.requireAllScopes(ScriptApp.AuthMode.FULL);

  const root = DriveApp.getFolderById(BOOTSTRAP.projectRootFolderId);
  const activeEmail = Session.getActiveUser().getEmail() || Session.getEffectiveUser().getEmail();
  if (activeEmail && activeEmail.toLowerCase() !== BOOTSTRAP.ownerEmail.toLowerCase()) {
    throw new Error("OWNER_ACCOUNT_MISMATCH: " + activeEmail);
  }

  const temp = SpreadsheetApp.create("VHDCHY_" + BOOTSTRAP.environment + "_AUTH_TEST_" + Date.now());
  temp.getSheets()[0].getRange("A1:B2").setValues([
    ["environment", BOOTSTRAP.environment],
    ["status", "AUTH_TEST_PASS"]
  ]);
  DriveApp.getFileById(temp.getId()).setTrashed(true);

  const resp = UrlFetchApp.fetch("https://www.google.com/generate_204", { muteHttpExceptions: true });
  const triggerCount = ScriptApp.getProjectTriggers().length;

  PropertiesService.getScriptProperties().setProperties({
    VHDCHY_ENVIRONMENT: BOOTSTRAP.environment,
    VHDCHY_OWNER_EMAIL: BOOTSTRAP.ownerEmail,
    VHDCHY_ROOT_FOLDER_ID: BOOTSTRAP.projectRootFolderId,
    VHDCHY_BOOTSTRAP_AUTH_AT: new Date().toISOString()
  }, false);

  return { ok: true, environment: BOOTSTRAP.environment, scriptId: ScriptApp.getScriptId(), urlFetchStatus: resp.getResponseCode(), triggerCount: triggerCount };
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
