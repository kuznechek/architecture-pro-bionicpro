# Задание 1. Повышение безопасности системы

## Задача 1. Предложите архитектурное решение и доработайте диаграмму C4 для управления учётными данными пользователя. 

*As-is :*

![as-is.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/as-is.png)

[as-is.drawio](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/as-is.drawio)

В текущую диаграмму необходимо добавить компоненты для управления доступом и отчетностью:

- API Gateway / BFF (проксирование, валидация сессий и выбор IdP)

- Auth Service (унификация запросов к различным внешним IdP)

- Report Aggregator (сервис для сборки отчетов из CRM и DB в ClickHouse).

- ClickHouse (хранилище для отчетов и аналитики)

![to-be_1.1.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/to-be_1.1.png)

[to-be.drawio](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/to-be.drawio)

## Задача 2. Улучшите безопасность существующего приложения, заменив Code Grant на PKCE. 

Новый [realm-export.json](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/keycloak/realm-export.json.v1)

Также нужно скорректировать код приложения [App.tsx](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/frontend/src/App.tsx):

```
const keycloakConfig: KeycloakConfig = {
  url: process.env.REACT_APP_KEYCLOAK_URL,
  realm: process.env.REACT_APP_KEYCLOAK_REALM||"",
  clientId: process.env.REACT_APP_KEYCLOAK_CLIENT_ID||"",
  pkceMethod: 'S256'
};
```

## Задача 3 Безопасное получение и хранение токенов, сервис bionicpro-auth

Обновлённый файл [`realm-export.json`](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/keycloak/realm-export.json)

Бэкенд добавлен, его реализация на ПЯВУ C# расположен в директории `/bionicpro-auth`

*KeycloakService*

- Выполняет аутентификацию пользователя по паролю (grant_type=password).

- Обновляет access_token с помощью refresh_token.

- Запрашивает scope=openid для корректной работы userinfo endpoint.

- Получает информацию о пользователе и реализует выход.

**SessionMiddleware**

- Проверяет наличие и валидность сессионной cookie (session_id).

- Ротация сессии: при каждом успешном запросе генерируется новый sessionId, старый удаляется, cookie обновляется (предотвращение session fixation).

- Устанавливает cookie с флагами HttpOnly и Secure.

**AuthController**

Эндпоинты /login, /logout, /refresh, /user.

- При успешной аутентификации access_token и refresh_token шифруются с помощью TokenProtector и сохраняются в оперативной памяти (защищённое хранилище).

- Клиенту возвращается только сессионная cookie (токены не передаются).

- Автоматическое обновление истёкшего access_token через refresh_token без участия пользователя.

**TokenProtector**

- Использует Microsoft.AspNetCore.DataProtection для шифрования токенов перед сохранением в памяти сервера

**Фронтенд (React) – обновлён**

- Удалены все вызовы keycloak.login(), keycloak.token.

- Добавлена форма логина (username/password), отправляющая запрос на /api/auth/login с credentials: 'include'.

- Все защищённые запросы (например, к /api/reports) также используют credentials: 'include'

![bionic_pro_client_1.3.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/bionic_pro_client_1.3.png)

## Задача 4. LDAP

Развёрнут OpenLDAP в Docker:

```
openldap:
  image: osixia/openldap:1.5.0
  environment:
    LDAP_ORGANISATION: "BionicPRO"
    LDAP_DOMAIN: "bionicpro.com"
    LDAP_ADMIN_PASSWORD: "admin"
  volumes:
    - ./ldif/data:/var/lib/ldap
    - ./ldif/config:/etc/ldap/slapd.d
  ports:
    - "389:389"

```

Keycloak настройка федерации:

Добавлен провайдер ldap в User Federation:

Connection URL: ldap://openldap:389

Users DN: ou=people,dc=bionicpro,dc=com

Bind DN: cn=admin,dc=bionicpro,dc=com

Edit Mode: READ_ONLY

Создан маппер group-ldap-mapper для синхронизации групп → ролей Keycloak.

Пользователи из LDAP могут входить (пароли из LDAP), автоматически получают роли.

![ldap_1.4.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/ldap_1.4.png)

## Задача 5. Настройте MFA

- В requiredActions добавлен CONFIGURE_TOTP как defaultAction: true.

- Создан новый поток аутентификации browser with OTP, в котором auth-otp-form обязателен (REQUIRED).

- Установлены параметры OTP: otpPolicyType: totp, otpPolicyDigits: 6, otpPolicyPeriod: 30

При первом входе пользователь должен настроить Google Authenticator (отсканировать QR‑код). После этого при каждой попытке входа требуетcя ввести одноразовый пароль.

![mfa_1.5.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/mfa_1.5.png)

## Задача 6. Добавьте OAuth 2.0 от Яндекс ID

```
"identityProviders": [
  {
    "alias": "yandex",
    "providerId": "oidc",
    "enabled": true,
    "config": {
      "clientId": "6de04cea94fe4909b6d042739b22c193",
      "clientSecret": "f58db3773d144f1389f689c6c4da1d6a",
      "authorizationUrl": "https://oauth.yandex.ru/authorize",
      "tokenUrl": "https://oauth.yandex.ru/token",
      "userInfoUrl": "https://login.yandex.ru/info",
      "defaultScope": "",
      "syncMode": "FORCE"
    }
  }
]
```

