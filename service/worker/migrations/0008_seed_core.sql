PRAGMA foreign_keys = ON;

INSERT OR IGNORE INTO clusters(cluster_id, display_name, cluster_type, status, feature_flags_json)
VALUES ('PICK_PACK_1291', 'Pick Pack 1291', 'PICK_PACK', 'ACTIVE', '{}');

INSERT OR IGNORE INTO modules(module_id, display_name, status, config_json)
VALUES ('PICK_PACK', 'Pick Pack Operations', 'ACTIVE', '{}');

INSERT OR IGNORE INTO cluster_modules(cluster_id, module_id, status, config_json)
VALUES ('PICK_PACK_1291', 'PICK_PACK', 'ACTIVE', '{}');

INSERT OR IGNORE INTO module_domain_registry(module_id, domain_code, display_name, status) VALUES
  ('PICK_PACK', 'ATTENDANCE', 'Attendance and presence', 'ACTIVE'),
  ('PICK_PACK', 'WORK_SESSION', 'Work session and tasks', 'ACTIVE'),
  ('PICK_PACK', 'RESOURCE_ASSIGNMENT', 'Pick/Pack resource assignment', 'ACTIVE'),
  ('PICK_PACK', 'LABOR_SUPPORT', 'Công nhật / support work', 'ACTIVE'),
  ('PICK_PACK', 'DROPPED_GOODS', 'Nhận hàng rớt', 'ACTIVE'),
  ('PICK_PACK', 'DOCUMENT', 'Business documents', 'ACTIVE');

INSERT OR IGNORE INTO resource_type_catalog(resource_type_id, module_id, type_code, display_name, daily_reuse_policy, status) VALUES
  ('PICK_PACK:PDA', 'PICK_PACK', 'PDA', 'PDA', 'REUSABLE', 'ACTIVE'),
  ('PICK_PACK:USER_PICK', 'PICK_PACK', 'USER_PICK', 'User Pick', 'LOCK_AFTER_RELEASE_UNTIL_REISSUE', 'ACTIVE'),
  ('PICK_PACK:PACK_TABLE', 'PICK_PACK', 'PACK_TABLE', 'Bàn Pack', 'LOCK_AFTER_RELEASE_UNTIL_REISSUE', 'ACTIVE'),
  ('PICK_PACK:USER_PACK', 'PICK_PACK', 'USER_PACK', 'User Pack', 'LOCK_AFTER_RELEASE_UNTIL_REISSUE', 'ACTIVE');

INSERT OR IGNORE INTO labor_type_catalog(labor_type_id, cluster_id, module_id, labor_type_code, display_name, default_deduct_staff, status) VALUES
  ('PICK_PACK_1291:LABOR:HO_TRO_PICK', 'PICK_PACK_1291', 'PICK_PACK', 'HO_TRO_PICK', 'Hỗ trợ Pick', 0, 'ACTIVE'),
  ('PICK_PACK_1291:LABOR:HO_TRO_PACK', 'PICK_PACK_1291', 'PICK_PACK', 'HO_TRO_PACK', 'Hỗ trợ Pack', 0, 'ACTIVE'),
  ('PICK_PACK_1291:LABOR:KEO_HANG', 'PICK_PACK_1291', 'PICK_PACK', 'KEO_HANG', 'Kéo hàng', 0, 'ACTIVE'),
  ('PICK_PACK_1291:LABOR:KHAC', 'PICK_PACK_1291', 'PICK_PACK', 'KHAC', 'Khác', 0, 'ACTIVE');
