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
