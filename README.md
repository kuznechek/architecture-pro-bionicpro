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