```
"identityProviderMappers": [
  {
    "name": "Email importer",
    "identityProviderMapper": "oidc-attribute-importer",
    "config": { "claim": "email", "user.attribute": "email" }
  },
  {
    "name": "Login importer",
    "identityProviderMapper": "oidc-attribute-importer",
    "config": { "claim": "login", "user.attribute": "username" }
  }
]
```

![yandex_oauth_1.6.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/yandex_oauth_1.6.png)

Также добавлена кнопка на форму "Войти через Яндекс"

![auth_1.6.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/auth_1.6.png)

# Задание 2. Разработка сервиса отчётов

## Задача 1. Создать архитектуру решения для подготовки и получения отчётов

![to-be_2.1.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/to-be_2.1.png)

## Задача 2. Разработать Airflow DAG и настроить его на запуск по расписанию

`airflow-webserver`	предоставляет веб-интерфейс для мониторинга, запуска и отладки DAG

`airflow-scheduler` сканирует DAG-файлы, определяет, какие задачи нужно запустить и в какой момент

## Задача 3. Создайте бэкенд-часть приложения для API.

# Контейнеры проекта BionicPRO

| Имя сервиса | Образ | Порты (хост:контейнер) | Назначение |
|-------------|-------|------------------------|-------------|
| `postgres` | `postgres:15` | `5433:5432` | База данных Keycloak и операционная БД (сессии, телеметрия) |
| `postgres_airflow` | `postgres:15` | – | База метаданных Airflow |
| `keycloak` | `quay.io/keycloak/keycloak:26.0` | `8080:8080` | Сервер аутентификации и федерации (Realm `reports-realm`) |
| `openldap` | `osixia/openldap:1.5.0` | `389:389`, `636:636` | LDAP-сервер для федерации пользователей (другое представительство) |
| `phpldapadmin` | `osixia/phpldapadmin:latest` | `8090:80` | Веб-интерфейс управления LDAP |
| `bionicpro-auth` | собственный (`./bionicpro-auth`) | `5001:80` | Бэкенд BFF (C#) – аутентификация, сессии, прокси отчётов |
| `frontend` | собственный (`./frontend`) | `3000:80` | React-фронтенд (форма логина, отчёты) |
| `airflow-webserver` | собственный (`./airflow`) | `8081:8080` | Веб-интерфейс Apache Airflow для мониторинга ETL |
| `airflow-scheduler` | собственный (`./airflow`) | – | Планировщик задач Airflow |
| `airflow-init` | собственный (`./airflow`) | – | Инициализация базы данных Airflow и создание пользователя |
| `clickhouse` | `clickhouse/clickhouse-server:23.8` | `8123:8123`, `9000:9000` | Аналитическая OLAP-база (витрина отчётов) |
| `report-service` | собственный (`./report-service`) | `8082:8080` | Микросервис на FastAPI для генерации отчётов (JSON/PDF) |

![ps_2.2.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/ps_2.2.png)

## Задача 4. Реализуйте ограничение доступа к эндпоинту отчётности.

Доступ к отчёту по пользователю должен предоставляться только в отношении себя. 

## Задача 5. Добавьте в UI кнопку получения отчёта и вызова эндпоинта его генерации.

Кнопка добавлена в [ReportPage.tsx](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/frontend/src/components/ReportPage.tsx)

![success_auth_2.5.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/success_auth_2.5.png)

```
{
  "data": [
    {
      "user_name": "prothetic1",
      "prosthesis_id": "P001",
      "total_signals": 1250,
      "avg_strength": 0.78,
      "total_active_minutes": 360,
      "total_errors": 0
    }
  ]
}
```

Если база данных пуста, отчёт будет пустым (массив []).

# Задание 3. Снижение нагрузки на базу данных

## Компоненты

- **Minio** – S3-совместимое объектное хранилище (бакет `reports`).  
  Хранит готовые отчёты в виде файлов (JSON/PDF).

- **CDN (Nginx)** – прокси-сервер с включённым кэшированием (`proxy_cache`).  
  Стоит перед Minio, кэширует статические файлы отчётов. Повторные запросы к одному и тому же отчёту отдаются из кэша CDN, минуя Minio.

- **report-service** – микросервис генерации отчётов (FastAPI).  
  Получает `user_id` из заголовка `X-User-Id` (устанавливается `bionicpro-auth`).  
  Реализует логику:  
  - проверить наличие отчёта в Minio;  
  - если есть – вернуть ссылку на CDN;  
  - если нет – сгенерировать отчёт из ClickHouse, сохранить в Minio, затем вернуть ссылку на CDN.

## Пошаговый процесс

1. **Пользователь** через фронтенд (React) отправляет GET-запрос на `/api/reports/summary?from_date=...&to_date=...&format=...`. 
   Запрос идёт через `bionicpro-auth` (проксирование), который добавляет заголовок `X-User-Id`

2. **report-service** :
   - извлекает `user_id` из заголовка;
   - формирует ключ объекта в S3:  
     `reports/{user_id}/{from_date}_{to_date}.{format}`

3. **Проверка существования отчёта в Minio** :
   - Выполняется метод `head_object` (или `exists`) по ключу
   - **Если файл существует** → сервис генерирует прямую ссылку на CDN
   - **Если файл не существует** → переходим к генерации

4. **Генерация нового отчёта** :
   - report-service выполняет SQL-запрос к витрине ClickHouse (`report_fact`)
   - Полученные данные преобразуются в нужный формат (JSON или PDF)
   - Сгенерированный файл (в виде байтов) загружается в Minio по тому же клюу методом `put_object`
   - После успешной загрузки сервис возвращает клиенту **редирект** на CDN URL (как в п. 3).

5. **Раздача через CDN (Nginx)** :
   - Клиент следует редиректу и запрашивает файл по CDN-URL.
   - Nginx проверяет свой кэш (`proxy_cache`).  
     - Если файл уже есть в кэше – отдаёт его с заголовком `X-Cache-Status: HIT`. 
     - Если нет – запрашивает файл у Minio, сохраняет в кэш и отдаёт клиенту (заголовок `MISS`). 
   - CDN кэширует отчёты на заданное время (например, 1 час), после чего они запрашиваются у Minio повторно

## Преимущества подхода

- **Снижение нагрузки на ClickHouse** – отчёт генерируется только один раз (первый запрос).  
- **Быстрая выдача** – повторные запросы обслуживаются из CDN (и/или Minio) практически мгновенно.  
- **Масштабируемость** – Minio и CDN легко распределяются.  
- **Экономия ресурсов** – нет повторных тяжёлых SQL-запросов.

`http://localhost:9003/browser/reports/report_test.txt`

![report_test_3.1.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/report_test_3.1.png)

Получение файла:

![report_test_cdn_3.2.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/report_test_cdn_3.2.png)

## Задание 4. Повышение оперативности и стабильности работы CRM

- **Добавлены сервисы**:

| Компонент | Имя контейнера / сервиса | Порт (хост) |
|-----------|--------------------------|--------------|
| PostgreSQL (CRM) | `postgres_crm` | 5432 (внутренний) |
| Debezium | `debezium` | 8084 |
| Kafka | `kafka` | 9092 |
| ClickHouse | `clickhouse` | 8123 (HTTP), 9000 (native) |
| Zookeeper | `zookeeper` | 2181 |

- **Настройка PostgreSQL для CDC**:
  - Включён `wal_level = logical` (параметр командной строки в `docker-compose.yml`).
  - Добавлены слоты репликации для Debezium.

**Файлы конфигурации** (в репозитории):

- `debezium/crm-connector.json` – конфигурация Debezium Postgres Connector:
  - URL PostgreSQL: `postgres_crm:5432`
  - Отслеживаемые таблицы: `public.clients`, `public.prostheses`
  - Имя топиков: `crm.public.clients`, `crm.public.prostheses`
  - Формат сообщений: JSON с `before`/`after`, преобразование `ExtractNewRecordState`.

- `clickhouse/init/010_cdc.sql` – создание таблиц
- `clickhouse/init/020_dims.sql` – таблицы измерений
- `clickhouse/init/030_report_mart_v2.sql` – обновлённая витрина отчётов, которая использует измерения

**Запуск CDC**:

1. Запущены все сервисы (`docker-compose up -d`).
2. Коннектор Debezium зарегистрирован (через `init_cdc.sh` или вручную).

**Проверка**:

- Внесено изменение в `postgres_crm`

- Проверка списка коннекторов:

```
 curl.exe -s http://localhost:8084/connectors/crm-connector/status
```

```
{
  "name": "crm-connector",
  "connector": {
    "state": "RUNNING",
    "worker_id": "debezium:8083"
  },
  "tasks": [
    {
      "id": 0,
      "state": "RUNNING",
      "worker_id": "debezium:8083"
    }
  ]
}
```

**Проверка топика Kafka :**

**Команда для просмотра сообщений в топике **
```bash
docker exec -it kafka kafka-console-consumer --bootstrap-server localhost:9092 --topic crm.public.clients --from-beginning
```

```
{
  "before": null,
  "after": { "id": 1, "name": "Test User", "email": "test@example.com" },
  "op": "r"
}
```
[]()
[kafka-topic.txt](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/kafka-topic.txt)

**Проверка, что события дошли до ClickHouse ^**

```
http://localhost:8123/?query=SELECT%20count()%20AS%20cnt%20FROM%20reports.crm_user_raw%20FORMAT%20JSON
```

[clickhouse-payload.txt](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/clickhouse-payload.txt)

**Проверка таблицы протезов:**

```
http://localhost:8123/?query=SELECT%20*%20FROM%20reports.crm_prosthesis_dim%20FINAL%20WHERE%20user_id='user1'%20FORMAT%20JSON
```

```
{
  "data": [
    {
      "id": 100,
      "user_id": "user1",
      "model": "BionicPro X1 DIM OK 33333",
      "updated_at": "2026-05-13 12:30:45"
    }
  ]
}
```