from clickhouse_driver import Client
from core.config import settings

class ClickHouseClient:
    def __init__(self):
        self.client = Client(
            host=settings.CLICKHOUSE_HOST,
            port=settings.CLICKHOUSE_PORT,
            user=settings.CLICKHOUSE_USER,
            password=settings.CLICKHOUSE_PASSWORD,
            database=settings.CLICKHOUSE_DB
        )
    
    def execute(self, query, params=None):
        return self.client.execute(query, params)

ch_client = ClickHouseClient()