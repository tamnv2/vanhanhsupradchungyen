/** VHDCHY Google Gateway foundation. */
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

  const workbook = SpreadsheetApp.openById(BOOTSTRAP.projectionSpreadsheetId);
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
    workbookTitle: workbook.getName(),
    sheetCount: workbook.getSheets().length
  };
}

function bootstrapHealth_() {
  const props = PropertiesService.getScriptProperties();
  const authorized = Boolean(props.getProperty("VHDCHY_BOOTSTRAP_AUTH_AT"));
  const configMatch =
    props.getProperty("VHDCHY_ENVIRONMENT") === BOOTSTRAP.environment &&
    props.getProperty("VHDCHY_OWNER_EMAIL") === BOOTSTRAP.ownerEmail &&
    props.getProperty("VHDCHY_PROJECTION_SPREADSHEET_ID") === BOOTSTRAP.projectionSpreadsheetId;

  return {
    authorized,
    configMatch
  };
}

function doGet() {
  const bootstrap = bootstrapHealth_();
  return json_({
    ok: bootstrap.authorized && bootstrap.configMatch,
    service: "VHDCHY_GOOGLE_GATEWAY",
    environment: BOOTSTRAP.environment,
    scriptId: ScriptApp.getScriptId(),
    deployedUrl: ScriptApp.getService().getUrl() || null,
    bootstrap
  });
}

function doPost() {
  return json_({
    ok: false,
    code: "FOUNDATION_ONLY",
    environment: BOOTSTRAP.environment,
    message: "Business runtime chưa được kích hoạt."
  });
}

function json_(obj) {
  return ContentService.createTextOutput(JSON.stringify(obj)).setMimeType(ContentService.MimeType.JSON);
}
