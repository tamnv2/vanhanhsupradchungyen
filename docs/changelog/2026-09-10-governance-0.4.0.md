# governance-0.4.0 — AI continuity and project-scope operating model

Change ID: `GOV-20260910-01`
Time: 2026-09-10 Asia/Ho_Chi_Minh
Module: governance / continuity / project scope
Environment: repository authority only; no provider runtime deploy intended

## Reason

Owner yêu cầu dự án dài hạn phải kế thừa nhất quán qua nhiều chat, không dựa vào memory hội thoại; chạy song song việc độc lập; không đợi hard-limit khoảng 25 phút; lưu checkpoint có thể resume; changelog đầy đủ; và khóa rõ Pick Pack 1291 chỉ là nguồn tham khảo cho cluster 1291 trong dự án VHDCHY rộng hơn.

## Changes

- Added `PROJECT_SCOPE.md` as explicit scope authority.
- Added `AI_OPERATING_CONTRACT.md` with bootstrap, dependency, parallelism, soft-stop, checkpoint, token optimization and Owner-interaction rules.
- Reworked `AI_BOOTSTRAP.md` into a short 4-file bootstrap pointer.
- Added `NEXT_ACTIONS.md` and `SESSION_CHECKPOINT.md` for deterministic continuation.
- Added `DECISIONS_INDEX.md` for fast decision lookup.
- Appended D-010..D-014 in `DECISIONS.md`.
- Added governance/reference tasks to `TASK_LEDGER.md`.
- Added Pick Pack 1291 digest index under `docs/reference/pick-pack-1291/`.
- Changed future changelog model to root append-only index + immutable detailed records.

## Files/resources affected

Repository documentation/governance only. No Cloudflare, D1, GAS, Drive runtime data, Android signer or STABLE runtime mutation is part of this change.

## Migration

None.

## Tests / verification

- Pre-change BETA validation run `34495312672`: SUCCESS.
- Pre-change BETA deploy run `34495312746`: SUCCESS.
- Governance consistency verification required after commit: files exist, bootstrap pointers resolve, branch refs unchanged except `main` authority commit.

## Result

Expected: continuation becomes deterministic and reference scope is explicitly fenced.

## Rollback

Do not delete history. If this operating model needs revision, create a later governance decision/changelog entry that supersedes affected rules.

## Related decisions

D-010, D-011, D-012, D-013, D-014.

## Next impact

Run `RECONCILE-001` before expanding business schema/auth/client implementation.
