using System.Reflection;
using System.Text.Json.Serialization;
using bank_accounts.Account.Data;
using bank_accounts.Account.Exceptions;
using bank_accounts.Account.Interfaces;
using bank_accounts.Account.PipelineBehaviors;
using bank_accounts.Account.Services;
using bank_accounts.Hangfire;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true
        };
    });
builder.Services.AddAuthorization();

builder.Logging.AddConsole();

builder.Services.Configure<JwtBearerOptions>("Bearer", options =>
{
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"AUTH FAIL: {context.Exception}");
            return Task.CompletedTask;
        },
        OnTokenValidated = _ =>
        {
            Console.WriteLine("Token is valid!");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddDbContext<BankAccountsDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddHangfireServer();

builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddScoped<IAccountService, AccountService>();
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
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            []
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();
    db.Database.Migrate();
}

app.UseProblemDetails();
app.UseMiddleware<ValidationExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "bank-accounts V1"));

app.UseCors("AllowAll");

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter()]
});

RecurringJob.AddOrUpdate<AccountService>(
    "accrue-interest-job",
    service => service.AccrueInterestForAllAccounts(),
    Cron.Daily);

//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// if (app.Environment.IsDevelopment())
// {
//     app.UseDeveloperExceptionPage();
// }

app.Run();

public partial class Program;
