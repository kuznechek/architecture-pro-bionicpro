# Задание 1. Повышение безопасности системы

## Задача 1. Предложите архитектурное решение и доработайте диаграмму C4 для управления учётными данными пользователя. 

As-is :

![as-is.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/as-is.png)

[as-is.drawio](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/as-is.drawio)

В текущую диаграмму необходимо добавить компоненты для управления доступом и отчетностью:

- API Gateway / BFF (проксирование, валидация сессий и выбор IdP)

- Auth Service (унификация запросов к различным внешним IdP)

- Report Aggregator (сервис для сборки отчетов из CRM и DB в ClickHouse).

- ClickHouse (хранилище для отчетов и аналитики)

![to-be.png](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/to-be.png)

[to-be.drawio](https://github.com/kuznechek/architecture-pro-bionicpro/blob/feature/src/to-be.drawio)