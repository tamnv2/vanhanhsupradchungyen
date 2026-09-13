PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS attendance_events (
  attendance_event_id TEXT PRIMARY KEY,
  employee_id TEXT NOT NULL,
  cluster_id TEXT NOT NULL,
  business_date TEXT NOT NULL,
  event_type TEXT NOT NULL CHECK (event_type IN ('IN','OUT')),
  occurred_at TEXT NOT NULL,
  actor_user_id TEXT,
  device_id TEXT,
  source TEXT NOT NULL DEFAULT 'APP',
  idempotency_key TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  ingested_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (employee_id) REFERENCES employees(employee_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (actor_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (device_id) REFERENCES device_registry(device_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_attendance_idempotency
ON attendance_events(idempotency_key) WHERE idempotency_key IS NOT NULL;

CREATE TRIGGER IF NOT EXISTS trg_attendance_no_update
BEFORE UPDATE ON attendance_events
BEGIN SELECT RAISE(ABORT, 'attendance_events are immutable'); END;

CREATE TRIGGER IF NOT EXISTS trg_attendance_no_delete
BEFORE DELETE ON attendance_events
BEGIN SELECT RAISE(ABORT, 'attendance_events are immutable'); END;

CREATE TABLE IF NOT EXISTS presence_state (
  employee_id TEXT PRIMARY KEY,
  current_state TEXT NOT NULL DEFAULT 'OUT' CHECK (current_state IN ('IN','OUT')),
  last_attendance_event_id TEXT,
  cluster_id TEXT,
  business_date TEXT,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  entity_version INTEGER NOT NULL DEFAULT 1 CHECK (entity_version >= 1),
  FOREIGN KEY (employee_id) REFERENCES employees(employee_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (last_attendance_event_id) REFERENCES attendance_events(attendance_event_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS extra_session_approvals (
  approval_id TEXT PRIMARY KEY,
  employee_id TEXT NOT NULL,
  cluster_id TEXT NOT NULL,
  business_date TEXT NOT NULL,
  reason TEXT NOT NULL,
  requested_by_user_id TEXT,
  approved_by_user_id TEXT,
  state TEXT NOT NULL DEFAULT 'PENDING' CHECK (state IN ('PENDING','APPROVED','REJECTED','REVOKED')),
  requested_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  decided_at TEXT,
  FOREIGN KEY (employee_id) REFERENCES employees(employee_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (requested_by_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (approved_by_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS work_sessions (
  session_id TEXT PRIMARY KEY,
  cluster_id TEXT NOT NULL,
  business_date TEXT NOT NULL,
  shift_definition_id TEXT,
  employee_id TEXT NOT NULL,
  session_kind TEXT NOT NULL DEFAULT 'MAIN' CHECK (session_kind IN ('MAIN','EXTRA')),
  approval_id TEXT,
  status TEXT NOT NULL DEFAULT 'OPEN' CHECK (status IN ('OPEN','CLOSED','CANCELLED','CORRECTED')),
  started_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ended_at TEXT,
  entity_version INTEGER NOT NULL DEFAULT 1 CHECK (entity_version >= 1),
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (shift_definition_id) REFERENCES shift_definitions(shift_definition_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (employee_id) REFERENCES employees(employee_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (approval_id) REFERENCES extra_session_approvals(approval_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_work_session_main_open
ON work_sessions(employee_id) WHERE status = 'OPEN' AND session_kind = 'MAIN';

CREATE INDEX IF NOT EXISTS ix_work_sessions_cluster_date
ON work_sessions(cluster_id, business_date, status);

CREATE TABLE IF NOT EXISTS session_tasks (
  task_id TEXT PRIMARY KEY,
  session_id TEXT NOT NULL,
  cluster_id TEXT NOT NULL,
  module_id TEXT NOT NULL,
  task_type TEXT NOT NULL CHECK (task_type IN ('PICK','PACK','OTHER')),
  status TEXT NOT NULL DEFAULT 'OPEN' CHECK (status IN ('OPEN','CLOSED','CANCELLED','CORRECTED')),
  started_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ended_at TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  entity_version INTEGER NOT NULL DEFAULT 1 CHECK (entity_version >= 1),
  FOREIGN KEY (session_id) REFERENCES work_sessions(session_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (module_id) REFERENCES modules(module_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_session_tasks_session
ON session_tasks(session_id, status, task_type);
