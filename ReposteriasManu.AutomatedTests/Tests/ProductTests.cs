using OpenQA.Selenium;
using ReposteriasManu.AutomatedTests.Helpers;
using System.Globalization;

namespace ReposteriasManu.AutomatedTests.Tests;

[TestClass]
public sealed class ProductTests : SeleniumTestBase
{
    [TestMethod]
    public async Task SEL_05_RegisterProduct()
    {
        var token = TestData.NewToken("SEL05_PRODUCT");
        var product = TestData.Product(token);

        NavigateTo("products", "products-body");
        Click(By.CssSelector("#products .btn-primary"));
        WaitForModalOpen("product-modal");

        Fill(By.Id("p-name"), product.Name);
        Fill(By.Id("p-description"), product.Description);
        Fill(By.Id("p-flavor"), product.Flavor);
        Fill(By.Id("p-size"), product.Size);
        Fill(By.Id("p-price"), product.Price.ToString("0.00", CultureInfo.InvariantCulture));
        SubmitForm("product-form");

        WaitForModalClosed("product-modal");
        WaitForRowContaining("products-body", product.Name);

        var created = await Api.FindProductByNameAsync(product.Name);
        Assert.IsNotNull(created, "El producto creado desde la interfaz debe existir en la API.");
        TrackProduct(created.Id);
    }

    [TestMethod]
    public async Task SEL_06_ProductPriceValidation()
    {
        var token = TestData.NewToken("SEL06_PRODUCT");
        var product = TestData.Product(token, 35.75m);

        NavigateTo("products", "products-body");
        Click(By.CssSelector("#products .btn-primary"));
        WaitForModalOpen("product-modal");

        Fill(By.Id("p-name"), product.Name);
        Fill(By.Id("p-description"), product.Description);
        Fill(By.Id("p-flavor"), product.Flavor);
        Fill(By.Id("p-size"), product.Size);

        Fill(By.Id("p-price"), "0");
        SubmitForm("product-form");
        Assert.IsTrue(IsModalOpen("product-modal"));
        Assert.IsFalse(IsFieldValid("p-price"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(ValidationMessageOf("p-price")));
        Assert.IsNull(await Api.FindProductByNameAsync(product.Name), "El precio 0 no debe crear producto.");

        Fill(By.Id("p-price"), "-1");
        SubmitForm("product-form");
        Assert.IsTrue(IsModalOpen("product-modal"));
        Assert.IsFalse(IsFieldValid("p-price"));
        Assert.IsNull(await Api.FindProductByNameAsync(product.Name), "El precio -1 no debe crear producto.");

        Fill(By.Id("p-price"), product.Price.ToString("0.00", CultureInfo.InvariantCulture));
        SubmitForm("product-form");

        WaitForModalClosed("product-modal");
        WaitForRowContaining("products-body", product.Name);

        var created = await Api.FindProductByNameAsync(product.Name);
        Assert.IsNotNull(created, "Un precio valido debe permitir crear el producto.");
        TrackProduct(created.Id);
    }
}
