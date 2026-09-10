# PICK PACK 1291 — SHEETS MODEL REFERENCE / VHDCHY ADAPTATION

Status: `ADAPTED`
Date: 2026-09-10

## Provenance

Source is the Owner-provided read-only `BACKUP PICK PACK 1291`, including `VHDCHY - THAM CHIẾU SCHEMA PICK PACK 1291 (CHỈ ĐỌC).xlsx` and historical `DỮ LIỆU THEO NGÀY.xlsx` variants.

This document preserves useful human-facing schema/catalog knowledge. It does not authorize migration of old rows or make the retired workbook a runtime dependency.

## New VHDCHY BETA workbook

- Title: `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`
- Spreadsheet ID: `14gHSgWXP2QvtPmBD3CzFt3vQ6AED2hPSm_LxMGpXAKg`
- Cluster folder ID: `1toB5gBne5-v15clBkdbL6O5CrxzPBmEs`
- Schema: `PP1291_SHEETS_BETA_V1`
- Time zone: `Asia/Ho_Chi_Minh`
- Authority: D1; Sheet is projection/human-readable surface.
- Old row migration: NONE.

## Tabs created

`00_CONTROL`, `Danh mục`, `DANH SÁCH PDA`, `DANH SÁCH USER PICK`, `DANH SÁCH BÀN PACK`, `DANH SÁCH USER PACK`, `DANH SÁCH NHÂN SỰ`, `DANH SÁCH TÀI KHOẢN`, `LỊCH SỬ NGHIỆP VỤ`, `RA - VÀO TRONG CA`, `THÔNG TIN USER CỦA NLĐ`, `CÔNG NHẬT`, `Vị trí`, `Nhận hàng rớt`, `TÀI LIỆU`, `CONFLICT CORRECTION`, `IMPORT AUDIT`.

The retired `Danh sách Admin` is intentionally adapted to `DANH SÁCH TÀI KHOẢN`; password verifier/secret material is not projected to the human-readable Sheet.

Old LAN authority/emergency/fallback tabs are not recreated by default.

## Adapted human-facing headers

### DANH SÁCH PDA
`Seri PDA`, `5 số cuối Seri`, `Nguồn`, `Tình trạng`, `Ghi chú`

### DANH SÁCH USER PICK
`Số User`, `User Pick`, `Tình trạng`, `Ghi chú`

### DANH SÁCH BÀN PACK
`Tên bàn pack`, `Tình trạng`

### DANH SÁCH USER PACK
`Tên bàn pack`, `User pack`, `User Pack`, `Tình trạng`

### DANH SÁCH NHÂN SỰ
`Mã nhân viên`, `Họ và tên`, `Số điện thoại`, `Vị trí chính`, `Nhà cung cấp`, `Bộ phận`, `Site`, `Kho`, `Ngày bắt đầu làm việc`, `Ghi chú`, `Người cập nhật`, `Thời gian cập nhật`

### LỊCH SỬ NGHIỆP VỤ
`Ngày`, `Session ID`, `Mã nhân viên`, `Họ tên`, `Ca`, `Loại sự kiện`, `Nhãn sự kiện`, `Thời gian`, `Người xử lý`, `Chi tiết`, `Event ID`, `Phạm vi`, `App Revision`

### RA - VÀO TRONG CA
`Ngày`, `Ca`, `Mã nhân viên`, `Họ và tên`, `Số điện thoại`, `Nhà cung cấp`, `Bộ phận`, `Site`, `Kho`, `Vị trí chính`, `Vị trí trong ca`, `Seri PDA`, `User Pick`, `Bàn Pack`, `User Pack`, `Loại thao tác`, `Ghi chú`, `Người cập nhật`, `Thời gian cập nhật`, `Event ID`, `App action`, `App revision`

### THÔNG TIN USER CỦA NLĐ
`Ngày`, `Ca`, `Mã nhân viên`, `Họ và tên`, `Nhà cung cấp`, `Bộ phận`, `Site`, `Vị trí trong ca`, `User`, `Người cập nhật`, `Event ID`

### CÔNG NHẬT
`Ngày`, `Ca`, `Mã nhân viên`, `Họ và tên`, `Số điện thoại`, `Nhà cung cấp`, `Bộ phận`, `Site`, `Kho`, `Vị trí chính`, `Vị trí trong ca`, `Thông tin công nhật`, `Thời gian bắt đầu`, `Thời gian kết thúc`, `Mốc thời gian`, `Trạng thái`, `Ghi chú`, `Người cập nhật`, `Thời gian cập nhật`, `Event ID`, `Finish Event ID`, `App revision`, `Khấu trừ nhân sự`

### Vị trí
`Vị trí`

### Nhận hàng rớt
`Vị trí`, `Ngày`, `Scan QR`, `DO`, `Số kiện`, `Người cập nhật`, `Thời gian cập nhật`, `ID bản ghi`

## Reference catalogs currently copied to BETA

- Vị trí chính: Trưởng kho; Trưởng nhóm; Chuyên viên; Điều phối; Tổ trưởng; Kéo hàng; 5S; Phúc Long; Pick; Pack.
- Nhà cung cấp: Nguồn Lực Việt; Hoa Anh Đào; Việt Work; Man Power; Mega Link; Hà Gia Phát; Inhouse.
- Bộ phận: Pick Pack; Invent; Outbound; Inbound; Giao vận; Khác.
- Site: 1291; 1399; 1368; 1386; Khác.
- Kho: HY1; HY2.
- Tình trạng PDA: Nguyên vẹn; Vỡ màn hình; Không thể sử dụng.
- User Pick/Bàn Pack/User Pack: Khả dụng; Không khả dụng.
- Loại thao tác: VÀO; RA.
- Ca: Ca 1; Ca 2; Ca HC.
- Mốc thời gian: Trong ngày; Qua 24:00; Sau 24:00.
- Trạng thái công nhật: Đang làm; Hoàn thành.
- Nguồn PDA: 1291; 1386; 1368; 1399; Inbound; Outbound.
- Thông tin công nhật: Hỗ trợ 1399; Hỗ trợ 1386; Hỗ trợ 1368; Hỗ trợ Inbound; Hỗ trợ Outbound; Hỗ trợ Invent; Hỗ trợ scan lại vị trí trong pickface; Hỗ trợ 5S; Chờ kế hoạch; Hỗ trợ Pick; Hỗ trợ Pack; Hỗ trợ thay thế điều phối; Hỗ trợ Pick Pack Phúc Long; Khác.

## Projection rule

The Sheet is not a multi-writer transaction database. Canonical mutation belongs to Service/D1. Projection uses a controlled one-writer/batch outbox path with idempotency, retry, ACK and checkpoint semantics. Closed-quarter corrections must be additive/reference-based rather than destructive history rewrite.
