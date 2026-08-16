using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ReposteriasManu.API.Responses;
using ReposteriasManu.Application.Contract;
using ReposteriasManu.Application.Services;
using ReposteriasManu.Infrastructure.Context;
using ReposteriasManu.Infrastructure.Interfaces;
using ReposteriasManu.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
        new BadRequestObjectResult(ApiErrorResponse.FromModelState(context.ModelState));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7185",
                "http://localhost:7185",
                "https://localhost:52455",
                "http://localhost:52456",
                "https://localhost:5001",
                "http://localhost:5000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var databaseConfigurationError = GetDatabaseConfigurationError(builder.Configuration, out var connectionString);
var isDatabaseConfigured = databaseConfigurationError is null;

if (isDatabaseConfigured)
{
    builder.Services.AddDbContext<ReposteriasManuContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(ReposteriasManuContext).Assembly.FullName);
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        }));
}

if (isDatabaseConfigured)
{
    builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IDecorationRepository, DecorationRepository>();

    builder.Services.AddScoped<ICustomerService, CustomerService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IDecorationService, DecorationService>();
}

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var (statusCode, message) = GetSafeExceptionResponse(exception);

        app.Logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new ApiErrorResponse(message));
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowWeb");
app.UseAuthorization();

if (isDatabaseConfigured)
{
    app.MapGet("/health/database", async (ReposteriasManuContext dbContext) =>
    {
        try
        {
            await dbContext.Database.OpenConnectionAsync();
            var hasCustomers = await dbContext.Customers.AsNoTracking().AnyAsync();

            return Results.Ok(new
            {
                status = "Connected",
                provider = dbContext.Database.ProviderName,
                readCheck = "Customers query succeeded",
                hasCustomers
            });
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Database health check failed.");

            return Results.Json(
                new ApiErrorResponse("No se pudo conectar con la base de datos. Intente nuevamente."),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    });
}
else
{
    app.MapGet("/health/database", () => Results.Json(
        new ApiErrorResponse("La conexion con la base de datos no esta configurada."),
        statusCode: StatusCodes.Status503ServiceUnavailable));
}

if (!isDatabaseConfigured)
{
    app.Logger.LogWarning("{DatabaseConfigurationError}", databaseConfigurationError);

    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse("La conexion con la base de datos no esta configurada."));

            return;
        }

        await next();
    });
}

app.MapControllers();
app.Run();

static string? GetDatabaseConfigurationError(IConfiguration configuration, out string connectionString)
{
    var configuredConnectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(configuredConnectionString))
    {
        connectionString = string.Empty;

        return "Configure ConnectionStrings:DefaultConnection with the Supabase PostgreSQL connection string " +
            "using user secrets, an environment variable, or appsettings.Development.json outside source control.";
    }

    try
    {
        connectionString = NormalizeSupabaseConnectionString(configuredConnectionString);
        return null;
    }
    catch (Exception ex)
    {
        connectionString = string.Empty;

        return $"ConnectionStrings:DefaultConnection is not a valid PostgreSQL connection string: {ex.Message}";
    }
}

static string NormalizeSupabaseConnectionString(string connectionString)
{
    var builder = new NpgsqlConnectionStringBuilder(connectionString);

    if (!string.IsNullOrWhiteSpace(builder.Host) &&
        builder.Host.Contains("supabase.co", StringComparison.OrdinalIgnoreCase))
    {
        SetIfMissing(connectionString, builder, "SSL Mode", () => builder.SslMode = SslMode.Require);
        SetIfMissing(connectionString, builder, "Pooling", () => builder.Pooling = true);
        SetIfMissing(connectionString, builder, "Timeout", () => builder.Timeout = 15);
        SetIfMissing(connectionString, builder, "Command Timeout", () => builder.CommandTimeout = 30);
        SetIfMissing(connectionString, builder, "Keepalive", () => builder.KeepAlive = 30);
    }

    return builder.ConnectionString;
}

static void SetIfMissing(string connectionString, NpgsqlConnectionStringBuilder builder, string key, Action setValue)
{
    if (!ConnectionStringContainsKey(connectionString, key))
    {
        setValue();
    }
}

static bool ConnectionStringContainsKey(string connectionString, string key)
{
    var normalizedKey = key.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);

    return connectionString
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(part => part.Split('=', 2)[0].Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase))
        .Any(partKey => string.Equals(partKey, normalizedKey, StringComparison.OrdinalIgnoreCase));
}

static (int StatusCode, string Message) GetSafeExceptionResponse(Exception? exception)
{
    if (exception is null)
    {
        return (StatusCodes.Status500InternalServerError, "No fue posible completar la operacion.");
    }

    if (exception is DbUpdateException dbUpdateException &&
        dbUpdateException.GetBaseException() is PostgresException postgresException)
    {
        return postgresException.SqlState switch
        {
            PostgresErrorCodes.ForeignKeyViolation =>
                (StatusCodes.Status400BadRequest,
                    "No fue posible guardar el registro porque una relacion seleccionada no existe."),
            PostgresErrorCodes.UniqueViolation =>
                (StatusCodes.Status409Conflict,
                    "No fue posible guardar el registro porque ya existe un dato con esa informacion."),
            _ =>
                (StatusCodes.Status409Conflict,
                    "No fue posible guardar los cambios en la base de datos.")
        };
    }

    if (IsDatabaseUnavailable(exception))
    {
        return (StatusCodes.Status503ServiceUnavailable,
            "No se pudo conectar con la base de datos. Intente nuevamente.");
    }

    if (exception is DbUpdateException)
    {
        return (StatusCodes.Status409Conflict,
            "No fue posible guardar los cambios en la base de datos.");
    }

    return (StatusCodes.Status500InternalServerError, "No fue posible completar la operacion.");
}

static bool IsDatabaseUnavailable(Exception exception)
{
    var baseException = exception.GetBaseException();

    return exception is NpgsqlException or TimeoutException ||
        baseException is NpgsqlException or TimeoutException;
}
