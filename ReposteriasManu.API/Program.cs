using Microsoft.EntityFrameworkCore;
using Npgsql;
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
            return Results.Problem(
                title: "Database connection failed",
                detail: ex.GetBaseException().Message,
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
    app.MapGet("/health/database", () => Results.Problem(
        title: "Database connection is not configured",
        detail: databaseConfigurationError,
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

            await context.Response.WriteAsJsonAsync(new
            {
                title = "Database connection is not configured",
                detail = databaseConfigurationError,
                requiredConfiguration = "ConnectionStrings:DefaultConnection"
            });

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
