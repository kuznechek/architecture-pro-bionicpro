#!/bin/bash
set -e

# --- 1. Создание топиков в Kafka ---
echo "Creating Kafka topics..."
docker exec -it kafka kafka-topics --create --if-not-exists \
  --bootstrap-server localhost:9092 \
  --topic crm.public.clients --partitions 1 --replication-factor 1

docker exec -it kafka kafka-topics --create --if-not-exists \
  --bootstrap-server localhost:9092 \
  --topic crm.public.orders --partitions 1 --replication-factor 1

# --- 2. Регистрация Debezium коннектора ---
echo "Registering Debezium connector..."
curl -i -X POST http://localhost:8083/connectors/ \
  -H "Content-Type: application/json" \
  -d '{
    "name": "crm-connector",
    "config": {
      "connector.class": "io.debezium.connector.postgresql.PostgresConnector",
      "database.hostname": "postgres_crm",
      "database.port": "5432",
      "database.user": "crm_user",
      "database.password": "crm_pass",
      "database.dbname": "crm_db",
      "database.server.name": "crm",
      "plugin.name": "pgoutput",
      "table.include.list": "public.clients,public.orders",
      "key.converter": "org.apache.kafka.connect.json.JsonConverter",
      "value.converter": "org.apache.kafka.connect.json.JsonConverter",
      "transforms": "unwrap",
      "transforms.unwrap.type": "io.debezium.transforms.ExtractNewRecordState",
      "transforms.unwrap.drop.tombstones": "false"
    }
  }'

# --- 3. Создание витрины и материализованного представления в ClickHouse ---
echo "Creating ClickHouse tables and materialized views..."

# 3.1 Таблица-потребитель Kafka (raw данные)
docker exec -i clickhouse clickhouse-client --query "
CREATE TABLE IF NOT EXISTS crm.clients_queue (
    after String
) ENGINE = Kafka
SETTINGS kafka_broker_list = 'kafka:9092',
         kafka_topic_list = 'crm.public.clients',
         kafka_format = 'JSONEachRow',
         kafka_group_name = 'clickhouse_cdc';
"

# 3.2 Целевая витрина
docker exec -i clickhouse clickhouse-client --query "
CREATE TABLE IF NOT EXISTS crm.clients_fact (
    id UInt32,
    name String,
    email String,
    updated_at DateTime
) ENGINE = MergeTree()
ORDER BY id;
"

# 3.3 Материализованное представление для парсинга и вставки
docker exec -i clickhouse clickhouse-client --query "
CREATE MATERIALIZED VIEW IF NOT EXISTS crm.clients_mv TO crm.clients_fact AS
SELECT
    JSONExtractUInt(after, 'id') AS id,
    JSONExtractString(after, 'name') AS name,
    JSONExtractString(after, 'email') AS email,
    JSONExtractDateTime(after, 'updated_at') AS updated_at
FROM crm.clients_queue
WHERE after != '';
"

echo "CDC initialization completed."