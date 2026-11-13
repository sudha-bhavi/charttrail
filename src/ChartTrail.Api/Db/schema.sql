CREATE TABLE IF NOT EXISTS patients (
    id          text PRIMARY KEY,
    family_name text,
    given_name  text,
    gender      text,
    birth_date  date,
    resource    jsonb NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_patients_family ON patients (lower(family_name));
CREATE INDEX IF NOT EXISTS ix_patients_given  ON patients (lower(given_name));

CREATE TABLE IF NOT EXISTS encounters (
    id           text PRIMARY KEY,
    patient_id   text NOT NULL REFERENCES patients(id) ON DELETE CASCADE,
    class_code   text,
    type_display text,
    period_start timestamptz,
    period_end   timestamptz,
    resource     jsonb NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_encounters_patient ON encounters (patient_id, period_start);

CREATE TABLE IF NOT EXISTS medication_requests (
    id          text PRIMARY KEY,
    patient_id  text NOT NULL REFERENCES patients(id) ON DELETE CASCADE,
    code        text,
    display     text,
    status      text,
    authored_on timestamptz,
    resource    jsonb NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_medreq_patient ON medication_requests (patient_id, authored_on);

CREATE TABLE IF NOT EXISTS observations (
    id           text PRIMARY KEY,
    patient_id   text NOT NULL REFERENCES patients(id) ON DELETE CASCADE,
    category     text,
    code         text,
    display      text,
    value_number numeric,
    value_unit   text,
    value_text   text,
    effective_at timestamptz,
    resource     jsonb NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_obs_patient_code ON observations (patient_id, code, effective_at);