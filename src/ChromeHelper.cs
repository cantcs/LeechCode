using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using System.Text;

namespace LeechCode
{
    public class ChromeHelper
    {
        private static object _cookiesLock = new object();
        public bool LoggedIn { get; private set; } = false;
        public bool LoadSavedCookies { get; set; } = true;
        public ChromeHelperOptions Options;

        public class ChromeHelperOptions
        {
            public string? UserName;
            public string? Password;
            public bool Headless { get; set; } = true;
            public ChromeHelperOptions(string userName, string password)
            {
                UserName = userName;
                Password = password;
            }
            public ChromeHelperOptions() 
            { 
            }
        }

        public ChromeHelper()
        {
            Options = new ChromeHelperOptions();
        }
        public ChromeHelper(ChromeHelperOptions options)
        {
            Options = options;
        }

        private ChromeOptions GetChromeOptions()
        {
            ChromeOptions options = new ChromeOptions();
            if (Options?.Headless != false)
                options.AddArgument("headless");

            options.AddArgument("--window-size=1920,1080");
            //options.AddArgument("--window-size=3840,2160");
            return options;
        }

        public ChromeDriver CreateChromeDriver()
        {
            ChromeOptions options = GetChromeOptions();
            ChromeDriver driver = new ChromeDriver(options);
            try { driver.Manage().Window.Maximize(); } catch { }

            //bool cookiesLoaded = false;
            if (LoadSavedCookies)
            {
                lock (_cookiesLock)
                {
                    (_, LoggedIn) = LoadCookies(driver);
                }
            }

            if (!LoggedIn && !string.IsNullOrEmpty(Options.UserName) && !string.IsNullOrEmpty(Options.Password))
            {
                LoginAndSaveCookies(driver); // log-in and all subsequent calls to CreateChromeDriver will load these cookies
            }

            return driver;
        }

        public void LoginAndSaveCookies(ChromeDriver driver)
        {
            Login(driver);
            SaveCookies(driver);
        }

        public void Login(ChromeDriver driver)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            driver.Navigate().GoToUrl($"https://leetcode.com/accounts/login/");
            wait.Until(d => d.FindElements(By.CssSelector("#signin_btn")).Count == 1);
            driver.FindElement(By.Name("login")).SendKeys(Options.UserName);
            driver.FindElement(By.Name("password")).SendKeys(Options.Password);
            //var signin = driver.FindElement(By.CssSelector("#signin_btn"));
            //new Actions(driver).MoveToElement(signin).Click().Perform();
            driver.FindElement(By.CssSelector("#signin_btn")).Click(); // Click waits for the page to finish loading before resuming code execution
            wait.Until(d => d.FindElements(By.CssSelector(".notification-btn-container__23CT")).Count == 1);
        }

        public void SaveCookies(ChromeDriver driver)
        {
            lock (_cookiesLock)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var ck in driver.Manage().Cookies.AllCookies)
                    sb.AppendLine($"{ck.Name};{ck.Value};{ck.Domain};{ck.Path};{ck.Expiry};{ck.Secure};{ck.IsHttpOnly};{ck.SameSite}");
                File.WriteAllText("Cookies.data", sb.ToString());
            }
        }
        public void ClearCookies()
        {
            lock (_cookiesLock)
            {
                if (File.Exists("Cookies.data"))
                    File.Delete("Cookies.data");
            }
        }

        private (bool cookieLoaded, bool loggedIn) LoadCookies(ChromeDriver driver)
        {
            lock (_cookiesLock)
            {
                if (!File.Exists("Cookies.data"))
                    return (false, false);

                driver.Url = "https://leetcode.com"; // TODO: try driver.ExecuteCdpCommand("Network.setCookie")

                ICookieJar cookieJar = driver.Manage().Cookies;
                var cookies = File.ReadAllLines("Cookies.data");
                foreach (var cookie in cookies)
                {
                    var parts = cookie.Split(';');
                    string name = parts[0];
                    string value = parts[1];
                    string domain = parts[2];
                    string path = parts[3];
                    DateTime? expiry = (string.IsNullOrEmpty(parts[4]) ? null : DateTime.Parse(parts[4]));
                    bool isSecure = bool.Parse(parts[5]);
                    bool isHttpOnly = bool.Parse(parts[6]);
                    string sameSite = parts[7];
                    Cookie ck = new Cookie(name, value, domain.TrimStart('.'), path, expiry, isSecure, isHttpOnly, sameSite);
                    cookieJar.AddCookie(ck);
                }
                
                driver.Navigate().Refresh();
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                wait.Until(d => d.FindElements(By.CssSelector(".account-icon__3u4B, a[href='/accounts/login/']")).Any());
                bool loggedIn = driver.FindElements(By.CssSelector(".account-icon__3u4B")).Any();
                bool notLoggedIn = driver.FindElements(By.CssSelector("a[href='/accounts/login/']")).Any(); // just to be sure
                if (loggedIn != !notLoggedIn)
                    if (Debugger.IsAttached)
                        Debugger.Break();

                return (true, loggedIn);
            }
        }

        public HttpClientHandler CreateHttpClientHandler()
        {
            var handler = new HttpClientHandler();
            if (File.Exists("Cookies.data"))
            {
                handler.CookieContainer = new System.Net.CookieContainer();
                lock (_cookiesLock)
                {
                    var cookies = File.ReadAllLines("Cookies.data");
                    foreach (var cookie in cookies)
                    {
                        var parts = cookie.Split(';');
                        string name = parts[0];
                        string value = parts[1];
                        string domain = parts[2];
                        string path = parts[3];
                        DateTime? expiry = (string.IsNullOrEmpty(parts[4]) ? null : DateTime.Parse(parts[4]));
                        bool isSecure = bool.Parse(parts[5]);
                        bool isHttpOnly = bool.Parse(parts[6]);
                        string sameSite = parts[7];
                        //Cookie ck = new Cookie(name, value, domain.TrimStart('.'), path, expiry, isSecure, isHttpOnly, sameSite);
                        var ck = new System.Net.Cookie(name, value, path, domain.TrimStart('.'));
                        if (expiry.HasValue)
                            ck.Expires = expiry.Value;
                        ck.Secure = isSecure;
                        ck.HttpOnly = isHttpOnly;
                        handler.CookieContainer.Add(ck);
                    }
                }
            }
            return handler;
        }


    }
}
