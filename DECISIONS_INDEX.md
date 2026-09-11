# DECISIONS INDEX

Đây là index tra nhanh. Chi tiết authority nằm trong `DECISIONS.md` hoặc active decision record; không dùng index để thay thế nội dung decision đầy đủ khi có conflict.

| ID | Trạng thái | Tóm tắt |
|---|---|---|
| D-001 | ACTIVE | Repo PUBLIC để dùng hosted Actions; không commit secrets |
| D-002 | ACTIVE | BETA/STABLE tách resources/secrets |
| D-003 | ACTIVE | BETA `beta.supra.cc.cd`, STABLE `supra.cc.cd` |
| D-004 | ACTIVE | LAN hostname không public DNS |
| D-005 | ACTIVE | Worker public service, D1 structured/canonical store; DO theo use case, R2 OFF mặc định |
| D-006 | ACTIVE | Google Gateway/GAS + Drive/Sheets tách môi trường |
| D-007 | ACTIVE | STABLE cần Owner promotion/approval |
| D-008 | SUPERSEDED_BY_D010_D011 | Continuity authority ban đầu |
| D-009 | ACTIVE | Changelog append-only |
| D-010 | ACTIVE | VHDCHY là dự án chính; Pick Pack 1291 chỉ là cluster/reference có chọn lọc |
| D-011 | ACTIVE | Bootstrap tối thiểu + indexed inheritance để tối ưu token/time |
| D-012 | ACTIVE | Tranche khoảng 20 phút với soft stop + mandatory checkpoint |
| D-013 | ACTIVE | Dependency graph + parallel execution là mặc định |
| D-014 | ACTIVE | Detailed changelog record cho mọi thay đổi đáng kể |
| D-015 | ACTIVE | DC Core generic + cluster rollout; Pick Pack 1291 là cluster đầu tiên |
| D-016 | ACTIVE | Backup Pick Pack reuse có chọn lọc; không migrate/runtime dependency |
| D-017 | ACTIVE | Immutable event/idempotency/version/device sequence + correction as new event |
| D-018 | ACTIVE | D1 authority; Sheets projection/đối soát/DR qua outbox one-writer |
| D-019 | ACTIVE | Free-first; capacity quyết định bằng measurement/stress/soak |
| D-020 | ACTIVE | BETA/STABLE runtime isolation đầy đủ |
| D-021 | ACTIVE | Business data/mutation không anonymous trước auth |
| D-022 | ACTIVE | Google projection availability không chặn canonical D1 core |
| D-023 | ACTIVE | Pick Pack 1291 Sheet baseline ADAPTED; không old data/password verifier |
| D-024 | ACTIVE | Generic resource taxonomy; dropped-goods là module Pick Pack 1291 |
| D-025 | ACTIVE | LAN-PILOT priority gate; physical PDA + synthetic service load; PDA auto-LAN |
| D-026 | ACTIVE | Android/LAN Agent update có auto discovery + manual fallback + verify |
| D-027 | ACTIVE | LAN Agent lightweight background runtime + tray/settings; đo CPU/RAM thật |
| D-028 | ACTIVE | LAN pilot no-admin/minimum-information; portable user-mode, no router/DNS/firewall assumption; MT90 target |
| D-029 | ACTIVE | Hiện chỉ có 2 MT90: cho phép quyết định feasibility bằng 2 PDA thật + synthetic headroom, nhưng không được suy diễn RF/Wi-Fi >2 PDA |

Lưu ý: các ID D-010..D-017 từng xuất hiện trên nhánh BETA lịch sử trước governance không còn là decision namespace authority; nội dung hợp lệ đã được re-issued thành D-015..D-022.
