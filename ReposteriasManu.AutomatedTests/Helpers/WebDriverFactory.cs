using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace ReposteriasManu.AutomatedTests.Helpers;

internal static class WebDriverFactory
{
    public static IWebDriver Create()
    {
        var options = new ChromeOptions();
        options.AddArgument("--window-size=1440,1000");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-search-engine-choice-screen");
        options.AddArgument("--no-first-run");
        options.AddArgument("--lang=es-DO");

        if (TestConfiguration.Headless)
        {
            options.AddArgument("--headless=new");
        }

        var service = ChromeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;

        return new ChromeDriver(service, options, TimeSpan.FromSeconds(60));
    }
}
