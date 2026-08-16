using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace ReposteriasManu.AutomatedTests.Helpers;

[TestClass]
public abstract class SeleniumTestBase
{
    private readonly List<int> _customerIds = [];
    private readonly List<int> _productIds = [];
    private readonly List<int> _orderIds = [];
    private readonly List<int> _decorationIds = [];

    protected IWebDriver Driver { get; private set; } = null!;

    protected WebDriverWait Wait { get; private set; } = null!;

    protected TestApiClient Api { get; private set; } = null!;

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        Api = new TestApiClient();
        await Api.AssertReadyAsync();

        Driver = WebDriverFactory.Create();
        Wait = new WebDriverWait(Driver, TestConfiguration.DefaultTimeout);
        Wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

        Driver.Navigate().GoToUrl(TestConfiguration.WebUrl);
        WaitForDocumentReady();
    }

    [TestCleanup]
    public async Task CleanupAsync()
    {
        try
        {
            if (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed)
            {
                CaptureScreenshot();
            }
        }
        finally
        {
            Driver?.Quit();
            Driver?.Dispose();
        }

        await CleanupCreatedDataAsync();
        Api?.Dispose();
    }

    protected void TrackCustomer(int id) => _customerIds.Add(id);

    protected void TrackProduct(int id) => _productIds.Add(id);

    protected void TrackOrder(int id) => _orderIds.Add(id);

    protected void TrackDecoration(int id) => _decorationIds.Add(id);

    protected void NavigateTo(string sectionId, string? tableBodyId = null)
    {
        Click(By.CssSelector($"[data-section='{sectionId}']"));
        Wait.Until(driver => ElementHasClass(driver.FindElement(By.Id(sectionId)), "active"));

        if (tableBodyId is not null)
        {
            WaitForTableReady(tableBodyId);
        }
    }

    protected void WaitForDashboardReady()
    {
        Wait.Until(_ => TextOf("stat-customers") != "—" &&
                        TextOf("stat-products") != "—" &&
                        TextOf("stat-orders") != "—" &&
                        TextOf("stat-pending") != "—");
    }

    protected void WaitForTableReady(string tableBodyId)
    {
        Wait.Until(driver =>
        {
            var rows = driver.FindElements(By.CssSelector($"#{tableBodyId} tr"));
            return rows.Count > 0 &&
                   rows.All(row => !(row.GetAttribute("class") ?? string.Empty).Contains("loading", StringComparison.OrdinalIgnoreCase));
        });
    }

    protected IWebElement WaitForRowContaining(string tableBodyId, string expectedText)
    {
        return Wait.Until(driver => FindDisplayedRow(driver, tableBodyId, expectedText));
    }

    protected void WaitForNoRowContaining(string tableBodyId, string expectedText)
    {
        Wait.Until(driver => FindDisplayedRow(driver, tableBodyId, expectedText) is null);
    }

    protected void Click(By by)
    {
        var element = Wait.Until(driver =>
        {
            var candidate = driver.FindElement(by);
            return candidate.Displayed && candidate.Enabled ? candidate : null;
        });

        ClickElement(element);
    }

    protected void ClickElement(IWebElement element)
    {
        ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
        element.Click();
    }

    protected void Fill(By by, string value)
    {
        var element = Wait.Until(driver =>
        {
            var candidate = driver.FindElement(by);
            return candidate.Displayed && candidate.Enabled ? candidate : null;
        });

        element.SendKeys(Keys.Control + "a");
        element.SendKeys(value);
    }

    protected void SetInputValue(string elementId, string value)
    {
        var element = Wait.Until(driver =>
        {
            var candidate = driver.FindElement(By.Id(elementId));
            return candidate.Displayed && candidate.Enabled ? candidate : null;
        });

        ((IJavaScriptExecutor)Driver).ExecuteScript(
            """
            arguments[0].value = arguments[1];
            arguments[0].dispatchEvent(new Event('input', { bubbles: true }));
            arguments[0].dispatchEvent(new Event('change', { bubbles: true }));
            """,
            element,
            value);
    }

    protected void SubmitForm(string formId)
    {
        Click(By.CssSelector($"button[form='{formId}']"));
    }

    protected void WaitForModalOpen(string modalId)
    {
        Wait.Until(driver => ElementHasClass(driver.FindElement(By.Id(modalId)), "open"));
    }

    protected void WaitForModalClosed(string modalId)
    {
        Wait.Until(driver => !ElementHasClass(driver.FindElement(By.Id(modalId)), "open"));
    }

    protected bool IsModalOpen(string modalId)
    {
        return ElementHasClass(Driver.FindElement(By.Id(modalId)), "open");
    }

    protected bool IsFieldValid(string elementId)
    {
        var result = ((IJavaScriptExecutor)Driver).ExecuteScript(
            "return document.getElementById(arguments[0]).validity.valid;",
            elementId);

        return result is bool isValid && isValid;
    }

    protected string ValidationMessageOf(string elementId)
    {
        return ((IJavaScriptExecutor)Driver).ExecuteScript(
            "return document.getElementById(arguments[0]).validationMessage;",
            elementId)?.ToString() ?? string.Empty;
    }

    protected string TextOf(string elementId)
    {
        return Driver.FindElement(By.Id(elementId)).Text.Trim();
    }

    protected bool HasVisibleErrorToast()
    {
        var toast = Driver.FindElement(By.Id("toast"));
        var toastClass = toast.GetAttribute("class") ?? string.Empty;

        return toast.Displayed &&
               toastClass.Contains("error", StringComparison.OrdinalIgnoreCase) &&
               toastClass.Contains("show", StringComparison.OrdinalIgnoreCase);
    }

    protected void AcceptAlert()
    {
        var alert = Wait.Until(driver =>
        {
            try
            {
                return driver.SwitchTo().Alert();
            }
            catch (NoAlertPresentException)
            {
                return null;
            }
        });

        (alert ?? throw new WebDriverTimeoutException("No se mostro el dialogo de confirmacion esperado.")).Accept();
    }

    protected async Task<CustomerDto> CreateTrackedCustomerAsync(string token)
    {
        var customer = await Api.CreateCustomerAsync(TestData.Customer(token));
        TrackCustomer(customer.Id);
        return customer;
    }

    protected async Task<ProductDto> CreateTrackedProductAsync(string token)
    {
        var product = await Api.CreateProductAsync(TestData.Product(token));
        TrackProduct(product.Id);
        return product;
    }

    protected async Task<OrderDto> CreateTrackedOrderAsync(string notes, int customerId, string status = "Pendiente")
    {
        var order = await Api.CreateOrderAsync(TestData.Order(notes, customerId, status));
        TrackOrder(order.Id);
        return order;
    }

    protected async Task<DecorationDto> CreateTrackedDecorationAsync(string token, int orderId, int productId)
    {
        var decoration = await Api.CreateDecorationAsync(TestData.Decoration(token, orderId, productId));
        TrackDecoration(decoration.Id);
        return decoration;
    }

    private async Task CleanupCreatedDataAsync()
    {
        foreach (var id in _decorationIds.AsEnumerable().Reverse())
        {
            await Api.DeleteDecorationAsync(id);
        }

        foreach (var id in _orderIds.AsEnumerable().Reverse())
        {
            await Api.DeleteOrderAsync(id);
        }

        foreach (var id in _productIds.AsEnumerable().Reverse())
        {
            await Api.DeleteProductAsync(id);
        }

        foreach (var id in _customerIds.AsEnumerable().Reverse())
        {
            await Api.DeleteCustomerAsync(id);
        }
    }

    private void WaitForDocumentReady()
    {
        Wait.Until(driver =>
            string.Equals(
                ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState")?.ToString(),
                "complete",
                StringComparison.OrdinalIgnoreCase));
    }

    private static IWebElement? FindDisplayedRow(IWebDriver driver, string tableBodyId, string expectedText)
    {
        return driver.FindElements(By.CssSelector($"#{tableBodyId} tr"))
            .FirstOrDefault(row => row.Displayed &&
                                   row.Text.Contains(expectedText, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ElementHasClass(IWebElement element, string className)
    {
        return (element.GetAttribute("class") ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(cssClass => string.Equals(cssClass, className, StringComparison.OrdinalIgnoreCase));
    }

    private void CaptureScreenshot()
    {
        if (Driver is not ITakesScreenshot screenshotDriver)
        {
            return;
        }

        var resultsDirectory = TestContext.ResultsDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "TestResults");
        var screenshotDirectory = Path.Combine(resultsDirectory, "Screenshots");
        Directory.CreateDirectory(screenshotDirectory);

        var testName = TestContext.TestName ?? "FailedTest";
        var safeTestName = new string(testName.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character).ToArray());
        var screenshotPath = Path.Combine(screenshotDirectory, $"{safeTestName}.png");

        screenshotDriver.GetScreenshot().SaveAsFile(screenshotPath);
        TestContext.AddResultFile(screenshotPath);
    }
}
