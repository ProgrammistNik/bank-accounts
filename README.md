# Описание проекта
Микросервис для управления банковскими счетами розничных клиентов, разработанный в рамках стажировки в Модульбанк на C#

### 🚀 Быстрый старт
- через Docker

```bash
  docker compose up --build
```
- Локально

```bash
  dotnet run
```
Swagger: `http://0.0.0.0:8080/swagger/index.html`

## Получение JWT токена

Чтобы получить JWT токен, необходимо выполнить POST-запрос на эндпоинт:

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

## Проверка Hangfire
Панель мониторинга Hangfire: `http://0.0.0.0:8080/hangfire`
