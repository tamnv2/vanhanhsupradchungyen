const UPDATED = '2026-09-11';

const BASE_CSS = `
  :root { color-scheme: light; font-family: Arial, Helvetica, sans-serif; }
  body { margin: 0; color: #202124; background: #fff; line-height: 1.6; }
  main { max-width: 860px; margin: 0 auto; padding: 48px 24px 72px; }
  h1 { font-size: 2rem; margin: 0 0 8px; }
  h2 { margin-top: 32px; }
  p, li { font-size: 1rem; }
  .meta { color: #5f6368; }
  nav { margin: 20px 0 32px; display: flex; gap: 18px; flex-wrap: wrap; }
  a { color: #0b57d0; }
  code { background: #f1f3f4; padding: 2px 5px; border-radius: 4px; }
  footer { margin-top: 48px; padding-top: 24px; border-top: 1px solid #dadce0; color: #5f6368; }
`;

function layout(title, body) {
  return `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>${title}</title>
  <style>${BASE_CSS}</style>
</head>
<body>
<main>
  ${body}
  <footer>
    <div>VHDCHY BETA Automation</div>
    <div>Contact: <a href="mailto:automation@supra.cc.cd">automation@supra.cc.cd</a></div>
    <div>Domain: supra.cc.cd</div>
  </footer>
</main>
</body>
</html>`;
}

function home() {
  return layout('VHDCHY BETA Automation', `
    <h1>VHDCHY BETA Automation</h1>
    <p class="meta">VẬN HÀNH DC HƯNG YÊN — BETA automation and integration service</p>
    <nav>
      <a href="/">Home</a>
      <a href="/privacy">Privacy Policy</a>
      <a href="/terms">Terms of Service</a>
    </nav>
    <h2>About this application</h2>
    <p>VHDCHY BETA Automation is an owner-operated automation application for the VẬN HÀNH DC HƯNG YÊN project. It is used to manage approved Google Apps Script deployment tasks and the designated Google Sheets projection used by the BETA environment.</p>
    <p>The application is not a public consumer service. Access is intended for the project owner and explicitly authorized project automation only.</p>
    <h2>Current Google access boundary</h2>
    <p>The current CI authorization is limited to Google Apps Script project and deployment management. The current Apps Script runtime is limited to the designated Google Sheets projection and the signed-in account email required for owner verification.</p>
    <p>The current application does not request Gmail, Calendar, Contacts, or Google Drive file-management access, and it does not send email.</p>
    <h2>Contact</h2>
    <p>For questions about this application or its use of Google APIs, contact <a href="mailto:automation@supra.cc.cd">automation@supra.cc.cd</a>.</p>
  `);
}

function privacy() {
  return layout('Privacy Policy — VHDCHY BETA Automation', `
    <h1>Privacy Policy</h1>
    <p class="meta">Last updated: ${UPDATED}</p>
    <nav>
      <a href="/">Home</a>
      <a href="/privacy">Privacy Policy</a>
      <a href="/terms">Terms of Service</a>
    </nav>
    <h2>Scope</h2>
    <p>This policy applies to VHDCHY BETA Automation, an owner-operated automation application for the VẬN HÀNH DC HƯNG YÊN project.</p>
    <h2>Google data and permissions</h2>
    <p>The current CI OAuth authorization is used only to manage the project owner's Google Apps Script project content, versions, and deployments. The current Apps Script runtime accesses only the designated Google Sheets projection and the signed-in Google account email used to verify the authorized owner.</p>
    <p>The application does not currently request or use Gmail, Calendar, Contacts, or Google Drive file-management permissions, and it does not use Google APIs to send email.</p>
    <h2>Use of data</h2>
    <p>Google-authorized data is used only to operate, verify, deploy, and maintain the VHDCHY project functions described above. It is not sold, rented, or used for advertising.</p>
    <h2>Credential handling</h2>
    <p>OAuth client credentials, refresh tokens, signing keys, passwords, and provider API tokens are treated as secrets and are stored only in approved provider secret stores or owner-controlled secure backups. They are not intentionally published on this website or committed to the public source repository.</p>
    <h2>Sharing</h2>
    <p>Google-authorized data is not shared with third parties except where technically necessary for the owner-approved infrastructure to operate, or where required by law.</p>
    <h2>Retention and deletion</h2>
    <p>Operational data and authorization material are retained only as required for the project. The project owner may revoke Google authorization at any time from the Google Account security and connected-app settings. Project credentials are rotated or removed when no longer required.</p>
    <h2>Contact</h2>
    <p>Privacy questions may be sent to <a href="mailto:automation@supra.cc.cd">automation@supra.cc.cd</a>.</p>
  `);
}

function terms() {
  return layout('Terms of Service — VHDCHY BETA Automation', `
    <h1>Terms of Service</h1>
    <p class="meta">Last updated: ${UPDATED}</p>
    <nav>
      <a href="/">Home</a>
      <a href="/privacy">Privacy Policy</a>
      <a href="/terms">Terms of Service</a>
    </nav>
    <h2>Purpose</h2>
    <p>VHDCHY BETA Automation is an internal, owner-operated automation component for the VẬN HÀNH DC HƯNG YÊN project. It is provided for authorized project operation, testing, deployment, and integration only.</p>
    <h2>Authorized use</h2>
    <p>Use is limited to the project owner and explicitly authorized project processes. Unauthorized access, credential reuse, abuse, spam, or attempts to use the service outside the approved project scope are prohibited.</p>
    <h2>No public service commitment</h2>
    <p>This BETA service may change, be suspended, or be replaced as the project architecture evolves. It is not offered as a public commercial service and carries no uptime or support commitment to external users.</p>
    <h2>Security</h2>
    <p>Users and operators must not expose OAuth tokens, provider API tokens, passwords, signing keys, or other secrets. Permissions must remain limited to the minimum capabilities required by current approved functionality.</p>
    <h2>Changes</h2>
    <p>These terms may be updated when project functionality or the Google API permission boundary changes. The current version is published at this URL.</p>
    <h2>Contact</h2>
    <p>Questions may be sent to <a href="mailto:automation@supra.cc.cd">automation@supra.cc.cd</a>.</p>
  `);
}

export default {
  async fetch(request) {
    const url = new URL(request.url);
    let html;
    if (url.pathname === '/' || url.pathname === '') html = home();
    else if (url.pathname === '/privacy' || url.pathname === '/privacy/') html = privacy();
    else if (url.pathname === '/terms' || url.pathname === '/terms/') html = terms();
    else return new Response('Not Found', { status: 404, headers: { 'content-type': 'text/plain; charset=utf-8' } });

    return new Response(html, {
      status: 200,
      headers: {
        'content-type': 'text/html; charset=utf-8',
        'cache-control': 'public, max-age=300',
        'x-content-type-options': 'nosniff',
        'referrer-policy': 'strict-origin-when-cross-origin',
        'content-security-policy': "default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'"
      }
    });
  }
};
