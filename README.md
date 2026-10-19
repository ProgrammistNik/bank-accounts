# Bank Accounts

Сервис управляет банковскими счетами и операциями по ним. Он позволяет открывать и закрывать счета, менять процентную ставку, выполнять переводы, регистрировать пополнения и списания, получать список счетов и выписку за период.

Основная задача сервиса — сохранить согласованность балансов и надёжно передать события о выполненных операциях. Изменение данных и создание сообщения outbox выполняются в одной транзакции PostgreSQL. Публикация в RabbitMQ происходит отдельно, поэтому временная недоступность брокера не приводит к потере события.

## Что реализовано

- создание расчётных, депозитных и кредитных счетов;
- закрытие счёта только при нулевом балансе;
- изменение процентной ставки для депозитных и кредитных счетов;
- переводы между счетами в одной валюте;
- отдельные операции пополнения и списания;
- выписка по счёту с фильтрацией по датам;
- ежедневное начисление процентов через Hangfire;
- transactional outbox для исходящих событий;
- idempotent inbox для antifraud-событий;
- блокировка и разблокировка счетов клиента через RabbitMQ;
- JWT-аутентификация через Keycloak;
- health checks для приложения, RabbitMQ и outbox;
- unit- и интеграционные тесты.

## Технологии

- .NET 9 и ASP.NET Core;
- Entity Framework Core и PostgreSQL;
- MediatR и FluentValidation;
- RabbitMQ;
- Hangfire;
- Keycloak;
- Docker Compose;
- NUnit, xUnit и Testcontainers.

## Устройство сервиса

HTTP-запрос поступает в контроллер и передаётся через MediatR в обработчик команды или запроса. Обработчик проверяет входные данные и вызывает доменный сервис. Все изменения счетов, транзакций и outbox-сообщений сохраняются через один `BankAccountsDbContext`.

При переводе обе строки счетов блокируются в PostgreSQL в одинаковом порядке. Это защищает баланс от потерянных обновлений и снижает вероятность взаимных блокировок при параллельных запросах. Валюта запроса должна совпадать с валютой обоих счетов. Замороженные и закрытые счета не участвуют в операциях.

`OutboxPublisher` выбирает до 100 неопубликованных сообщений с `FOR UPDATE SKIP LOCKED`, отправляет их с publisher confirms и отмечает опубликованными только после подтверждения RabbitMQ. Несколько экземпляров приложения могут обрабатывать outbox параллельно без выбора одной и той же строки.

`AntifraudConsumer` принимает события `Blocked` и `Unblocked`. Повторное событие определяется по `eventId` и не обрабатывается второй раз. Сообщения с неизвестным статусом или неподдерживаемой версией сохраняются в dead letter. Некорректный JSON отклоняется без повторной доставки.

## Быстрый запуск

Потребуются Docker и Docker Compose.

Создайте локальный файл с настройками:

```bash
cp .env.example .env
```

Замените значения `change-me` в `.env`, затем запустите сервисы:

```bash
docker compose up --build
```

После запуска доступны:

| Компонент | Адрес |
|---|---|
| API | `http://localhost:8080` |
| Swagger | `http://localhost:8080/swagger` |
| Liveness | `http://localhost:8080/api/health/live` |
| Readiness | `http://localhost:8080/api/health/ready` |
| Keycloak | `http://localhost:18081` |
| RabbitMQ Management | `http://localhost:15672` |
| Hangfire | `http://localhost:8080/hangfire` |

Swagger включён в окружении `Development`. Панель Hangfire требует авторизованный запрос.

Остановить сервисы можно командой:

```bash
docker compose down
```

Данные PostgreSQL и RabbitMQ сохраняются в Docker volumes. Для полного удаления локальных данных используйте `docker compose down -v`.

## Настройки окружения

