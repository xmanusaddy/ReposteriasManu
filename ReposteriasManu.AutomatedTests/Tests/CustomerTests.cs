using OpenQA.Selenium;
using ReposteriasManu.AutomatedTests.Helpers;

namespace ReposteriasManu.AutomatedTests.Tests;

[TestClass]
public sealed class CustomerTests : SeleniumTestBase
{
    [TestMethod]
    public async Task SEL_02_RegisterCustomer()
    {
        var token = TestData.NewToken("SEL02_CUSTOMER");
        var customer = TestData.Customer(token);

        NavigateTo("customers", "customers-body");
        Click(By.CssSelector("#customers .btn-primary"));
        WaitForModalOpen("customer-modal");

        Fill(By.Id("c-name"), customer.Name);
        Fill(By.Id("c-lastname"), customer.LastName);
        Fill(By.Id("c-email"), customer.Email);
        Fill(By.Id("c-phone"), customer.Phone);
        Fill(By.Id("c-address"), customer.Address);
        SubmitForm("customer-form");

        WaitForModalClosed("customer-modal");
        WaitForRowContaining("customers-body", customer.Email);

        var created = await Api.FindCustomerByEmailAsync(customer.Email);
        Assert.IsNotNull(created, "El cliente creado desde la interfaz debe existir en la API.");
        TrackCustomer(created.Id);
    }

    [TestMethod]
    public async Task SEL_03_CustomerValidation()
    {
        var token = TestData.NewToken("SEL03_CUSTOMER");

        NavigateTo("customers", "customers-body");
        Click(By.CssSelector("#customers .btn-primary"));
        WaitForModalOpen("customer-modal");

        SubmitForm("customer-form");

        Assert.IsTrue(IsModalOpen("customer-modal"));
        Assert.IsFalse(IsFieldValid("c-name"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(ValidationMessageOf("c-name")));

        Fill(By.Id("c-name"), token);
        Fill(By.Id("c-lastname"), "Automated");
        Fill(By.Id("c-email"), "correo-invalido");
        SubmitForm("customer-form");

        Assert.IsTrue(IsModalOpen("customer-modal"));
        Assert.IsFalse(IsFieldValid("c-email"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(ValidationMessageOf("c-email")));
        Assert.IsNull(await Api.FindCustomerByNameAsync(token), "El formulario invalido no debe crear clientes.");
    }

    [TestMethod]
    public async Task SEL_04_SearchAndEditCustomer()
    {
        var token = TestData.NewToken("SEL04_CUSTOMER");
        var updatedPhone = "8095550199";
        var customer = await CreateTrackedCustomerAsync(token);

        NavigateTo("customers", "customers-body");
        Fill(By.Id("search-customers"), customer.Email!);

        var row = WaitForRowContaining("customers-body", customer.Email!);
        ClickElement(row.FindElement(By.CssSelector(".btn-edit")));
        WaitForModalOpen("customer-modal");

        Fill(By.Id("c-phone"), updatedPhone);
        SubmitForm("customer-form");

        WaitForModalClosed("customer-modal");
        row = WaitForRowContaining("customers-body", customer.Email!);
        Assert.IsTrue(row.Text.Contains(updatedPhone, StringComparison.OrdinalIgnoreCase));
    }
}
