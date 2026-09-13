PRAGMA foreign_keys = ON;

INSERT OR REPLACE INTO vhdchy_meta(key, value, updated_at) VALUES
  ('domain_contract_version', 'VHDCHY_DOMAIN_V1', CURRENT_TIMESTAMP),
  ('permission_catalog_version', 'VHDCHY_PERMISSION_CATALOG_V1', CURRENT_TIMESTAMP),
  ('edge_reconciliation_model', 'edge_reconciliation_v1', CURRENT_TIMESTAMP);

CREATE TABLE IF NOT EXISTS edge_sources (
  edge_source_id TEXT PRIMARY KEY,
  environment TEXT NOT NULL,
  cluster_id TEXT NOT NULL,
  edge_instance_id TEXT NOT NULL,
  edge_epoch TEXT NOT NULL,
  domain_contract_version TEXT NOT NULL,
  edge_schema_version TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','BLOCKED','RETIRED','INCOMPATIBLE')),
  last_sync_cursor TEXT,
  last_sync_at TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE (environment, edge_instance_id, edge_epoch),
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_edge_sources_cluster_status
ON edge_sources(environment, cluster_id, status, updated_at);

CREATE TABLE IF NOT EXISTS edge_event_ingest (
  edge_event_id TEXT PRIMARY KEY,
  edge_source_id TEXT NOT NULL,
  request_id TEXT NOT NULL,
  idempotency_key TEXT NOT NULL,
  device_id TEXT,
  device_seq INTEGER CHECK (device_seq IS NULL OR device_seq > 0),
  command_code TEXT NOT NULL,
  event_type TEXT NOT NULL,
  entity_type TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  base_entity_version INTEGER CHECK (base_entity_version IS NULL OR base_entity_version >= 1),
  payload_hash TEXT NOT NULL,
  authority_snapshot_version TEXT NOT NULL,
  local_order INTEGER CHECK (local_order IS NULL OR local_order >= 1),
  local_accepted_at TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'RECEIVED' CHECK (status IN ('RECEIVED','RECONCILED','CONFLICT','REJECTED','RETRY_WAIT')),
  canonical_event_id TEXT,
  conflict_id TEXT,
  last_error_code TEXT,
  received_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (edge_source_id) REFERENCES edge_sources(edge_source_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (device_id) REFERENCES device_registry(device_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (canonical_event_id) REFERENCES domain_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (conflict_id) REFERENCES conflict_corrections(conflict_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_edge_ingest_source_idempotency
ON edge_event_ingest(edge_source_id, idempotency_key);

CREATE UNIQUE INDEX IF NOT EXISTS ux_edge_ingest_device_sequence
ON edge_event_ingest(edge_source_id, device_id, device_seq)
WHERE device_id IS NOT NULL AND device_seq IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_edge_ingest_status_order
ON edge_event_ingest(edge_source_id, status, local_order, received_at);

CREATE TABLE IF NOT EXISTS integration_receipts (
  receipt_id TEXT PRIMARY KEY,
  environment TEXT NOT NULL,
  cluster_id TEXT NOT NULL,
  edge_source_id TEXT,
  event_id TEXT,
  edge_event_id TEXT,
  target_kind TEXT NOT NULL CHECK (target_kind IN ('GOOGLE_SHEETS','GOOGLE_DRIVE')),
  logical_key TEXT NOT NULL,
  provider_ref TEXT,
  content_hash TEXT,
  checkpoint_ref TEXT,
  status TEXT NOT NULL DEFAULT 'COMPLETED' CHECK (status IN ('PENDING','COMPLETED','VERIFIED','ERROR')),
  completed_at TEXT,
  verified_at TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE (environment, target_kind, logical_key),
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (edge_source_id) REFERENCES edge_sources(edge_source_id) ON UPDATE CASCADE ON DELETE SET NULL,
  FOREIGN KEY (event_id) REFERENCES domain_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (edge_event_id) REFERENCES edge_event_ingest(edge_event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_integration_receipts_event
ON integration_receipts(event_id, edge_event_id, target_kind, status);

CREATE TABLE IF NOT EXISTS edge_sync_checkpoints (
  edge_source_id TEXT PRIMARY KEY,
  last_edge_event_id TEXT,
  last_local_order INTEGER CHECK (last_local_order IS NULL OR last_local_order >= 0),
  last_authority_snapshot_version TEXT,
  last_reconciled_at TEXT,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (edge_source_id) REFERENCES edge_sources(edge_source_id) ON UPDATE CASCADE ON DELETE CASCADE,
  FOREIGN KEY (last_edge_event_id) REFERENCES edge_event_ingest(edge_event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

INSERT OR IGNORE INTO auth_permissions(permission_id, resource_code, action_code, description, status) VALUES
  ('employee:view', 'employee', 'view', 'Xem nhân sự', 'ACTIVE'),
  ('employee:create', 'employee', 'create', 'Tạo nhân sự', 'ACTIVE'),
  ('employee:edit', 'employee', 'edit', 'Sửa nhân sự', 'ACTIVE'),
  ('employee:status', 'employee', 'status', 'Đổi trạng thái nhân sự', 'ACTIVE'),
  ('employee:portrait', 'employee', 'portrait', 'Đổi ảnh nhân sự', 'ACTIVE'),
  ('attendance:scan', 'attendance', 'scan', 'Quét VÀO/RA', 'ACTIVE'),
  ('attendance:correct', 'attendance', 'correct', 'Điều chỉnh VÀO/RA', 'ACTIVE'),
  ('session:open', 'session', 'open', 'Mở phiên làm việc', 'ACTIVE'),
  ('session:close', 'session', 'close', 'Đóng phiên làm việc', 'ACTIVE'),
  ('session.extra:approve', 'session.extra', 'approve', 'Duyệt phiên bổ sung', 'ACTIVE'),
  ('task.pick:operate', 'task.pick', 'operate', 'Thao tác PICK', 'ACTIVE'),
  ('task.pack:operate', 'task.pack', 'operate', 'Thao tác PACK', 'ACTIVE'),
  ('resource:view', 'resource', 'view', 'Xem tài nguyên', 'ACTIVE'),
  ('resource:assign', 'resource', 'assign', 'Cấp tài nguyên', 'ACTIVE'),
  ('resource:release', 'resource', 'release', 'Trả tài nguyên', 'ACTIVE'),
  ('resource:change', 'resource', 'change', 'Đổi tài nguyên', 'ACTIVE'),
  ('resource:reissue', 'resource', 'reissue', 'Phát lại tài nguyên', 'ACTIVE'),
  ('resource.borrow:request', 'resource.borrow', 'request', 'Yêu cầu mượn tài nguyên chéo cụm', 'ACTIVE'),
  ('resource.borrow:approve', 'resource.borrow', 'approve', 'Duyệt mượn tài nguyên chéo cụm', 'ACTIVE'),
  ('pack.mapping:manage', 'pack.mapping', 'manage', 'Quản lý mapping Bàn Pack - User Pack', 'ACTIVE'),
  ('labor:view', 'labor', 'view', 'Xem Công nhật', 'ACTIVE'),
  ('labor:start_finish', 'labor', 'start_finish', 'Start/Finish Công nhật', 'ACTIVE'),
  ('labor:correct', 'labor', 'correct', 'Điều chỉnh Công nhật', 'ACTIVE'),
  ('labor.catalog:manage', 'labor.catalog', 'manage', 'Quản lý danh mục Công nhật', 'ACTIVE'),
  ('dropped_goods:create', 'dropped_goods', 'create', 'Ghi nhận hàng rớt', 'ACTIVE'),
  ('dropped_goods:view', 'dropped_goods', 'view', 'Xem hàng rớt', 'ACTIVE'),
  ('document:create', 'document', 'create', 'Tạo biên bản/tài liệu', 'ACTIVE'),
  ('document:finalize', 'document', 'finalize', 'Chốt biên bản/tài liệu', 'ACTIVE'),
  ('document:view', 'document', 'view', 'Xem tài liệu/ảnh', 'ACTIVE'),
  ('account:manage', 'account', 'manage', 'Quản lý tài khoản', 'ACTIVE'),
  ('permission:manage', 'permission', 'manage', 'Phân quyền', 'ACTIVE'),
  ('conflict:resolve', 'conflict', 'resolve', 'Xử lý xung đột nghiệp vụ', 'ACTIVE'),
  ('history:view', 'history', 'view', 'Xem lịch sử', 'ACTIVE'),
  ('projection:admin', 'projection', 'admin', 'Quản lý trạng thái đồng bộ Google', 'ACTIVE'),
  ('import:run', 'import', 'run', 'Nhập dữ liệu', 'ACTIVE'),
  ('export:run', 'export', 'run', 'Xuất dữ liệu', 'ACTIVE'),
  ('system.settings:manage', 'system.settings', 'manage', 'Cài đặt cụm/tính năng', 'ACTIVE'),
  ('lan.routing:force', 'lan.routing', 'force', 'Chủ động ép tuyến LAN khi Cloud đang khỏe', 'ACTIVE');
