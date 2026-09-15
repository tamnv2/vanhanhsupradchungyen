/** VHDCHY Google Gateway foundation + projection contract. */
const BOOTSTRAP = Object.freeze({
  environment: "__ENVIRONMENT__",
  ownerEmail: "__OWNER_EMAIL__",
  projectionSpreadsheetId: "__PROJECTION_SPREADSHEET_ID__",
  projectName: "VẬN HÀNH DC HƯNG YÊN"
});

const PROJECTION_PROTOCOL = "VHDCHY_PROJECTION_V1";
const PROJECTION_MANAGEMENT_VERSION = "VHDCHY_PROJECTION_MANAGEMENT_V1";
const PROJECTION_MAX_ITEMS = 50;
const PROJECTION_SHEETS = Object.freeze({
  "DANH SÁCH PDA": { keyHeader: "Seri PDA" },
  "DANH SÁCH USER PICK": { keyHeader: "User Pick" },
  "DANH SÁCH BÀN PACK": { keyHeader: "Tên bàn pack" },
  "DANH SÁCH USER PACK": { keyHeader: "User Pack" },
  "DANH SÁCH NHÂN SỰ": { keyHeader: "Mã nhân viên" },
  "DANH SÁCH TÀI KHOẢN": { keyHeader: "Số User" },
  "LỊCH SỬ NGHIỆP VỤ": { keyHeader: "Event ID" },
  "RA - VÀO TRONG CA": { keyHeader: "Event ID" },
  "THÔNG TIN USER CỦA NLĐ": { keyHeader: "Event ID" },
  "CÔNG NHẬT": { keyHeader: "Event ID" },
  "Nhận hàng rớt": { keyHeader: "ID bản ghi" },
  "TÀI LIỆU": { keyHeader: "Document ID" },
  "CONFLICT CORRECTION": { keyHeader: "Correction ID" },
  "IMPORT AUDIT": { keyHeader: "Import ID" }
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

  return { authorized, configMatch };
}

function projectionHealth_() {
  const props = PropertiesService.getScriptProperties();
  return {
    protocol: PROJECTION_PROTOCOL,
    enabled: props.getProperty("VHDCHY_PROJECTION_ENABLED") === "true",
    authConfigured: Boolean(props.getProperty("VHDCHY_PROJECTION_SHARED_TOKEN_SHA256")),
    allowedSheetCount: Object.keys(PROJECTION_SHEETS).length
  };
}

function projectionManagementContext_(expectedEnvironment) {
  const requestedEnvironment = String(expectedEnvironment || "").toUpperCase();
  if (!requestedEnvironment || requestedEnvironment !== String(BOOTSTRAP.environment).toUpperCase()) {
    throw new Error("PROJECTION_MANAGEMENT_ENVIRONMENT_MISMATCH");
  }

  const bootstrap = bootstrapHealth_();
  if (!bootstrap.authorized || !bootstrap.configMatch) throw new Error("BOOTSTRAP_NOT_READY");

  const activeEmail = Session.getActiveUser().getEmail() || Session.getEffectiveUser().getEmail();
  if (!activeEmail || activeEmail.toLowerCase() !== BOOTSTRAP.ownerEmail.toLowerCase()) {
    throw new Error("OWNER_ACCOUNT_MISMATCH");
  }
  return bootstrap;
}

function provisionProjectionAuth(verifierSha256, expectedEnvironment) {
  const bootstrap = projectionManagementContext_(expectedEnvironment);
  if (typeof verifierSha256 !== "string" || !/^[a-f0-9]{64}$/.test(verifierSha256)) {
    throw new Error("PROJECTION_VERIFIER_INVALID");
  }

  PropertiesService.getScriptProperties().setProperties({
    VHDCHY_PROJECTION_SHARED_TOKEN_SHA256: verifierSha256,
    VHDCHY_PROJECTION_ENABLED: "false",
    VHDCHY_PROJECTION_AUTH_PROVISIONED_AT: new Date().toISOString()
  }, false);

  const projection = projectionHealth_();
  if (!projection.authConfigured || projection.enabled) throw new Error("PROJECTION_AUTH_PROVISION_READBACK_FAILED");

  return {
    ok: true,
    managementVersion: PROJECTION_MANAGEMENT_VERSION,
    environment: BOOTSTRAP.environment,
    bootstrap,
    projection
  };
}

function setProjectionEnabled(enabled, expectedEnvironment) {
  const bootstrap = projectionManagementContext_(expectedEnvironment);
  if (typeof enabled !== "boolean") throw new Error("PROJECTION_ENABLE_VALUE_INVALID");

  const before = projectionHealth_();
  if (enabled && !before.authConfigured) throw new Error("PROJECTION_AUTH_NOT_CONFIGURED");

  const props = PropertiesService.getScriptProperties();
  props.setProperty("VHDCHY_PROJECTION_ENABLED", enabled ? "true" : "false");
  props.setProperty(enabled ? "VHDCHY_PROJECTION_ENABLED_AT" : "VHDCHY_PROJECTION_DISABLED_AT", new Date().toISOString());

  const projection = projectionHealth_();
  if (projection.enabled !== enabled || (enabled && !projection.authConfigured)) {
    throw new Error("PROJECTION_ENABLE_READBACK_FAILED");
  }

  return {
    ok: true,
    managementVersion: PROJECTION_MANAGEMENT_VERSION,
    environment: BOOTSTRAP.environment,
    bootstrap,
    projection
  };
}

function projectionManagementHealth(expectedEnvironment) {
  const bootstrap = projectionManagementContext_(expectedEnvironment);
  return {
    ok: true,
    managementVersion: PROJECTION_MANAGEMENT_VERSION,
    environment: BOOTSTRAP.environment,
    bootstrap,
    projection: projectionHealth_()
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
    bootstrap,
    projection: projectionHealth_()
  });
}

