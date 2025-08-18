using System.Net;
using bank_accounts.Account.Enums;

namespace bank_accounts.Account.Exceptions;

public abstract class CustomExceptions(
    HttpStatusCode statusCode,
    string type,
    string title,
    string message)
    : Exception(message)
{
    public readonly HttpStatusCode StatusCode = statusCode;
    public readonly string Type = type;
    public readonly string Title = title;

    public class OwnerNotFoundException(Guid ownerId) : CustomExceptions(HttpStatusCode.NotFound,
        "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        "Владелец не найден",
        $"Владелец с таким id {ownerId} не найден");
    
    public class CurrencyDoesNotSupportedException(Currency currency) : CustomExceptions(
        HttpStatusCode.BadRequest,
        "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "Валюта не поддерживается",
        $"Валюта {currency} не поддерживается");
    
    public class AccountNotFoundException(Guid accountId) : CustomExceptions(HttpStatusCode.NotFound,
        "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        "Аккаунт не найден",
        $"Аккаунт с таким id {accountId} не найден");
    
    public class CheckingAccountNotSupportInterestRateException() : CustomExceptions(HttpStatusCode.BadRequest,
        "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "Аккаунт не поддерживает процентную ставку",
        "Аккаунт с типом Checking не поддерживает процентную ставку");
    
    public class AccountClosedException(Guid accountId) : CustomExceptions(HttpStatusCode.Conflict,
        "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        "Аккаунт закрыт",
        $"Аккаунт с таким id {accountId} закрыт или заморожен и не может быть использован для операций");
    
    public class InsufficientBalanceException(Guid accountId) : CustomExceptions(HttpStatusCode.Conflict,
        "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        "Недостаточно средств",
        $"На аккаунте с таким id {accountId} недостаточно средств для выполнения операции");
    
    public class CurriesDontMatchException() : CustomExceptions(HttpStatusCode.BadRequest,
        "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "Валюты не совпадают",
        "Валюты у счетов не совпадают"); 
    
    
    public class InvalidTransferException(string error) : CustomExceptions(HttpStatusCode.BadRequest,
        "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "Неверная операция перевода",
        $"Операция перевода не может быть выполнена из-за неверных данных: {error}"); 
    
    public class InvalidBalanceStateException(decimal balanceBefore, decimal balanceAfter) : CustomExceptions(HttpStatusCode.Conflict,
        "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        "Неверное состояние баланса",
        $"Баланс аккаунта изменился с {balanceBefore} на {balanceAfter} в процессе транзакции, что недопустимо");
    
    public class ConcurrencyConflictException(Guid accountId) : CustomExceptions(HttpStatusCode.Conflict,
        "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        "Конфликт параллельных изменений",
        $"Аккаунт {accountId} был изменен другим пользователем.");
}
