using ReposteriasManu.AutomatedTests.Helpers;

namespace ReposteriasManu.AutomatedTests.Tests;

[TestClass]
public sealed class DashboardTests : SeleniumTestBase
{
    [TestMethod]
    public void SEL_01_DashboardLoads()
    {
        WaitForDashboardReady();

        Assert.AreEqual("Dashboard", TextOf("page-title"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(TextOf("stat-customers")));
        Assert.IsFalse(string.IsNullOrWhiteSpace(TextOf("stat-products")));
        Assert.IsFalse(string.IsNullOrWhiteSpace(TextOf("stat-orders")));
        Assert.IsFalse(string.IsNullOrWhiteSpace(TextOf("stat-pending")));
        Assert.IsFalse(HasVisibleErrorToast(), "El dashboard no debe mostrar errores de carga.");
    }
}
