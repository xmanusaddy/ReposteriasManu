using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using ReposteriasManu.AutomatedTests.Helpers;

namespace ReposteriasManu.AutomatedTests.Tests;

[TestClass]
public sealed class DecorationTests : SeleniumTestBase
{
    [TestMethod]
    public async Task SEL_09_RegisterDecoration()
    {
        var token = TestData.NewToken("SEL09_DECORATION");
        var customer = await CreateTrackedCustomerAsync($"{token}_CUSTOMER");
        var product = await CreateTrackedProductAsync($"{token}_PRODUCT");
        var order = await CreateTrackedOrderAsync($"Pedido {token}", customer.Id);
        var decoration = TestData.Decoration(token, order.Id, product.Id);

        NavigateTo("decorations", "decorations-body");
        Click(By.CssSelector("#decorations .btn-primary"));
        WaitForModalOpen("decoration-modal");

        Fill(By.Id("d-type"), decoration.Type);
        Fill(By.Id("d-color"), decoration.Color);
        Fill(By.Id("d-message"), decoration.Message);
        new SelectElement(Driver.FindElement(By.Id("d-order-id"))).SelectByText($"Pedido #{order.Id}");
        new SelectElement(Driver.FindElement(By.Id("d-product-id"))).SelectByText(product.Name);
        SubmitForm("decoration-form");

        WaitForModalClosed("decoration-modal");
        var row = WaitForRowContaining("decorations-body", decoration.Message);
        Assert.IsTrue(row.Text.Contains(decoration.Type, StringComparison.OrdinalIgnoreCase));

        var created = await Api.FindDecorationByMessageAsync(decoration.Message);
        Assert.IsNotNull(created, "La decoracion creada desde la interfaz debe existir en la API.");
        TrackDecoration(created.Id);
    }

    [TestMethod]
    public async Task SEL_10_ControlledDeletion()
    {
        var token = TestData.NewToken("SEL10_DELETE");
        var customer = await CreateTrackedCustomerAsync($"{token}_CUSTOMER");
        var product = await CreateTrackedProductAsync($"{token}_PRODUCT");
        var order = await CreateTrackedOrderAsync($"Pedido {token}", customer.Id);
        var decoration = await CreateTrackedDecorationAsync(token, order.Id, product.Id);

        NavigateTo("decorations", "decorations-body");
        Fill(By.Id("search-decorations"), decoration.Message!);

        var row = WaitForRowContaining("decorations-body", decoration.Message!);
        ClickElement(row.FindElement(By.CssSelector(".btn-danger")));
        AcceptAlert();

        WaitForNoRowContaining("decorations-body", decoration.Message!);
        Assert.IsNull(await Api.FindDecorationByMessageAsync(decoration.Message!), "La decoracion eliminada desde UI no debe existir.");
    }
}
