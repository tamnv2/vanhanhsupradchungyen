# VHDCHY OAuth branding site

Purpose: public Google OAuth branding pages for `supra.cc.cd`.

Required URLs:

- `https://vhdchy.supra.cc.cd/`
- `https://vhdchy.supra.cc.cd/privacy`
- `https://vhdchy.supra.cc.cd/terms`

Source: `oauth-branding/worker.js`.

## Deployment model

Use a dedicated Cloudflare Worker so this public branding site is isolated from the retained VHDCHY BETA/STABLE runtime Workers.

Suggested Worker name: `vhdchy-oauth-info`.

In Cloudflare Dashboard:

1. Workers & Pages -> Create -> Worker.
2. Create a simple Worker and replace its code with `worker.js`.
3. Deploy.
4. Worker -> Settings -> Domains & Routes -> Add -> Custom Domain.
5. Enter `vhdchy.supra.cc.cd`.
6. Wait until the custom domain/certificate is Active.
7. Test the three URLs above in a private/incognito browser. All must return HTTP 200 without login.

Do not manually point `vhdchy.supra.cc.cd` at the existing `vhdchy-beta` or `vhdchy-stable` Worker.

No secret is required by this Worker. It serves public static policy/branding HTML only.

## Google Auth Platform values

After the URLs are live:

- Application home page: `https://vhdchy.supra.cc.cd/`
- Privacy policy: `https://vhdchy.supra.cc.cd/privacy`
- Terms of service: `https://vhdchy.supra.cc.cd/terms`
- Authorized domain: `supra.cc.cd`

The domain must remain verified in Google Search Console by the current project owner account.

## Permission consistency

The text intentionally reflects the current audited permission boundary:

- CI OAuth: Apps Script project/deployment management only.
- GAS runtime: designated Google Sheets projection + signed-in account email.
- No Gmail, Calendar, Contacts, Drive file-management or email-sending access at the current checkpoint.

If those permissions change later, update these public pages in the same change as the permission audit/runbook so the consent-screen disclosures remain accurate.
