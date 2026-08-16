using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using ReposteriasManu.AutomatedTests.Helpers;

namespace ReposteriasManu.AutomatedTests.Tests;

[TestClass]
public sealed class OrderTests : SeleniumTestBase
{
    [TestMethod]
    public async Task SEL_07_CreateOrder()
    {
        var token = TestData.NewToken("SEL07_ORDER");
        var customer = await CreateTrackedCustomerAsync(token);
        var notes = $"Pedido {token}";

        NavigateTo("orders", "orders-body");
        Click(By.CssSelector("#orders .btn-primary"));
        WaitForModalOpen("order-modal");

        new SelectElement(Driver.FindElement(By.Id("o-customer-id"))).SelectByText($"{customer.Name} {customer.LastName}");
        SetInputValue("o-order-date", DateTime.Today.ToString("yyyy-MM-dd"));
        SetInputValue("o-delivery-date", DateTime.Today.AddDays(3).ToString("yyyy-MM-dd"));
        Fill(By.Id("o-notes"), notes);
        SubmitForm("order-form");

        WaitForModalClosed("order-modal");
        var row = WaitForRowContaining("orders-body", notes);
        Assert.IsTrue(row.Text.Contains(customer.Name, StringComparison.OrdinalIgnoreCase));

        var created = await Api.FindOrderByNotesAsync(notes);
        Assert.IsNotNull(created, "El pedido creado desde la interfaz debe existir en la API.");
        TrackOrder(created.Id);
    }

    [TestMethod]
    public async Task SEL_08_UpdateOrderStatus()
    {
        var token = TestData.NewToken("SEL08_ORDER");
        var customer = await CreateTrackedCustomerAsync(token);
        var notes = $"Pedido {token}";
        await CreateTrackedOrderAsync(notes, customer.Id);

        NavigateTo("orders", "orders-body");
        Fill(By.Id("search-orders"), notes);

        var row = WaitForRowContaining("orders-body", notes);
        ClickElement(row.FindElement(By.CssSelector(".btn-edit")));
        WaitForModalOpen("order-modal");

        new SelectElement(Driver.FindElement(By.Id("o-status"))).SelectByText("Listo");
        SubmitForm("order-form");

        WaitForModalClosed("order-modal");
        row = WaitForRowContaining("orders-body", notes);
        Assert.IsTrue(row.Text.Contains("Listo", StringComparison.OrdinalIgnoreCase));

        var updated = await Api.FindOrderByNotesAsync(notes);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Listo", updated.Status);
    }
}
