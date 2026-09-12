# ARCHITECTURE

## Service path

```text
PDA / Web
   -> Cloudflare Worker
      -> D1 canonical structured data
      -> Google Gateway (Apps Script)
         -> Google Sheets projection
         -> Google Drive runtime artifacts
```

## Environment mapping

| Component | BETA | STABLE |
|---|---|---|
| Host | `beta.supra.cc.cd` | `supra.cc.cd` |
| Worker intent | `vhdchy-beta` | `vhdchy-stable` |
| D1 intent | `vhdchy-data-beta` | `vhdchy-data-stable` |
| Drive root | `01_BETA` | `02_STABLE` |
| GAS | isolated project | isolated project |
| Signing | isolated signer | isolated signer |

## Data rules

- D1 is canonical authority for structured business state.
- Google Sheets is asynchronous projection/reconciliation/DR.
- Projection failure must not invalidate a committed D1 business transaction.
- Business mutation is event-oriented, idempotent and correction-based rather than raw history rewrite.

## LAN

LAN is a parallel resilience/local-transport path, not a replacement for Service authority. It must support no-admin operation, local durable queueing and automatic reacquisition without assuming control of company router/firewall/DNS.

Physical LAN work resumes only when Owner is on-site.
