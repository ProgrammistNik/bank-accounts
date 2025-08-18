# bank-accounts
Задание стажировки C# в Модульбанк:
микросервис «Банковские счета», обслуживающий процессы розничного банка.

### Тестирование
```bash
    dotnet test
``` 
Важные замечания по тестированию:
 - Тесты запускаются в отдельном контейнере, поэтому их стоит выполнять до запуска приложения через Docker.
 - Интеграционные тесты нужно запускать отдельно друг от друга, не одновременно.

### Запуск проекта
- через Docker

```bash
  docker compose up --build
```
- Локально

```bash
  dotnet run
```
Документация Swagger будет доступна по адресу: 

`http://0.0.0.0:8080/swagger/index.html`

## Получение JWT токена

Для получения JWT токена необходимо выполнить POST-запрос на эндпоинт:

`http://localhost:8081/realms/BankAccount/protocol/openid-connect/token`


### Заголовки
- `Content-Type: application/x-www-form-urlencoded`

### Тело запроса (form-data или x-www-form-urlencoded):
| Параметр      | Значение           | Описание               |
|---------------|--------------------|------------------------|
| grant_type    | client_credentials | Тип получения токена   |
| client_id     | AccountClient      | Идентификатор клиента  |
| client_secret | my-secret          | Секрет клиента         |

### Пример ответа (успешный `HTTP 200 OK`):
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires_in": 300,
  "refresh_expires_in": 0,
  "token_type": "Bearer",
  "not-before-policy": 0,
  "scope": "profile email"
}
```

## Проверка работоспособности Hangfire
Панель мониторинга Hangfire расположена по адресу:

`http://0.0.0.0:8080/hangfire`

## RabbitMQ
Панель управления RabbitMQ доступна по адресу:

`http://localhost:15672/`

`admin` - логин
`password` - пароль

Пример сообщения для AntifraudConsumer:

`Routing Key: client.unblocked`

```json
 {
  "eventId": "0e697796-6128-4c9c-8b47-9ddc01affd7b",
  "occurredAt": "2025-08-15T12:00:00Z",
  "payload": {
    "ownerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": "Unblocked"
  },
  "meta": {
    "version": "v1",
    "source": "test-client",
    "correlationId": "eb61ed5a-8f41-4717-8b39-0ecc0d606615",
    "causationId": "85b2b573-6ac9-455f-86e2-a98f8bf56b89"
  }
}
```

Сообщение создается внутри панели управления RabbitMQ, в разделе "Exchanges" выберите `account.events`, затем перейдите в раздел "Publish message". Введите пример сообщения в формате JSON и нажмите "Publish message".

## Важное замечание по работе с приложением

При локальном запуске приложения, может возникать ошибка Npgsql.PostgresException : 28P01. Для решения проблемы может помочь изменение пароля в строке подключения к базе данных в файле `appsettings.json` на пароль от вашей локальной базы данных.
