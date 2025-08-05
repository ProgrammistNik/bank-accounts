using System.Reflection;
using System.Text.Json.Serialization;
using bank_accounts.Account.Exceptions;
using bank_accounts.Account.Interfaces;
using bank_accounts.Account.PipelineBehaviors;
using bank_accounts.Account.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(o =>
{
    o.IncludeExceptionDetails = (_, _) => false;
    
    o.Map<ValidationException>(_ => new ProblemDetails
    {
        Title = "Ошибки валидации",
        Status = StatusCodes.Status400BadRequest,
        Detail = "Одна или несколько ошибок валидации",
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
    });
    
    o.Map<CustomExceptions.OwnerNotFoundException>(ex => new ProblemDetails
    {
        Type = ex.Type,
        Title = ex.Title,
        Status = (int)ex.StatusCode,
        Detail = ex.Message
    });
    
    o.Map<CustomExceptions.CurrencyDoesNotSupportedException>(ex => new ProblemDetails
    {
        Type = ex.Type,
        Title = ex.Title,
        Status = (int)ex.StatusCode,
        Detail = ex.Message
    });
    
    o.Map<CustomExceptions.AccountNotFoundException>(ex => new ProblemDetails
    {
        Type = ex.Type,
        Title = ex.Title,
        Status = (int)ex.StatusCode,
        Detail = ex.Message
    });
    
    o.Map<CustomExceptions.CheckingAccountNotSupportInterestRateException>(ex => new ProblemDetails
    {
        Type = ex.Type,
        Title = ex.Title,
        Status = (int)ex.StatusCode,
        Detail = ex.Message
    });
    
    o.Map<CustomExceptions.AccountClosedException>(ex => new ProblemDetails
    {
        Type = ex.Type,
        Title = ex.Title,
        Status = (int)ex.StatusCode,
        Detail = ex.Message
    });
    
    o.Map<CustomExceptions.InsufficientBalanceException>(ex => new ProblemDetails
    {
        Type = ex.Type,
        Title = ex.Title,
        Status = (int)ex.StatusCode,
        Detail = ex.Message
    });
    
    o.Map<CustomExceptions.CurriesDontMatchException>(ex => new ProblemDetails
    {
        Type = ex.Type,
        Title = ex.Title,
        Status = (int)ex.StatusCode,
        Detail = ex.Message
    });
    
    o.Map<CustomExceptions.InvalidTransferException>(ex => new ProblemDetails
    {
        Type = ex.Type,
        Title = ex.Title,
        Status = (int)ex.StatusCode,
        Detail = ex.Message
    });
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));

builder.Services.AddAutoMapper(typeof(Program).Assembly);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddSingleton<IAccountService, AccountService>();
builder.Services.AddScoped<IClientVerifyService, ClientVerifyService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "bank-accounts", Version = "v1" });
    
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

app.UseProblemDetails();
app.UseMiddleware<ValidationExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("v1/swagger.json", "bank-accounts V1"));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
