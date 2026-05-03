# Задание 1. Повышение безопасности системы

*As-is :*

![as-is.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/as-is.png)

[as-is.drawio](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/as-is.drawio)

## Задача 1. Предложите архитектурное решение и доработайте диаграмму C4 для управления учётными данными пользователя. 

В текущую диаграмму необходимо добавить компоненты для управления доступом и отчетностью:

- API Gateway / BFF (проксирование, валидация сессий и выбор IdP)

- Auth Service (унификация запросов к различным внешним IdP)

- Report Aggregator (сервис для сборки отчетов из CRM и DB в ClickHouse).

- ClickHouse (хранилище для отчетов и аналитики)

![to-be_1.1.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/to-be_1.1.png)

[to-be.drawio](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/to-be.drawio)

## Задача 2. Улучшите безопасность существующего приложения, заменив Code Grant на PKCE

Новый [realm-export.json](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/keycloak/realm-export.json)

Также нужно скорректировать код приложения [App.tsx](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/frontend/src/App.tsx):

```
const keycloakConfig: KeycloakConfig = {
  url: process.env.REACT_APP_KEYCLOAK_URL,
  realm: process.env.REACT_APP_KEYCLOAK_REALM||"",
  clientId: process.env.REACT_APP_KEYCLOAK_CLIENT_ID||"",
  pkceMethod: 'S256'
};
```

## Задача 3. Обеспечьте безопасное получение и хранение access-и refresh-токенов

Бэкенд добавлен, его реализация на ПЯВУ C# расположен в директории `/bionicpro-auth`:

```
cd bionicpro-auth
dotnet build
dotnet run --urls "https://localhost:5001;http://localhost:5000"
```

```
docker run -p 8080:8080 -e KEYCLOAK_ADMIN=admin -e KEYCLOAK_ADMIN_PASSWORD=admin quay.io/keycloak/keycloak:26.0.0 start-dev
```