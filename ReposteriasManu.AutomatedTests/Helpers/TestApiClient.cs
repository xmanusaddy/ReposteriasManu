using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ReposteriasManu.AutomatedTests.Helpers;

public sealed class TestApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client = new()
    {
        BaseAddress = TestConfiguration.ApiUrl,
        Timeout = TestConfiguration.DefaultTimeout
    };

    private readonly HttpClient _healthClient = new()
    {
        Timeout = TestConfiguration.DefaultTimeout
    };

    public async Task AssertReadyAsync()
    {
        using var response = await _healthClient.GetAsync(TestConfiguration.DatabaseHealthUrl);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode || !content.Contains("Connected", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Fail($"API/Supabase no esta listo. GET {TestConfiguration.DatabaseHealthUrl} devolvio {(int)response.StatusCode}: {content}");
        }
    }

    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync() =>
        GetListAsync<CustomerDto>("customer");

    public Task<IReadOnlyList<ProductDto>> GetProductsAsync() =>
        GetListAsync<ProductDto>("product");

    public Task<IReadOnlyList<OrderDto>> GetOrdersAsync() =>
        GetListAsync<OrderDto>("order");

    public Task<IReadOnlyList<DecorationDto>> GetDecorationsAsync() =>
        GetListAsync<DecorationDto>("decoration");

    public Task<CustomerDto> CreateCustomerAsync(CustomerInput input) =>
        PostAsync<CustomerDto>("customer", input);

    public Task<ProductDto> CreateProductAsync(ProductInput input) =>
        PostAsync<ProductDto>("product", input);

    public Task<OrderDto> CreateOrderAsync(OrderInput input) =>
        PostAsync<OrderDto>("order", input);

    public Task<DecorationDto> CreateDecorationAsync(DecorationInput input) =>
        PostAsync<DecorationDto>("decoration", input);

    public async Task<CustomerDto?> FindCustomerByEmailAsync(string email) =>
        (await GetCustomersAsync()).FirstOrDefault(customer =>
            string.Equals(customer.Email, email, StringComparison.OrdinalIgnoreCase));

    public async Task<CustomerDto?> FindCustomerByNameAsync(string name) =>
        (await GetCustomersAsync()).FirstOrDefault(customer =>
            string.Equals(customer.Name, name, StringComparison.OrdinalIgnoreCase));

    public async Task<ProductDto?> FindProductByNameAsync(string name) =>
        (await GetProductsAsync()).FirstOrDefault(product =>
            string.Equals(product.Name, name, StringComparison.OrdinalIgnoreCase));

    public async Task<OrderDto?> FindOrderByNotesAsync(string notes) =>
        (await GetOrdersAsync()).FirstOrDefault(order =>
            string.Equals(order.Notes, notes, StringComparison.OrdinalIgnoreCase));

    public async Task<DecorationDto?> FindDecorationByMessageAsync(string message) =>
        (await GetDecorationsAsync()).FirstOrDefault(decoration =>
            string.Equals(decoration.Message, message, StringComparison.OrdinalIgnoreCase));

    public Task DeleteDecorationAsync(int id) => DeleteIfExistsAsync($"decoration/{id}");

    public Task DeleteOrderAsync(int id) => DeleteIfExistsAsync($"order/{id}");

    public Task DeleteProductAsync(int id) => DeleteIfExistsAsync($"product/{id}");

    public Task DeleteCustomerAsync(int id) => DeleteIfExistsAsync($"customer/{id}");

    public void Dispose()
    {
        _client.Dispose();
        _healthClient.Dispose();
    }

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string path)
    {
        using var response = await _client.GetAsync(path);
        await EnsureSuccessAsync(response);

        var result = await response.Content.ReadFromJsonAsync<List<T>>(JsonOptions);
        return result ?? [];
    }

    private async Task<T> PostAsync<T>(string path, object body)
    {
        using var response = await _client.PostAsJsonAsync(path, body, JsonOptions);
        await EnsureSuccessAsync(response);

        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return result ?? throw new InvalidOperationException($"La API no devolvio datos para POST {path}.");
    }

    private async Task DeleteIfExistsAsync(string path)
    {
        using var response = await _client.DeleteAsync(path);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        await EnsureSuccessAsync(response);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        Assert.Fail($"La API devolvio {(int)response.StatusCode} en {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}: {content}");
    }
}
