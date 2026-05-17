from airflow import DAG
from airflow.operators.python import PythonOperator
from airflow.providers.postgres.hooks.postgres import PostgresHook
from airflow.providers.http.hooks.http import HttpHook
from datetime import datetime, timedelta
import pandas as pd
import logging
import json
from clickhouse_driver import Client

default_args = {
    'owner': 'bionicpro',
    'depends_on_past': False,
    'start_date': datetime(2026, 5, 1),
    'retries': 1,
    'retry_delay': timedelta(minutes=5),
}

dag = DAG(
    'bionicpro_etl_report_v1',
    default_args=default_args,
    description='ETL for report fact table',
    schedule_interval='@hourly',
    catchup=False,
)

def extract_crm_clients(**context):
    """Extract client data from CRM (Bitrix24 REST API)"""
    hook = HttpHook(method='GET', http_conn_id='bitrix24')
    endpoint = '/crm.contact.list.json'
    params = {
        'select[]': ['ID', 'NAME', 'LAST_NAME', 'EMAIL', 'UF_PROSTHESIS_ID'],
        'filter[>ID]': 0,
        'order[ID]': 'ASC'
    }
    response = hook.run(endpoint, data=params)
    users_data = response.json().get('result', [])
    context['ti'].xcom_push(key='crm_users', value=users_data)
    logging.info(f"Extracted {len(users_data)} users from CRM")

def extract_telemetry_postgres(**context):
    """Extract telemetry data from PostgreSQL for the last hour"""
    pg_hook = PostgresHook(postgres_conn_id='operational_pg')
    start_time = (datetime.now() - timedelta(hours=1)).strftime('%Y-%m-%d %H:00:00')
    end_time = datetime.now().strftime('%Y-%m-%d %H:00:00')
    sql = f"""
        SELECT
            user_id,
            prosthesis_id,
            date_trunc('hour', timestamp) AS hour,
            COUNT(*) as total_signals,
            AVG(signal_strength) as avg_signal_strength,
            COUNT(DISTINCT movement_type) as movement_count,
            SUM(CASE WHEN error_code IS NOT NULL THEN 1 ELSE 0 END) as error_count,
            COUNT(DISTINCT EXTRACT(MINUTE FROM timestamp)) as active_minutes
        FROM telemetry
        WHERE timestamp >= '{start_time}' AND timestamp < '{end_time}'
        GROUP BY user_id, prosthesis_id, hour
    """
    df = pg_hook.get_pandas_df(sql)
    context['ti'].xcom_push(key='telemetry_df', value=df.to_json())
    logging.info(f"Extracted {len(df)} rows from telemetry")

def transform_and_merge(**context):
    """Join CRM and telemetry, compute aggregates"""
    import json
    users = context['ti'].xcom_pull(key='crm_users', task_ids='extract_crm_clients')
    telemetry_json = context['ti'].xcom_pull(key='telemetry_df', task_ids='extract_telemetry_postgres')
    if not users or not telemetry_json:
        logging.warning("No data from sources, skipping transform")
        return
    df_tele = pd.read_json(telemetry_json)
    df_users = pd.DataFrame(users)
    df_users.rename(columns={
        'ID': 'user_id',
        'NAME': 'user_name',
        'EMAIL': 'user_email',
        'UF_PROSTHESIS_ID': 'prosthesis_id'
    }, inplace=True)
    df_users['user_id'] = df_users['user_id'].astype(str)
    df_fact = df_tele.merge(df_users, on='user_id', how='left')
    df_fact['user_name'] = df_fact['user_name'].fillna('unknown')
    df_fact['user_email'] = df_fact['user_email'].fillna('unknown@example.com')
    df_fact['date'] = pd.to_datetime(df_fact['hour']).dt.date
    df_fact['hour'] = pd.to_datetime(df_fact['hour']).dt.hour
    df_fact = df_fact[[
        'user_id', 'user_email', 'user_name', 'prosthesis_id',
        'date', 'hour', 'total_signals', 'avg_signal_strength',
        'movement_count', 'error_count', 'active_minutes'
    ]]
    context['ti'].xcom_push(key='fact_data', value=df_fact.to_json())

def load_to_clickhouse(**context):
    import json
    from clickhouse_driver import Client

    fact_json = context['ti'].xcom_pull(key='fact_data', task_ids='transform_and_merge')
    if not fact_json:
        logging.info("No transformed data to load")
        return
    df = pd.read_json(fact_json)
    ch_client = Client(
        host='clickhouse',
        port=9000,
        user='default',
        password='',
        database='bionicpro'
    )
    for _, row in df.iterrows():
        delete_sql = f"ALTER TABLE bionicpro.report_fact DELETE WHERE user_id = '{row['user_id']}' AND date = '{row['date']}' AND hour = {row['hour']}"
        ch_client.execute(delete_sql)

        insert_sql = f"""
            INSERT INTO bionicpro.report_fact
            (user_id, user_email, user_name, prosthesis_id, date, hour,
             total_signals, avg_signal_strength, movement_count, error_count, active_minutes, etl_updated_at)
            VALUES (
                '{row['user_id']}', '{row['user_email']}', '{row['user_name']}', '{row['prosthesis_id']}',
                '{row['date']}', {row['hour']}, {row['total_signals']}, {row['avg_signal_strength']},
                {row['movement_count']}, {row['error_count']}, {row['active_minutes']}, now()
            )
        """
        ch_client.execute(insert_sql)
    logging.info(f"Loaded {len(df)} rows into ClickHouse")

with dag:
    extract_crm = PythonOperator(task_id='extract_crm_clients', python_callable=extract_crm_clients)
    extract_telemetry = PythonOperator(task_id='extract_telemetry_postgres', python_callable=extract_telemetry_postgres)
    transform = PythonOperator(task_id='transform_and_merge', python_callable=transform_and_merge)
    load = PythonOperator(task_id='load_to_clickhouse', python_callable=load_to_clickhouse)
    [extract_crm, extract_telemetry] >> transform >> load
    