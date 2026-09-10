# ARCHITECTURE FOUNDATION

## Luồng chính

```text
PDA / Web client
      |
      v
Cloudflare Worker
      |
      +--> D1 structured data
      +--> Durable Objects (khi cần lock/rate-limit/concurrency)
      |
      +--> Google Gateway (Apps Script)
                |
                +--> Google Sheets
                +--> Google Drive
```

## Environment mapping

| Thành phần | BETA | STABLE |
|---|---|---|
| Public hostname | `beta.supra.cc.cd` | `supra.cc.cd` |
| Worker | `vhdchy-beta` | `vhdchy-stable` |
| D1 | `vhdchy-data-beta` | `vhdchy-data-stable` |
| Apps Script | project riêng | project riêng |
| Drive runtime | root riêng | root riêng |
| Android signer | signer riêng | signer riêng |

## LAN

LAN hostname tồn tại ở tầng thiết kế nhưng không được publish lên Cloudflare DNS. Việc resolve/routing LAN sẽ dùng cơ chế nội bộ riêng khi triển khai tại DC.

## Nguyên tắc dữ liệu

- Không coi GitHub là runtime database.
- Không ghi dữ liệu nghiệp vụ động vào source repo.
- BETA không dùng chung D1/Drive runtime với STABLE.
- R2 chỉ bật khi có use-case cache/object storage đủ rõ.
