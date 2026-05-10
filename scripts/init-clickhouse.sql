CREATE DATABASE IF NOT EXISTS bionicpro;

CREATE TABLE IF NOT EXISTS bionicpro.report_fact (
    user_id          String,
    user_email       String,
    user_name        String,
    prosthesis_id    String,
    date             Date,
    hour             UInt8,
    total_signals    UInt64,
    avg_signal_strength Float64,
    movement_count   UInt64,
    error_count      UInt64,
    active_minutes   UInt16,
    etl_updated_at   DateTime DEFAULT now()
) ENGINE = SummingMergeTree()
ORDER BY (user_id, date, prosthesis_id);