namespace ReposteriasManu.AutomatedTests.Helpers;

public static class TestData
{
    public static string NewToken(string scenario)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var suffix = Guid.NewGuid().ToString("N")[..8];

        return $"E2E_{scenario}_{timestamp}_{suffix}";
    }

    public static CustomerInput Customer(string token) =>
        new(token, "Automated", "8095550101", $"{token.ToLowerInvariant()}@example.com", "Direccion E2E");

    public static ProductInput Product(string token, decimal price = 25.50m) =>
        new(token, "Producto creado por Selenium", price, "Vainilla", "Pequeno");

    public static OrderInput Order(string notes, int customerId, string status = "Pendiente") =>
        new(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(3), status, notes, customerId);

    public static DecorationInput Decoration(string token, int orderId, int productId) =>
        new($"Tipo {token}", "Blanco", $"Mensaje {token}", orderId, productId);
}

public sealed record CustomerInput(string Name, string LastName, string Phone, string Email, string Address);

public sealed record ProductInput(string Name, string Description, decimal Price, string Flavor, string Size);

public sealed record OrderInput(DateTime OrderDate, DateTime DeliveryDate, string Status, string Notes, int CustomerId);

public sealed record DecorationInput(string Type, string Color, string Message, int OrderId, int ProductId);

public sealed record CustomerDto(int Id, string Name, string LastName, string? Phone, string? Email, string? Address);

public sealed record ProductDto(int Id, string Name, string? Description, decimal Price, string? Flavor, string? Size);

public sealed record OrderDto(
    int Id,
    DateTime OrderDate,
    DateTime DeliveryDate,
    string Status,
    string? Notes,
    int CustomerId);

public sealed record DecorationDto(
    int Id,
    string? Type,
    string? Color,
    string? Message,
    int OrderId,
    int ProductId);