| Переменная | Назначение |
|---|---|
| `POSTGRES_USER` | пользователь PostgreSQL |
| `POSTGRES_PASSWORD` | пароль PostgreSQL |
| `POSTGRES_DB` | имя базы данных |
| `RABBITMQ_USER` | пользователь RabbitMQ |
| `RABBITMQ_PASSWORD` | пароль RabbitMQ |
| `KEYCLOAK_ADMIN` | администратор Keycloak |
| `KEYCLOAK_ADMIN_PASSWORD` | пароль администратора Keycloak |
| `ACCOUNT_CLIENT_SECRET` | секрет клиента `AccountClient` |
| `KEYCLOAK_PORT` | локальный порт Keycloak |

Секреты не хранятся в репозитории. Файл `.env` исключён из Git.

## Получение JWT

Realm и клиент импортируются в Keycloak при первом запуске. Для получения токена выполните запрос, подставив значение `ACCOUNT_CLIENT_SECRET` из `.env`:

```bash
curl --request POST \
  --url http://localhost:18081/realms/BankAccount/protocol/openid-connect/token \
  --header 'Content-Type: application/x-www-form-urlencoded' \
  --data 'grant_type=client_credentials' \
  --data 'client_id=AccountClient' \
  --data 'client_secret=<client-secret>'
```

Значение `access_token` передаётся в защищённые методы API:

```text
Authorization: Bearer <access-token>
```

## HTTP API

| Метод | Путь | Назначение |
|---|---|---|
| `POST` | `/account` | открыть счёт |
| `DELETE` | `/account/{accountId}` | закрыть счёт с нулевым балансом |
| `GET` | `/accounts?ownerId={ownerId}` | получить счета клиента |
| `GET` | `/accounts/{accountId}` | проверить существование счёта |
| `PATCH` | `/account/{accountId}/interest-rate?interestRate={value}` | изменить процентную ставку |
| `GET` | `/account/{accountId}/transactions` | получить выписку по счёту |
| `POST` | `/transaction` | выполнить перевод между счетами |
| `POST` | `/incoming-outgoing-transaction` | зарегистрировать пополнение или списание |

Полные схемы запросов и ответов доступны в Swagger.

Ошибки возвращаются в формате Problem Details. Некорректные данные дают `400`, отсутствующий счёт — `404`, недостаточный баланс, закрытый или замороженный счёт и конфликт изменения — `409`.

## События RabbitMQ

Сервис публикует события в exchange `account.events`.

| Событие | Routing key |
|---|---|
| `AccountOpened` | `account.opened` |
| `MoneyCredited` | `money.credited` |
| `MoneyDebited` | `money.debited` |
| `TransferCompleted` | `money.transfer.completed` |
| `InterestAccrued` | `account.audit` |

Для изменения antifraud-статуса отправьте сообщение в `account.events` с routing key `client.blocked` или `client.unblocked`:

```json
{
  "eventId": "0e697796-6128-4c9c-8b47-9ddc01affd7b",
  "occurredAt": "2026-10-19T12:00:00Z",
  "payload": {
    "ownerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": "Blocked"
  },
  "meta": {
    "version": "v1",
    "source": "antifraud-service",
    "correlationId": "eb61ed5a-8f41-4717-8b39-0ecc0d606615",
    "causationId": "85b2b573-6ac9-455f-86e2-a98f8bf56b89"
  }
}
```

Допустимы только статусы `Blocked` и `Unblocked` и версия сообщения `v1`.

## Проверка проекта

Для локального запуска тестов нужен .NET SDK 9 и работающий Docker daemon:

```bash
dotnet test bank-accounts.sln
```

Unit-тесты проверяют основные правила работы со счетами и переводами. Интеграционные тесты поднимают отдельные PostgreSQL и RabbitMQ через Testcontainers и проверяют HTTP API, конкурентные переводы и повторную публикацию outbox после недоступности брокера.

Проверить конфигурацию Compose без запуска контейнеров можно командой:

```bash
docker compose config --quiet
```