function doPost(e) {
  const bootstrap = bootstrapHealth_();
  if (!bootstrap.authorized || !bootstrap.configMatch) {
    return json_({ ok: false, code: "BOOTSTRAP_NOT_READY", environment: BOOTSTRAP.environment });
  }

  const projection = projectionHealth_();
  if (!projection.authConfigured) {
    return json_({ ok: false, code: "PROJECTION_AUTH_NOT_CONFIGURED", environment: BOOTSTRAP.environment });
  }

  let body;
  try {
    body = JSON.parse(e && e.postData && e.postData.contents ? e.postData.contents : "{}");
  } catch (error) {
    return json_({ ok: false, code: "INVALID_JSON", environment: BOOTSTRAP.environment });
  }

  if (body.protocol !== PROJECTION_PROTOCOL || String(body.environment || "").toUpperCase() !== BOOTSTRAP.environment.toUpperCase()) {
    return json_({ ok: false, code: "PROJECTION_CONTRACT_MISMATCH", environment: BOOTSTRAP.environment });
  }
  if (!projectionTokenValid_(body.sharedToken)) {
    return json_({ ok: false, code: "PROJECTION_AUTH_FAILED", environment: BOOTSTRAP.environment });
  }
  if (!projection.enabled) {
    return json_({ ok: false, code: "PROJECTION_NOT_LIVE", environment: BOOTSTRAP.environment });
  }
  if (!Array.isArray(body.items) || body.items.length < 1 || body.items.length > PROJECTION_MAX_ITEMS) {
    return json_({ ok: false, code: "INVALID_PROJECTION_BATCH", environment: BOOTSTRAP.environment });
  }

  const lock = LockService.getScriptLock();
  if (!lock.tryLock(10000)) {
    return json_({ ok: false, code: "PROJECTION_BUSY", environment: BOOTSTRAP.environment });
  }

  try {
    const workbook = SpreadsheetApp.openById(BOOTSTRAP.projectionSpreadsheetId);
    const results = body.items.map(function(item) { return upsertProjectionItem_(workbook, item); });
    return json_({
      ok: results.every(function(result) { return result.ok === true; }),
      protocol: PROJECTION_PROTOCOL,
      environment: BOOTSTRAP.environment,
      results
    });
  } catch (error) {
    return json_({
      ok: false,
      code: "PROJECTION_WRITE_FAILED",
      environment: BOOTSTRAP.environment,
      message: String(error && error.message ? error.message : error).slice(0, 500)
    });
  } finally {
    lock.releaseLock();
  }
}

