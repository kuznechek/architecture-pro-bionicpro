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

## Задачи 3-5

Обновлённый файл [`realm-export.json`](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/keycloak/realm-export.json)

Бэкенд добавлен, его реализация на ПЯВУ C# расположен в директории `/bionicpro-auth`

*KeycloakService*

- Выполняет аутентификацию пользователя по паролю (grant_type=password).

- Обновляет access_token с помощью refresh_token.

- Запрашивает scope=openid для корректной работы userinfo endpoint.

- Получает информацию о пользователе и реализует выход.

*SessionMiddleware*

- Проверяет наличие и валидность сессионной cookie (session_id).

- Ротация сессии: при каждом успешном запросе генерируется новый sessionId, старый удаляется, cookie обновляется (предотвращение session fixation).

- Устанавливает cookie с флагами HttpOnly и Secure.

*AuthController*

Эндпоинты /login, /logout, /refresh, /user.

- При успешной аутентификации access_token и refresh_token шифруются с помощью TokenProtector и сохраняются в оперативной памяти (защищённое хранилище).

- Клиенту возвращается только сессионная cookie (токены не передаются).

- Автоматическое обновление истёкшего access_token через refresh_token без участия пользователя.

*TokenProtector*

- Использует Microsoft.AspNetCore.DataProtection для шифрования токенов перед сохранением в памяти сервера..

# Задание 2. Разработка сервиса отчётов

## Задача 1. Создать архитектуру решения для подготовки и получения отчётов.

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

Кнопка добавлена в [ReportPage.tsx](https://github.com/kuznechek/architecture-pro-bionicpro/frontend/src/components/ReportPage.tsx)
