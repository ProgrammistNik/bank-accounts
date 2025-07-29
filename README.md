# Банковский сервис

## Технический стек 

- **Архитектура** — CQRS через MediatR;
- **Валидация** — FluentValidation;
- **Данные** — EF Core + SQLite;
- **Логика** — Изолирована в Features (Accounts, Transactions);
- **Инфраструктура** — Репозитории, вспомогательные сервисы;

## REST API

Swagger

## Управление счетами

### 1. Создание счёта
`POST /accounts`

Тело запроса:
```json
{
  "ownerId": "GUID",    
  "type": "Deposit",    
  "currency": "EUR",    
  "interestRate": 3.0,   
}
```
Ответ (201 Created) - ID созданного счёта.
Ответ (400 Bad Request) - ошибки валидации.
### 2. Изменение счёта
`PATCH /accounts/{id}`

Тело запроса:
```json
{
 "interestRate": null 
}
```
Ответ (200 Ok)
### 3. Удаление счёта

`DELETE /accounts/{id}`  

Ответ (200 Ok) - успешное удаление.

Ответ (400 BadRequest) - ошибки валидации.

Ответ (404 NotFound) - счёта не существует.

**Если требуется закрыть счёт:**

### 4. Закрытие счёта

`PATCH /accounts/{id}/close`

Ответ (200 Ok) - счёт закрыт.

Ответ (400 BadRequest) - ошибки валидации.

Ответ (404 NotFound) - счёта не существует.
### 5. Получение счетов

*По умолчанию возвращаются все счета, отсортированные по дате, требуется использовать фильтрацию*

```http
GET /accounts?ownerId=123&page=1&pageSize=20
```
```http
GET /accounts?accountIds=1,5,10&page=1&pageSize=10
```
```http
GET /accounts?ownerId=123&type=Deposit&currency=EUR&page=2&pageSize=50
```
Ответ (200 Ok):
```json
{
  "accounts": [
    {
      "id": "GUID",
      "ownerId": 123,
      "type": "Deposit",
      "currency": "EUR",
      "balance": 1000,
      "interestRate": null,
      "openingDate": "2025-03-12",
      "closingDate": null
    },
    {
      "id": "GUID",
      "ownerId": 123,
      "type": "Current",
      "currency": "USD",
      "balance": 500,
      "interestRate": null,
      "openingDate": "2025-03-10",
      "closingDate": null
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 35,
    "totalPages": 2
  }
}
```
Ответ (400 Bad Request) - ошибки валидации.

Ответ (404 Not Found) - счёта не существует.
### 6. Создание транзакции
`POST /transactions`

Тело запроса:
```json
{
	"accountId": "GUID",
	"counterpartyAccountId": null,
	"currency": "EUR",
	"value": 100,
	"type": "Credit", 
	"description": "",
	"date": "2025-03-12T11:30:19"
}
```
Ответ (201 Created)  - ID транзакции(ий)

Ответ (400 Bad Request) - ошибки валидации.

Ответ (404 Not Found) - транзакции не существует.
### 7.  Выполнение перевода между счетами
`POST /transactions`

Запрос:
```json
{
	"accountId": "GUID",
	"counterpartyAccountId": "GUID",
	"currency": "EUR",
	"value": 100,
	"type": "Credit",
	"description": "",
	"date": "2025-03-12T11:30:19"
}
```
Ответ (201 Created)  - ID транзакции(ий)

Ответ (400 Bad Request) - ошибки валидации.

Ответ (404 Not Found) - транзакции не существует.
### 8. Получение выписки
`GET /accounts/{accountId}/statement?start=2025-03-01&end=2025-03-31`

Ответ (200 Ok):
```json
{
  "accountId": "GUID",
  "ownerId": "GUID",
  "currency": "EUR",
  "startDate": "2025-03-01",
  "endDate": "2025-03-31",
  "openingBalance": 100.00,
  "closingBalance": 350.00,
  "transactions": [
    {
      "id": "GUID",
      "type": "Credit",
      "value": 200.00,
      "description": "Пополнение счёта",
      "date": "2025-03-05T14:30:00",
      "counterpartyAccountId": null
    },
    {
      "id": "GUID",
      "type": "Debit",
      "value": 50.00,
      "description": "Перевод в сбережения",
      "date": "2025-03-10T09:15:00",
      "counterpartyAccountId": 1
    }
  ],
  "totalCredits": 200.00,
  "totalDebits": 50.00
}
```
Ответ (400 Bad Request) - ошибки валидации.

Ответ (404 Not Found): счёта не существует.
### 9. Проверка счетов у клиента
`GET /accounts?ownerId=123`

Ответ (200 Ok):
```json
{
  "accounts": [
    {
      "id": "GUID",
      "ownerId": 123,
      "type": "Deposit",
      "currency": "EUR",
      "balance": 1000,
      "interestRate": null,
      "openingDate": "2025-03-12",
      "closingDate": null
    },
    {
      "id": "GUID",
      "ownerId": 123,
      "type": "Deposit",
      "currency": "USD",
      "balance": 500,
      "interestRate": null,
      "openingDate": "2025-03-10",
      "closingDate": null
    }
  ]
}
```
Ответ (400 Bad Request) - ошибки валидации.

Ответ (404 Not Found) - счёта не существует.
