using OpenQA.Selenium.Chrome;
using System.Collections.Concurrent;

namespace LeechCode
{
    public class ChromePool
    {
        private ConcurrentBag<ChromeDriver> instances = new ConcurrentBag<ChromeDriver>();
        private readonly Func<ChromeDriver> _createChromeDriver;

        public ChromePool(Func<ChromeDriver> createChromeDriver)
        {
            _createChromeDriver = createChromeDriver;
        }

        public ChromeDriver GetOrCreateChromeDriver()
        {
            ChromeDriver? driver;
            if (!instances.TryTake(out driver))
                driver = _createChromeDriver();
            return driver;
        }
        public void ReturnChromeDriver(ChromeDriver? driver)
        {
            if (driver != null)
                instances.Add(driver);
        }

    }
}
