# LAN / APK DEV BUILD STATUS

Updated: 2026-09-13
Status: BUILD PASS / PHYSICAL REGRESSION PENDING
Authority: current repository contracts and `docs/LAN_TRANSPORT_BETA_V1.md`
Legacy reference: `tamnv2supra/vanhanhdchungyen@7b4488a89f585812c1bccba5d07d86049482bf4c` (NON_AUTHORITY)

## Owner scope

Android/APK build and LAN Agent/model development are active in parallel with Service/Google/Web work. They must not be paused merely because the final company-laptop/network regression environment is unavailable.

The Owner has an Android device available for APK installation/testing. Final corporate-network LAN evidence remains a separate later gate.

## Current source

### Android transport pilot

Path: `android-pilot/`

Current package: `vn.vhdchy.transport.beta`.

Current purpose is transport-only validation:
- cached Agent health -> UDP discovery;
- two-sample activation hysteresis;
- `LAN_AVAILABLE`, `LAN_ACTIVE`, `LAN_LOST`, `LOCAL_QUEUE_ONLY` state behavior;
- durable local SQLite transport-test queue;
- stable `deviceId + deviceSeq + idempotencyKey` for each queued test event;
- echo/latency test;
- retry of queued transport-test events.

The current DEV pilot explicitly does not send business credentials, employee PII or canonical business mutations over LAN.

### Windows LAN Agent

Path: `lan-agent/`.

Current model:
- portable .NET 8 user-mode process;
- HTTP `17891`;
- UDP discovery `17892`;
- persistent per-user Agent instance ID;
- fresh `streamEpoch` per Agent start;
- health/echo transport-test endpoints;
- durable test-event receipt log under the normal user's LocalAppData;
- idempotency payload-collision guard;
- `(deviceId, deviceSeq)` collision guard;
- no Administrator/router/DNS/firewall bypass logic.

Agent test ACK is intentionally `TEST_ACCEPTED_AGENT_ONLY`. It is not a canonical business ACK and must never be reused as permission to delete future business commands.

## CI build

Workflow: `.github/workflows/build-lan-dev.yml`.

First parallel build run: `34756569016` — SUCCESS.
Source commit: `bd3e3f379fc954c781f37ede958bed53b961c0a8`.

Both jobs passed:
- `lan-agent`: SUCCESS; portable self-contained win-x64 Agent built and uploaded;
- `android-pilot`: SUCCESS; installable debug APK built and uploaded.

DEV artifacts:
- artifact `lan-agent-dev`, id `10317647559`, artifact digest `sha256:a3ba4fa6b1394d62c1c03bf8717841e424addb92afff4c32e94d621673425c6a`;
- artifact `android-transport-dev`, id `10317492785`, artifact digest `sha256:4eb427c8f6ce4c5306cc95f037b53d1c30882839067edeaa3cd91e4c4999d6a5`.

Contained files:
- `VHDCHY-Transport-BETA-debug.apk`;
- `VHDCHY-Transport-BETA-debug.apk.sha256`;
- `VHDCHY-LAN-Agent-DEV-win-x64.zip`;
- `VHDCHY-LAN-Agent-DEV-win-x64.zip.sha256`.

This DEV workflow uses no provider mutation and no Android release-signing secret. Release/BETA signing remains a later gate after transport source/build and physical behavior stabilize.

## Acceptance boundary

CI BUILD PASS proves source/build/package viability only. It does not prove current physical LAN feasibility.

The available Android device can now be used for APK install/open/offline-queue checks. LAN discovery/echo/end-to-end testing additionally needs a machine running the matching Agent. Final LAN PASS still requires later evidence against the actual company laptop/network plus the intended PDA environment.
