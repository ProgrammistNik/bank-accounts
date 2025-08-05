using bank_accounts.Account.Commands;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Queries;
using bank_accounts.Account.MbResult;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace bank_accounts.Account.Controllers;
/// <summary>
/// Контроллер для управления банковскими счетами
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Создание нового банковского счёта
    /// </summary>
    /// <param name="accountDto">Данные для создания счёта</param>
    /// <response code="200">Счёт успешно создан</response>
    /// <response code="400">
    /// Возможные ошибки:
    /// - Некорректные данные запроса
    /// - Неверная операция перевода
    /// - Валюты не совпадают
    /// </response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Счет не найден</response>
    /// <response code="409">
    /// Возможные ошибки:
    /// - Аккаунт закрыт
    /// - Недостаточно средств 
    /// </response>
    [HttpPost("/account")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto accountDto)
    {
        var account = new CreateAccountCommand
        {
            OwnerId = accountDto.OwnerId,
            AccountType = accountDto.AccountType,
            Currency = accountDto.Currency,
            Balance = accountDto.Balance,
            InterestRate = accountDto.InterestRate
            
        };
        
        var result = await mediator.Send(account);
        return Ok(MbResult<ReturnAccountDto>.Success(result));
        
    }
    
    /// <summary>
    /// Закрытие банковского счёта
    /// </summary>
    /// <param name="accountId">Id банковского счета</param>
    /// <response code="200">Счёт успешно закрыт</response>
    /// <response code="400">Некорректные данные запроса</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Счет не найден</response>
    [HttpDelete("/account/{accountId:guid}")]
    public async Task<IActionResult> CloseAccount(Guid accountId)
    {
        var command = new CloseAccountCommand
        {
            AccountId = accountId
        };
        
        var result = await mediator.Send(command);
        return Ok(MbResult<ReturnAccountDto>.Success(result));
        
    }
    
    /// <summary>
    /// Получение списка банковских счетов клиента
    /// </summary>
    /// <param name="ownerId">Id владельца банковского счета</param>
    /// <response code="200"/>
    /// <response code="400">Некорректные данные запроса</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="409">
    /// Возможные ошибки:
    /// - Аккаунт закрыт
    /// - Недостаточно средств
    /// </response>
    [HttpGet("/accounts")]
    public async Task<IActionResult> GetUserAccounts(Guid ownerId)
    {
        var query = new GetUserAccountsQuery
        {
            OwnerId = ownerId
        };
        
        var result = await mediator.Send(query);
        return Ok(MbResult<IList<ReturnAccountDto>>.Success(result));
    }
    
    /// <summary>
    /// Изменение процентной ставки по счёту
    /// </summary>
    /// <param name="accountId">Id банковского счета</param>
    /// <param name="interestRate">Новая процентная ставка (от 0 до 100)</param>
    /// <response code="200"/>
    /// <response code="400">
    /// Возможные ошибки:
    /// - Некорректные данные запроса
    /// - Аккаунт не поддерживает процентную ставку
    /// </response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Счет не найден</response>
    /// <response code="409">Аккаунт закрыт</response>
    [HttpPatch("/account/{accountId:guid}/interest-rate")]
    public async Task<IActionResult> ChangeInterestRate(Guid accountId, decimal interestRate)
    {
        var command = new ChangeInterestRateCommand
        {
            AccountId = accountId,
            InterestRate = interestRate
        };
        
        var result = await mediator.Send(command);
        return Ok(MbResult<ReturnAccountDto>.Success(result));
    }
    
    /// <summary>
    /// Проверка существования банковского счёта
    /// </summary>
    /// <param name="accountId">Id банковского счета</param>
    /// <response code="200">Счёт существует</response>
    /// <response code="400">Некорректные данные запроса</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Счет не найден</response>
    [HttpGet("/accounts/{accountId:guid}")]
    public async Task<IActionResult> CheckIfAccountExists(Guid accountId)
    {
        var query = new CheckIfAccountsExistsQuery
        {
            AccountId = accountId
        };
        
        var result = await mediator.Send(query);
        return Ok(new { 
            Message = "Счёт существует",
            Result = MbResult<ReturnAccountDto>.Success(result) 
        });
    }
    
    /// <summary>
    /// Получение выписки по счёту
    /// </summary>
    /// <param name="accountId">Id банковского счета</param>
    /// <param name="from">Дата начала периода</param>
    /// <param name="to">Дата окончания периода</param>
    /// <response code="200"/>
    /// <response code="400">Некорректные данные запроса</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Счет не найден</response>
    [HttpGet("/account/{accountId:guid}/transactions")]
    public async Task<IActionResult> GetAccountStatement(Guid accountId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var query = new GetAccountStatementQuery
        {
            AccountId = accountId,
            From = from,
            To = to
        };
        var transactions = await mediator.Send(query);
        return Ok(MbResult<IList<ReturnTransactionDto>>.Success(transactions));
    }
    
}
