import psycopg2
from psycopg2 import sql
import os

DATABASE_URL = os.getenv("DATABASE_URL", "postgresql://user:pass@postgres:5432/bionicpro")

def upgrade():
    conn = psycopg2.connect(DATABASE_URL)
    cur = conn.cursor()
    
    cur.execute("""
        DO $$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                           WHERE table_name='telemetry' AND column_name='signal_strength') THEN
                ALTER TABLE telemetry ADD COLUMN signal_strength FLOAT;
            END IF;
        END
        $$;
    """)
    conn.commit()
    cur.close()
    conn.close()
    print("Migration completed successfully.")

if __name__ == "__main__":
    upgrade()