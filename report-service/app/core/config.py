from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    CLICKHOUSE_HOST: str = "clickhouse"
    CLICKHOUSE_PORT: int = 9000
    CLICKHOUSE_USER: str = "default"
    CLICKHOUSE_PASSWORD: str = ""
    CLICKHOUSE_DB: str = "default"
    MINIO_ENDPOINT: str = "minio:9000"
    MINIO_ACCESS_KEY: str = "minioadmin"
    MINIO_SECRET_KEY: str = "minioadmin"
    MINIO_BUCKET: str = "reports"
    MINIO_SECURE: bool = False
    CDN_BASE_URL: str = "http://localhost:9000/reports"

    class Config:
        env_file = ".env"

settings = Settings()