# LAN Service CI validation contract

This file records the current aggregate/dedicated CI expectations for the LAN reconciliation slice.

- A default LAN runtime with no Cloud reconciliation credential configured must start fail-closed for outbound reconciliation transport.
- `/api/v1/capabilities` must expose `CLOUD_RECONCILIATION_HMAC_V1`.
- With no complete endpoint/key configuration, capabilities must expose `CLOUD_RECONCILIATION_NETWORK_SENDER_NOT_CONFIGURED`.
- The deprecated planning marker `CLOUD_RECONCILIATION_TRANSPORT_PLANNED` is no longer the current contract.
- Aggregate product-foundation smoke tests and the dedicated Cloud-sync harness must both stay green against the same reviewed source HEAD before this regression is considered closed.

This document changes no runtime behavior; it exists to keep the CI contract explicit and to prevent the stale-capability assertion from returning.