function projectionTokenValid_(token) {
  if (typeof token !== "string" || token.length < 32 || token.length > 256) return false;
  const expected = PropertiesService.getScriptProperties().getProperty("VHDCHY_PROJECTION_SHARED_TOKEN_SHA256") || "";
  const actual = sha256Hex_(token);
  if (expected.length !== actual.length || expected.length === 0) return false;
  let diff = 0;
  for (let i = 0; i < expected.length; i += 1) diff |= expected.charCodeAt(i) ^ actual.charCodeAt(i);
  return diff === 0;
}

function sha256Hex_(value) {
  const bytes = Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256, value, Utilities.Charset.UTF_8);
  return bytes.map(function(byte) {
    const normalized = byte < 0 ? byte + 256 : byte;
    return normalized.toString(16).padStart(2, "0");
  }).join("");
}

function upsertProjectionItem_(workbook, item) {
  if (!item || typeof item !== "object") return { ok: false, code: "INVALID_ITEM" };
  const sheetName = String(item.sheet || "");
  const config = PROJECTION_SHEETS[sheetName];
  if (!config) return { ok: false, code: "SHEET_NOT_ALLOWED", sheet: sheetName };
  const values = item.values;
  if (!values || typeof values !== "object" || Array.isArray(values)) {
    return { ok: false, code: "INVALID_VALUES", sheet: sheetName };
  }

  const sheet = workbook.getSheetByName(sheetName);
  if (!sheet) return { ok: false, code: "SHEET_NOT_FOUND", sheet: sheetName };
  const lastColumn = Math.max(1, sheet.getLastColumn());
  const headers = sheet.getRange(1, 1, 1, lastColumn).getDisplayValues()[0];
  const headerIndex = {};
  headers.forEach(function(header, index) { if (header) headerIndex[String(header)] = index; });
  if (headerIndex[config.keyHeader] === undefined) {
    return { ok: false, code: "KEY_HEADER_NOT_FOUND", sheet: sheetName, keyHeader: config.keyHeader };
  }

  const unknown = Object.keys(values).filter(function(key) { return headerIndex[key] === undefined; });
  if (unknown.length) return { ok: false, code: "UNKNOWN_COLUMNS", sheet: sheetName, columns: unknown.slice(0, 10) };
  const keyValue = values[config.keyHeader];
  if (keyValue === null || keyValue === undefined || String(keyValue) === "") {
    return { ok: false, code: "KEY_VALUE_REQUIRED", sheet: sheetName, keyHeader: config.keyHeader };
  }

  const keyColumn = headerIndex[config.keyHeader] + 1;
  let targetRow = null;
  if (sheet.getLastRow() >= 2) {
    const finder = sheet.getRange(2, keyColumn, sheet.getLastRow() - 1, 1)
      .createTextFinder(String(keyValue))
      .matchEntireCell(true)
      .findNext();
    targetRow = finder ? finder.getRow() : null;
  }

  const row = targetRow
    ? sheet.getRange(targetRow, 1, 1, lastColumn).getValues()[0]
    : new Array(lastColumn).fill("");
  Object.keys(values).forEach(function(key) { row[headerIndex[key]] = values[key]; });

  if (targetRow) {
    sheet.getRange(targetRow, 1, 1, lastColumn).setValues([row]);
    return { ok: true, status: "UPDATED", sheet: sheetName, key: String(keyValue) };
  }

  sheet.appendRow(row);
  return { ok: true, status: "APPENDED", sheet: sheetName, key: String(keyValue) };
}

function json_(obj) {
  return ContentService.createTextOutput(JSON.stringify(obj)).setMimeType(ContentService.MimeType.JSON);
}
