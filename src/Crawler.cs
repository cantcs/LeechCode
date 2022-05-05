using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Net;

namespace LeechCode
{
    public class Crawler
    {
        public bool AutoLaunch { get; set; } = false;
        public bool GetSolutionsInAllLanguages { get; set; } = true;
        private readonly DirectoryInfo _baseFolder;
        private readonly ChromeHelper _chromeHelper;
        private readonly string _problemsFile;
        private static object _fileLock = new object();
        public int QuestionPageWidth { get; set; } = 900; // last media query breakpoint is 896
        public int SolutionPageWidth { get; set; } = 900; // last media query breakpoint is 896

        public Crawler(DirectoryInfo baseFolder, ChromeHelper chromeHelper)
        {
            _baseFolder = baseFolder;
            _problemsFile = Path.Combine(_baseFolder.FullName, "Problems.json");
            _chromeHelper = chromeHelper;
        }


        #region Problems metadata
        private async Task<List<Problem>> FetchAllProblems()
        {
            var response = await new HttpClient().GetAsync("https://leetcode.com/api/problems/all/");
            var responseBody = await response.Content.ReadAsStringAsync();
            var metadata = JsonConvert.DeserializeObject<ApiMetadata>(responseBody);
            return metadata?.stat_status_pairs.ToList();
        }
        public async Task<List<Problem>> LoadProblems()
        {
            try
            {
                if (File.Exists(_problemsFile))
                {
                    var problems = JsonConvert.DeserializeObject<List<Problem>>(File.ReadAllText(_problemsFile))!.OrderBy(q => q.stat.frontend_question_id).ToList();
                    return problems;
                }
            }
            catch (Exception ex)
            {
                // ignore and fetch again
            }
            var allProblems = await FetchAllProblems();
            SaveProblemsMetadata(allProblems);
            return allProblems;
        }

        public void SaveProblemsMetadata(List<Problem> allProblems)
        {
            lock (_fileLock)
            {
                if (!new FileInfo(_problemsFile).Directory.Exists)
                    new FileInfo(_problemsFile).Directory.Create();
                File.WriteAllText(_problemsFile, JsonConvert.SerializeObject(allProblems.OrderByDescending(q => q.stat.question_id).ToList(), Formatting.Indented));
            }
        }
        #endregion

        #region Languages (Languages to Use)


        /// <summary>
        /// Preferred Languages in order. The first match will be used
        /// </summary>
        private List<string> PreferredQuestionLanguages { get; set; } = new string[]
        {
            "C#",
            "Java",
            "Python3",
            "Python",
            "MS SQL Server",
            "MySQL",
            "C++",
            "Go", 
            "Rust",
            "Oracle"
        }.ToList();

        /// <summary>
        /// Given the available languages for a question, pick the one that we prefer, by prioritizing our favorite languages in PreferredQuestionLanguages
        /// </summary>
        /// <param name="availableLanguages"></param>
        /// <returns></returns>
        public string GetPreferredQuestionLanguage(List<string>? availableLanguages)
        {
            if (availableLanguages == null || !availableLanguages.Any())
                return null;
            var langs = availableLanguages.OrderBy(lang => PreferredQuestionLanguages.Contains(lang) ? PreferredQuestionLanguages.IndexOf(lang) : int.MaxValue).ToList();
            return langs.FirstOrDefault();
        }

        /// <summary>
        // Preferred Languages in order. The first match will be used
        /// </summary>
        private List<string> PreferredSolutionLanguages { get; set; } = new string[]
        {
            "C#",
            "Java",
            "Python3",
            "Python",
            "MS SQL Server",
            "MySQL",
            "C++",
            "Go", 
            //"Rust",
            //"Oracle"
        }.ToList();

        /// <summary>
        /// Given the language that we want (expectedLang) and the list of available languages for a solution, pick the one that most resembles the language we want.
        /// </summary>
        /// <param name="expectedLang"></param>
        /// <param name="availableLanguages"></param>
        /// <returns></returns>
        public string GetPreferredSolutionLanguage(string expectedLang, List<string>? availableLanguages)
        {
            string defaultLanguage = "Java";
            if (availableLanguages == null || !availableLanguages.Any())
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                return defaultLanguage;
            }

            if (expectedLang == "Python" && !availableLanguages.Contains("Python") && availableLanguages.Contains("Python3"))
                return "Python3";
            if (expectedLang == "Python3" && !availableLanguages.Contains("Python3") && availableLanguages.Contains("Python"))
                return "Python";
            if (expectedLang == "Java" && !availableLanguages.Contains("Java") && availableLanguages.Contains("C#"))
                return "C#";
            if (expectedLang == "C#" && !availableLanguages.Contains("C#") && availableLanguages.Contains("Java"))
                return "Java";
            if (expectedLang == "C++" && !availableLanguages.Contains("C++") && availableLanguages.Contains("C"))
                return "C";
            if (expectedLang == "C" && !availableLanguages.Contains("C") && availableLanguages.Contains("C++"))
                return "C++";

            // Just pick the first language from our ordered list PreferredSolutionLanguages which is available in availableLanguages
            var langs = availableLanguages.OrderBy(lang => PreferredSolutionLanguages.Contains(lang) ? PreferredSolutionLanguages.IndexOf(lang) : int.MaxValue).ToList();
            return langs.First();
        }
        #endregion

        #region ScrapeQuestion
        public QuestionDetails ScrapeQuestion(ChromeDriver driver, Problem problem, bool loggedIn, bool overwrite = true, string lang = null)
        {
            Debug.WriteLine($"Downloading question for {problem.stat.frontend_question_id.ToString("0000")}: {problem.stat.question__title_slug}...");
            QuestionDetails questionDetails = new QuestionDetails() { last_fetch = DateTime.Now };
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

            string url = $"https://leetcode.com/problems/{problem.stat.question__title_slug}/";
            try { driver.Manage().Window.Maximize(); } catch { driver.Manage().Window.Maximize();  }
            try
            {
                driver.Navigate().GoToUrl(url);
            }
            catch (WebDriverException ex)
            {
                driver.Navigate().GoToUrl(url);
            }

            if (driver.Url.Contains("/accounts/login"))
            {
                _chromeHelper.LoginAndSaveCookies(driver);
                driver.Navigate().GoToUrl(url);
            }

            string contentCssClass = "content__3fR6";

            wait.Until(d => d.FindElements(By.CssSelector(".css-oqu510 .css-jkjiwi")).Count > 0);
            wait.Until(d => d.FindElements(By.ClassName(contentCssClass)).Count > 0);

            Thread.Sleep(1000);

            const string subContentCssClass = "side-tools-wrapper__1TS9";
            wait.Until(d => d.FindElements(By.ClassName(subContentCssClass)).Count > 0);

            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;


            var content = driver.FindElement(By.ClassName(subContentCssClass));

            if (problem.stat.frontend_question_id == 0)
            {
                var header = driver.FindElements(By.CssSelector($"div[data-cy='question-title']")).First();
                problem.stat.frontend_question_id = int.Parse(header.Text.Substring(0, header.Text.IndexOf(".")));
            }


            // background image when we print background graphics
            driver.ExecuteScript(@"
                        // Your CSS as text
                        var styles = '::-webkit-scrollbar-thumb { background: none; }'
                        var styleSheet = document.createElement('style')
                        styleSheet.innerText = styles
                        document.head.appendChild(styleSheet)
                    ");


            // code editor:
            var ddl = driver.FindElement(By.CssSelector("div[role='combobox']"));
            try
            {
                new Actions(driver).MoveToElement(ddl).Click().Perform(); // load dropdown languages
                wait.Until(d => d.FindElements(By.CssSelector("li[data-cy^=lang-select]")).Any());
            }
            catch (WebDriverTimeoutException ex) // sometimes it fails, works on second attempt
            {
                new Actions(driver).MoveToElement(ddl).Click().Perform(); // load dropdown languages
                wait.Until(d => d.FindElements(By.CssSelector("li[data-cy^=lang-select]")).Any());
            }

            bool hasSolution = driver.FindElements(By.CssSelector($"a[href='/problems/{problem.stat.question__title_slug}/solution/'")).Any();
            bool premiumSolution = driver.FindElements(By.CssSelector($"a[href='/problems/{problem.stat.question__title_slug}/solution/'] .css-ut75m1-ColoredIcon")).Any();
            bool freeSolution = driver.FindElements(By.CssSelector($"a[href='/problems/{problem.stat.question__title_slug}/solution/'] .css-1nf3fa5-ColoredIcon")).Any();
            questionDetails.has_solution = hasSolution;
            if (!hasSolution && !premiumSolution && !freeSolution) { }
            else if (premiumSolution != freeSolution)
                questionDetails.premium_solution = premiumSolution;
            else if (Debugger.IsAttached)
                Debugger.Break();


            if (lang == null)
            {
                var langs = driver.FindElements(By.CssSelector("li[data-cy^=lang-select]")).Select(lang => lang.GetAttribute("data-cy").Substring("lang-select-".Length)).ToList();
                questionDetails.languages = langs;
                lang = GetPreferredQuestionLanguage(langs);
            }

            js.ExecuteScript($"document.querySelector(\"li[data-cy='lang-select-{lang}']\").click();");
            js.ExecuteScript("document.querySelectorAll('.container__2WTi').forEach(el => el.remove());"); // run code / submit
            js.ExecuteScript("document.querySelectorAll('.question-picker-detail__Rehh, .global-clipboard-container').forEach(el => el.remove());"); // hidden divs // run code / submit
            js.ExecuteScript("Array.from(document.querySelectorAll('span')).filter(el => el.textContent == 'Autocomplete').forEach(el => el.parentNode.remove());");
            js.ExecuteScript("document.querySelectorAll('.btns__1OeZ').forEach(el => el.remove());");

            string script2 = $@"
                var newParent = document.querySelector('.content__u3I1.question-content__JfgR');
                var content = document.querySelector('.wrapper__1Diw.editor__DNsS');
                newParent.appendChild(content);
            ";
            js.ExecuteScript(script2);
            js.ExecuteScript("document.querySelector('.CodeMirror.cm-s-textmate.CodeMirror-wrap').style.position='relative';");
            js.ExecuteScript("document.querySelector('.CodeMirror.cm-s-textmate.CodeMirror-wrap').style.height='auto';");



            js.ExecuteScript("document.querySelectorAll('.editor-wrapper__1ru6').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.header__3STC').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.question-fast-picker-wrapper__2Y97').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.css-5wdlwo-TabViewHeader').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.erd_scroll_detection_container').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.question-picker-detail__Rehh').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.note__1Qo7').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.resize-bar__2_sK').forEach(el => el.remove());");
            if (loggedIn)
                js.ExecuteScript("Array.from(document.querySelectorAll('div .title__3BS7')).filter(el => el.textContent == 'Seen this question in a real interview before?')[0].parentNode.remove();"); // catch exception and wait for parentNode - looks like we're too fast here
            js.ExecuteScript("Array.from(document.querySelectorAll('span')).filter(el => el.textContent == 'Add to List')[0].parentNode.remove();");
            js.ExecuteScript("Array.from(document.querySelectorAll('span')).filter(el => el.textContent == 'Share')[0].parentNode.remove();");
            Thread.Sleep(1000);

            wait.Until(d => d.FindElements(By.CssSelector($".{contentCssClass}")).Count == 1);

            string script = $@"
                //var newParent = document.getElementsByClassName('main__2_tD')[0];
                var newParent = document.getElementById('app');
                newParent.style.height = 'auto';
                //var content = document.getElementsByClassName('{contentCssClass}')[0];
                var content = document.querySelector('div[data-key=""description-content""]').parentNode;
                newParent.innerHTML = '';
                newParent.appendChild(content);";
            js.ExecuteScript(script);
            driver.ExecuteScript($"document.querySelector('#app').style.maxWidth='{QuestionPageWidth}px';");
            driver.ExecuteScript($"document.querySelector('#app').style.width='{QuestionPageWidth}px';");

            js.ExecuteScript(@"document.querySelector('div[data-key=""description-content""]').style.display = 'block';");
            js.ExecuteScript(@"document.querySelector('div[data-key=""description-content""]').style.border = 'none';");
            //js.ExecuteScript("document.querySelector('#app').style.height = 'auto';");

            Thread.Sleep(1000);

            var body = driver.FindElement(By.CssSelector("body"));

            driver.ExecuteScript("arguments[0].style.overflow = 'scroll';", body);

            Thread.Sleep(2000);

            // Companies, Related Topics, Similar Questions...
            Expand(driver, () => driver.FindElements(By.CssSelector(".css-isal7m .css-blecvm.e5i1odf0")).Where(el => !el.FindElements(By.CssSelector(".lock-icon__1hmE")).Any()).ToList(), breakIfNone: true);
            try { driver.Manage().Window.Size = new Size(QuestionPageWidth, driver.Manage().Window.Size.Height); } catch { }


            Thread.Sleep(1000);

            string filename = GetFileName(problem, "Question", lang);
            SaveAsPdf(driver, filename, overwrite, launch: AutoLaunch);

            return questionDetails;
        }
        #endregion

        #region ScrapeSolution
        public SolutionDetails ScrapeSolution(ChromeDriver driver, Problem problem, bool onlyFirstPass, bool ignoreErrors, bool overwrite = true)
        {
            Debug.WriteLine($"Downloading solution for {problem.stat.frontend_question_id.ToString("0000")}: {problem.stat.question__title_slug}...");
            SolutionDetails solutionDetails = new SolutionDetails() { last_fetch = DateTime.Now };
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            string problemFolder = GetProblemFolder(problem);

            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            string contentCssClass = "content__QRGW";

            driver.SwitchTo().DefaultContent();

            string url = $"https://leetcode.com/problems/{problem.stat.question__title_slug}/solution/";
            
            if (driver.Url != url) // if the driver is already in the right page, it's because previous steps loaded page, removed boilerplate, etc.
            {

                try { driver.Manage().Window.Maximize(); } catch { driver.Manage().Window.Maximize(); } // move to other monitor before this?
                Thread.Sleep(new Random().Next(5000));
                driver.Navigate().GoToUrl(url);
                if (driver.Url.Contains("/accounts/login"))
                {
                    _chromeHelper.LoginAndSaveCookies(driver);
                    driver.Navigate().GoToUrl(url);
                }


                try
                {
                    wait.Until(d => d.FindElements(By.CssSelector($".{contentCssClass}")).Count == 1);
                }
                catch (WebDriverTimeoutException ex)
                {
                    bool noSolution = driver.FindElements(By.CssSelector("div[disabled] span.title__3f2k")).Any(t => t.Text == "Solution");
                    if (noSolution)
                        return null;
                    else if (Debugger.IsAttached)
                        Debugger.Break();
                }

                RemoveSolutionBoilerplate(driver, wait, contentCssClass);
            }


            try { driver.Manage().Window.Size = new Size(SolutionPageWidth, driver.Manage().Window.Size.Height); } catch { }

            js.ExecuteScript("document.querySelectorAll('.editor-wrapper__1ru6').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.header__3STC').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.pagination-container__px42').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.question-fast-picker-wrapper__2Y97').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.css-5wdlwo-TabViewHeader').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.nav__1n5p').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.comment__4GKl').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.editor__2AvG').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.community-rules__25MG').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.erd_scroll_detection_container').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.note__1Qo7').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.resize-bar__2_sK').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('.copy-code-btn').forEach(el => el.remove());");
            js.ExecuteScript("document.querySelectorAll('p a.report-link__1eJM').forEach(el => el.remove());");
            Thread.Sleep(1000);

            var body = driver.FindElement(By.CssSelector("body"));
            var element = driver.FindElement(By.ClassName("layout__3fIJ"));
            var element2 = driver.FindElement(By.ClassName("main__2_tD"));

            driver.ExecuteScript("arguments[0].style.overflow = 'scroll';", body);
            driver.ExecuteScript("arguments[0].style.overflow = 'scroll';", element);

            driver.ExecuteScript("arguments[0].style.position = 'static';", element);
            driver.ExecuteScript("arguments[0].style.position = 'static';", element2);

            if (!Directory.Exists(problemFolder))
                Directory.CreateDirectory(problemFolder);

            #region Videos in main html
            var videos = driver.FindElements(By.CssSelector($"video source[src]")).ToList();
            foreach (var video in videos)
            {
                string src = video.GetAttribute("src");
                string filename = src.Substring(src.LastIndexOf("/") + 1);
                string filepath = Path.Combine(problemFolder, filename);
                using (var client = new WebClient())
                {
                    client.DownloadFile(src, filepath);
                }
                driver.ExecuteScript($@"
                    var video = arguments[0].parentNode;
                    var poster = video.getAttribute('poster');
                    var img = document.createElement('img');
                    img.style.border='solid 1px black';
                    img.src = poster;

                    var a = document.createElement('a');
                    a.appendChild(img);
                    a.title = 'Video';
                    a.href = 'file:///{filename}';
                    a.setAttribute('target', '_blank');
                    video.parentNode.appendChild(a);
                    video.remove();
                    ", video);
            }
            #endregion

            #region Dia Slides
            var diaSlides = driver.FindElements(By.CssSelector(".dia-container__jsK9:not(.processed)")).ToList();
            foreach (var diaSlide in diaSlides)
            {
                var footer = diaSlide.FindElement(By.CssSelector(".frame-counter__mLmP")).Text;
                var nextBtn = diaSlide.FindElements(By.CssSelector(".control-panel__1ogu .controls__3i3n svg"))[2];
                var firstFooter = footer;
                string filename;
                string extension = null;
                do
                {
                    var blob = diaSlide.FindElement(By.CssSelector(".dia-img__3g12")).GetAttribute("src");
                    filename = $"Presentation{diaSlides.IndexOf(diaSlide) + 1}-Slide_{footer.Replace("/", "of").Replace(" ", "_")}";
                    string filepath = Path.Combine(problemFolder, filename);

                    string base64 = (string)driver.ExecuteScript($@"
                        function blobToBase64(blob) {{
                          return new Promise((resolve, _) => {{
                            const reader = new FileReader();
                            reader.onloadend = () => resolve(reader.result);
                            reader.readAsDataURL(blob);
                          }});
                        }}
                        return await blobToBase64(await fetch('{blob}').then(r => r.blob()));
                    ");
                    var binary = Convert.FromBase64String(base64.Substring(base64.IndexOf(",") + 1));
                    if (base64.StartsWith("data:image/png;"))
                        extension = "png";
                    else if (base64.StartsWith("data:image/jpeg;"))
                        extension = "jpg";
                    else if (base64.StartsWith("data:image/svg+xml;"))
                        extension = "svg";
                    else if (Debugger.IsAttached)
                        Debugger.Break();

                    File.WriteAllBytes($"{filepath}.{extension}", binary);

                    try
                    {
                        new Actions(driver).MoveToElement(nextBtn).Click().Perform();
                        wait.Until(d => diaSlide.FindElement(By.CssSelector(".frame-counter__mLmP")).Text != footer);
                    }
                    catch (WebDriverTimeoutException)
                    {
                        new Actions(driver).MoveToElement(nextBtn).Click().Perform();
                        wait.Until(d => diaSlide.FindElement(By.CssSelector(".frame-counter__mLmP")).Text != footer);
                    }

                    footer = diaSlide.FindElement(By.CssSelector(".frame-counter__mLmP")).Text;

                } while (footer != firstFooter);
                filename = $"Presentation{diaSlides.IndexOf(diaSlide) + 1}-Slide_{footer.Replace("/", "of").Replace(" ", "_")}";

                driver.ExecuteScript($@"
                    arguments[0].classList.add('processed');
                    var ppt = arguments[0].parentNode;
                    var parent = ppt.parentNode;
                    var a = document.createElement('a');
                    a.appendChild(ppt);
                    a.title = 'Presentation';
                    a.href = 'file:///{filename}.{extension}';
                    parent.appendChild(a);
                    ", diaSlide);
            }
            //TODO: combine all slides in PPTX?
            #endregion

            #region Vimeo videos embedded in iframes
            var vimeoVideos = driver.FindElements(By.CssSelector(".content__QRGW > *:not(.root__3XxC) iframe[src^='https://player.vimeo.com/']"));
            solutionDetails.vimeoVideos ??= new VimeoVideo[vimeoVideos.Count];

            int videoIndex = -1;
            foreach (var iframe in vimeoVideos)
            {
                videoIndex++;
                driver.SwitchTo().DefaultContent();
                var vimeourl = iframe.GetAttribute("src");
                driver.SwitchTo().Frame(iframe);
                var play = driver.FindElement(By.CssSelector(".play-icon"));
                new Actions(driver).MoveToElement(play).Click().Perform();
                var scripts = driver.FindElements(By.CssSelector("script"));
                var jsScript = scripts.Where(s => s.GetAttribute("innerHTML").Contains(" var config =")).SingleOrDefault();
                string jsCode = jsScript.GetAttribute("innerHTML");
                jsCode = jsCode.Substring(jsCode.IndexOf("var config = ") + "var config = ".Length);
                jsCode = jsCode.Substring(0, jsCode.IndexOf("vimeo_url\":\"vimeo.com\"}") + "vimeo_url\":\"vimeo.com\"}".Length);
                var vimeoMetadata = JsonConvert.DeserializeObject<Vimeo.VimeoRoot>(jsCode);
                var src = vimeoMetadata.request.files.progressive.OrderByDescending(p => p.width).First().url;

                string filename = src.Substring(src.LastIndexOf("/") + 1);
                string filepath = Path.Combine(problemFolder, filename);
                string extension = filename.Substring(filename.LastIndexOf(".") + 1);
                var thumbpreview = vimeoMetadata.seo.thumbnail;

                using (var client = new WebClient())
                {
                    client.DownloadFile(src, filepath);
                }
                try
                {
                    var pause = driver.FindElement(By.CssSelector(".pause-icon"));
                    new Actions(driver).MoveToElement(pause).Click().Perform();
                }
                catch { }

                solutionDetails.vimeoVideos[videoIndex] = new VimeoVideo() { Url = vimeourl, VideoUrl = src, Thumbnail = thumbpreview };

                driver.SwitchTo().DefaultContent();

                driver.ExecuteScript($@"
                    var parent = arguments[0].parentNode;
                    var img = document.createElement('img');
                    img.style.border='solid 1px black';
                    img.src = '{thumbpreview}';

                    var a = document.createElement('a');
                    a.appendChild(img);
                    a.title = 'Video';
                    a.href = 'file:///{filename}';
                    a.setAttribute('target', '_blank');
                    parent.innerHTML ='';
                    parent.appendChild(a);
                    parent.classList.add('processed');
                    ", iframe);
            }
            if (videoIndex>-1)
                driver.SwitchTo().DefaultContent();
            #endregion

            #region Remaining Videos (not processed) in main html
            var videoDivsInPage = driver.FindElements(By.CssSelector(".video-container:not(.processed)")).ToList();
            if (videoDivsInPage.Any())
                if (Debugger.IsAttached) // review what kind of videos we didnt process
                    Debugger.Break();
            try
            {
                js.ExecuteScript(@"
                        var videoDivs = document.querySelectorAll('.video-container:not(.processed)');
                        videoDivs.forEach(function(videoDiv) { 
                            var removedPreviousBlocks = false;
                            if (videoDiv.parentNode.previousSibling != null && videoDiv.parentNode.previousSibling.nodeName=='#text')
                            {
                                videoDiv.parentNode.previousSibling.remove();
                                removedPreviousBlocks = true;
                            }
                            if (videoDiv.parentNode.previousSibling != null && videoDiv.parentNode.previousSibling.nodeName=='HR')
                            {
                                videoDiv.parentNode.previousSibling.remove();
                                removedPreviousBlocks = true;
                            }
                            if (videoDiv.parentNode.previousSibling != null && videoDiv.parentNode.previousSibling.nodeName=='#text')
                            {
                                videoDiv.parentNode.previousSibling.remove();
                                removedPreviousBlocks = true;
                            }
                            if (videoDiv.parentNode.previousSibling != null && videoDiv.parentNode.previousSibling.nodeName=='H2')
                            {
                                videoDiv.parentNode.previousSibling.remove();
                                removedPreviousBlocks = true;
                            }
                            if (removedPreviousBlocks == true)
                            {
                                videoDiv.parentNode.remove();
                            }
                            else
                            {
                                videoDiv.remove();
                            }
                        });
                    ");

            }
            catch (Exception ex)
            {

            }
            #endregion



            //TODO: wrap iframes inside divs with "page-break-inside: avoid" ? I couldn't make it work. It's breaking new page before large divs.
            // document.querySelectorAll("iframe[src^='https://leetcode.com/']").forEach(function(iframe) { var div = document.createElement('div'), rel=iframe.nextSibling, par=iframe.parentNode; div.appendChild(iframe); par.insertBefore(div, rel);  })

            List<string> solutionAvailableLanguages = new List<string>();
            ReadOnlyCollection<IWebElement> iframes = null;
            try
            {
                // now we get only the SOLUTION frames
                driver.SwitchTo().DefaultContent();
                iframes = driver.FindElements(By.CssSelector(".content__QRGW > *:not(.root__3XxC) iframe[src^='https://leetcode.com/']"));
                if (iframes.Count == 0 && Debugger.IsAttached)
                {
                    // a small number of questions don't have the scrollable code editor (e.g. SQL questions like 175) - they just have this pre/code:
                    var preCodeBlocks = driver.FindElements(By.CssSelector("pre code[class^=language-]"));
                    if (preCodeBlocks.Any())
                    {
                        var langs = preCodeBlocks.Select(el => el.GetAttribute("class").Substring("language-".Length)).Distinct().ToList();
                        solutionAvailableLanguages.AddRange(langs);
                    }
                    else if (Debugger.IsAttached)
                        Debugger.Break();
                }

                solutionDetails.frames = new SolutionIframe[iframes.Count];
                for (int frameIndex = iframes.Count - 1; frameIndex >= 0; frameIndex--) // going backwards is better since we're expanding elements
                {
                    var iframe = iframes[frameIndex];
                    FrameInspectLanguages(driver, iframe, frameIndex, wait, solutionDetails, js, ignoreErrors);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            Thread.Sleep(2000);

            foreach (var iframe in solutionDetails.frames)
                    foreach (var tab in iframe.tabs)
                        solutionAvailableLanguages.Add(tab.language);
            if (solutionAvailableLanguages.Any())
            {
                // Order available languages by the most popular
                solutionDetails.languages = solutionAvailableLanguages.GroupBy(l => l).OrderByDescending(g => g.Count()).Select(g => g.Key).ToList();
            }
            else if (Debugger.IsAttached)
                Debugger.Break();


            if (!onlyFirstPass)
            {

                //List<string> langsToSave = new List<string> { GetPreferredSolutionLanguage(solutionAvailableLanguages) }; // if we were to pick a single lang
                List<string> langsToSave = solutionAvailableLanguages.Distinct().ToList(); // if we want all languages

                if (langsToSave.Count==0)
                    if (Debugger.IsAttached)
                        Debugger.Break();
                string filename;

                foreach (string lang in langsToSave)
                {
                    string langToSave;

                    for (int frameIndex = iframes.Count - 1; frameIndex >= 0; frameIndex--)
                    {
                        var iframe = iframes[frameIndex];
                        List<string> availableLangs = solutionDetails.frames[frameIndex].tabs.Select(tab => tab.language).ToList();

                        if (!solutionDetails.frames[frameIndex].tabs.Any()) // tabs without code editor
                            continue;

                        if (availableLangs.Contains(lang))
                            langToSave = lang;
                        else
                            // Different solutions (for the same question) may not be all available in the same language - so among the available languages for this code frame we pick the most popular
                            langToSave = GetPreferredSolutionLanguage(lang, availableLangs);

                        FrameSelectLanguages(driver, iframe, frameIndex, wait, js, langToSave, ignoreErrors);
                    }

                    // Just solutions
                    driver.ExecuteScript("document.querySelectorAll('.header___QdN, .comments-container__tcjS').forEach(el => el.style.display='none');");

                    filename = GetFileName(problem, "Solution", lang);
                    SaveAsPdf(driver, filename, overwrite, launch: AutoLaunch);
                }

                #region Comments
                driver.ExecuteScript("document.querySelectorAll('.header___QdN, .comments-container__tcjS').forEach(el => el.style.display='block');");
                driver.ExecuteScript("document.querySelector('.header___QdN').parentNode.previousSibling.style.display='none';");
                driver.ExecuteScript("document.querySelector('.header___QdN').parentNode.previousSibling.previousSibling.style.display='none';");

                ReadOnlyCollection<IWebElement> commentsCodeIframes = driver.FindElements(By.CssSelector(".content__QRGW > .root__3XxC iframe[src^='https://leetcode.com/']"));
                for (int frameIndex = commentsCodeIframes.Count - 1; frameIndex >= 0; frameIndex--)
                {
                    var iframe = commentsCodeIframes[frameIndex];
                    CleanCodeFramesInComments(driver, iframe, frameIndex, wait, js, ignoreErrors);
                }
                if (commentsCodeIframes.Count > 0)
                    driver.SwitchTo().DefaultContent();


                Expand(driver, () => driver.FindElements(By.CssSelector("div[data-is-show-read-more='true'] div[data-is-beyond-limit-size='true'] .read-more__3UuG")), breakIfNone: true);

                // expand for wider comments
                var maxWidth = (int)(long)driver.ExecuteScript("return document.querySelector('.content__QRGW').clientWidth;");
                int maxWidth2 = Convert.ToInt32(driver.ExecuteScript("return Math.max(...Array.from(document.querySelectorAll('.comment__3raU pre code')).map(function(x){return x.getBoundingClientRect().right}));")) + 90;
                maxWidth = Math.Max(maxWidth2, maxWidth); // looks like THIS is accurate.
                maxWidth = Math.Max(SolutionPageWidth, maxWidth); // don't reduce less than the default width
                maxWidth = Math.Min(maxWidth, 1600); // don't let it get too crazy
                try { driver.Manage().Window.Size = new Size(maxWidth, driver.Manage().Window.Size.Height); } catch { }
                driver.ExecuteScript($"document.querySelector('.main__2_tD').style.maxWidth='{maxWidth}px';");
                driver.ExecuteScript($"document.querySelector('.main__2_tD').style.width='{maxWidth}px';");
                driver.ExecuteScript($"document.querySelector('#app').style.maxWidth='{maxWidth}px';");
                driver.ExecuteScript($"document.querySelector('#app').style.width='{maxWidth}px';");
                filename = GetFileName(problem, "Solution-Comments");
                SaveAsPdf(driver, filename, overwrite, launch: AutoLaunch);

                int expanded = 1;
                while (expanded > 0)
                {
                    // Expand replies
                    expanded = Expand(driver, () => driver.FindElements(By.CssSelector(".action__1C-I span")).Where(el => el.Text.StartsWith("Show ")).ToList(), breakIfNone: false);

                    // Expand ReadMores !
                    expanded += Expand(driver, () => driver.FindElements(By.CssSelector("div[data-is-show-read-more='true'] div[data-is-beyond-limit-size='true'] .read-more__3UuG")), breakIfNone: true);
                }
                maxWidth = (int)(long)driver.ExecuteScript("return document.querySelector('.content__QRGW').clientWidth;");
                maxWidth2 = Convert.ToInt32(driver.ExecuteScript("return Math.max(...Array.from(document.querySelectorAll('.comment__3raU pre code')).map(function(x){return x.getBoundingClientRect().right}));")) + 90;
                maxWidth = Math.Max(maxWidth2, maxWidth); // looks like THIS is accurate.
                maxWidth = Math.Max(SolutionPageWidth, maxWidth); // don't reduce less than the default width
                maxWidth = Math.Min(maxWidth, 1600); // don't let it get too crazy
                try { driver.Manage().Window.Size = new Size(maxWidth, driver.Manage().Window.Size.Height); } catch { }
                driver.ExecuteScript($"document.querySelector('.main__2_tD').style.maxWidth='{maxWidth}px';");
                driver.ExecuteScript($"document.querySelector('.main__2_tD').style.width='{maxWidth}px';");
                driver.ExecuteScript($"document.querySelector('#app').style.maxWidth='{maxWidth}px';");
                driver.ExecuteScript($"document.querySelector('#app').style.width='{maxWidth}px';");

                filename = GetFileName(problem, "Solution-Comments-Expanded");
                SaveAsPdf(driver, filename, overwrite, launch: AutoLaunch);
                #endregion
            }

            return solutionDetails;
        }

        private void RemoveSolutionBoilerplate(ChromeDriver driver, WebDriverWait wait, string contentCssClass)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

            try
            {
                wait.Until(d => d.FindElements(By.CssSelector(".editor-wrapper__1ru6")).Count == 1);
                wait.Until(d => d.FindElements(By.CssSelector(".header__3STC")).Count == 1);

                Thread.Sleep(1000);
            }

            catch (Exception ex)
            {

            }
            Thread.Sleep(5000);


            try
            {
                wait.Until(d => d.FindElements(By.CssSelector(".read-more__3UuG")).Count > 0);
            }
            catch (Exception ex)
            {
                bool hasZero = (long)driver.ExecuteScript($"return Array.from(document.querySelectorAll('.comment-count__28iT')).filter(el => el.textContent == 'Comments: 0').length;") > 0;
                if (!hasZero)
                    throw;
            }
            Thread.Sleep(1000);


            // When we print with background graphics we get an ugly horizontal line in the bottom of first page
            driver.ExecuteScript(@"
                        // Your CSS as text
                        var styles = '::-webkit-scrollbar-thumb { background: none; }'
                        var styleSheet = document.createElement('style')
                        styleSheet.innerText = styles
                        document.head.appendChild(styleSheet)
                    ");

            string script = $@"
                var newParent = document.getElementsByClassName('main__2_tD')[0];
                var content = document.getElementsByClassName('{contentCssClass}')[0];
                newParent.innerHTML = '';
                newParent.appendChild(content);";
            js.ExecuteScript(script);
            driver.ExecuteScript($"document.querySelector('.main__2_tD').style.maxWidth='{SolutionPageWidth}px';");
            driver.ExecuteScript($"document.querySelector('.main__2_tD').style.width='{SolutionPageWidth}px';");
            Thread.Sleep(1000);

            wait.Until(d => d.FindElements(By.CssSelector($".{contentCssClass}")).Count == 1);
            wait.Until(d => d.FindElements(By.CssSelector($"body")).Count == 1);
        }


        private void CleanCodeFramesInComments(ChromeDriver driver, IWebElement? iframe, int frameIndex, WebDriverWait wait, IJavaScriptExecutor js, bool ignoreErrors)
        {
            driver.SwitchTo().DefaultContent();
            driver.SwitchTo().Frame(iframe);
            Thread.Sleep(1000);

            ReadOnlyCollection<IWebElement> langBtns = new ReadOnlyCollection<IWebElement>(new List<IWebElement>());

            try
            {
                wait.Until(d => (langBtns = d.FindElements(By.CssSelector(".lang-btn-set button.btn"))).Count() > 0);

                CleanCurrentFrame(driver);

                var clientHeight2 = (long)driver.ExecuteScript("return document.querySelector('#app').clientHeight;");

                // Go to outer html and resize the iframe
                driver.SwitchTo().DefaultContent();

                Thread.Sleep(200);
                driver.ExecuteScript("arguments[0].style.height = '5000px';", iframe);
                Thread.Sleep(200);

                int height = (int)clientHeight2;
                int padding = 50; // just to be on the safe side and show borders...

                driver.ExecuteScript($"arguments[0].style.height = '{height + padding}px';", iframe);
                Thread.Sleep(500);
                driver.ExecuteScript($"arguments[0].style.height = '{height + padding}px';", iframe);
                Thread.Sleep(500);



            }
            catch (WebDriverTimeoutException ex) when (ignoreErrors)
            {
            }
        }

        [DebuggerHidden]
        private (bool NoCode, ReadOnlyCollection<IWebElement>? codeButtons) GetCodeButtons(ChromeDriver driver, WebDriverWait wait)
        {
            bool noCode = driver.FindElements(By.CssSelector(".playground-mini-base.text-center.unavailable")).Any();
            ReadOnlyCollection<IWebElement> langBtns = null;
            try
            {
                wait.Until(d => (langBtns = d.FindElements(By.CssSelector(".lang-btn-set button.btn"))).Count() > 0);
                return (false, langBtns);
            }
            catch (WebDriverTimeoutException) when (noCode)
            {
                return (true, null); // no code, ok
            }
        }

        private void FrameInspectLanguages(ChromeDriver driver, IWebElement? iframe, int frameIndex, WebDriverWait wait, SolutionDetails solutionDetails, IJavaScriptExecutor js, bool ignoreErrors)
        {
            driver.SwitchTo().DefaultContent();
            driver.SwitchTo().Frame(iframe);
            Thread.Sleep(1000);
            solutionDetails.frames[frameIndex] ??= new SolutionIframe();


            (var noCode, ReadOnlyCollection<IWebElement> langBtns) = GetCodeButtons(driver, wait);
            if (noCode)
            {
                solutionDetails.frames[frameIndex].tabs = new SolutionIframeCodeTab[0];
                return;
            }

            try
            {
                var langs = langBtns.Select(e => e.Text).ToList();

                CleanCurrentFrame(driver);


                solutionDetails.frames[frameIndex].tabs ??= new SolutionIframeCodeTab[langs.Count];
                for (int k = 0; k < langBtns.Count; k++)
                {
                    solutionDetails.frames[frameIndex].tabs[k] = new SolutionIframeCodeTab() { language = langs[k] };
                    var langBtnToClick = langBtns[k];

                    try
                    {
                        new Actions(driver).MoveToElement(langBtnToClick).Click().Perform();
                    }
                    catch (Exception ex) when (ignoreErrors)
                    {
                        continue;
                    }
                    Thread.Sleep(1000);
                    js.ExecuteScript($"Array.from(document.querySelectorAll('.lang-btn-set button.btn')).filter(el => el.textContent != '{langs[k]}').forEach(el => el.style.display='none');");
                    var codeBlocks2 = driver.FindElements(By.CssSelector(".CodeMirror-code"));
                    if (codeBlocks2.Count == 1)
                    {
                        // Looks like this helps to get the right iframe height?
                        driver.SwitchTo().DefaultContent();

                        driver.SwitchTo().Frame(iframe);
                    }
                    else if (Debugger.IsAttached)
                        Debugger.Break();

                    js.ExecuteScript($"Array.from(document.querySelectorAll('.lang-btn-set button.btn')).forEach(el => el.style.display='inline-block');");
                }

            }
            catch (WebDriverTimeoutException ex)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
            }
        }

        private void FrameSelectLanguages(ChromeDriver driver, IWebElement? iframe, int frameIndex, WebDriverWait wait, IJavaScriptExecutor js, string lang, bool ignoreErrors)
        {
            driver.SwitchTo().DefaultContent();
            driver.SwitchTo().Frame(iframe);
            Thread.Sleep(1000);


            ReadOnlyCollection<IWebElement> langBtns = new ReadOnlyCollection<IWebElement>(new List<IWebElement>());

            try
            {
                js.ExecuteScript($"Array.from(document.querySelectorAll('.lang-btn-set button.btn')).forEach(el => el.style.display='inline-block');");
                wait.Until(d => (langBtns = d.FindElements(By.CssSelector(".lang-btn-set button.btn"))).Count() > 0);
                var langs = langBtns.Select(e => e.Text).ToList();

                js.ExecuteScript("document.querySelectorAll('.copy-code-btn').forEach(el => el.remove());");
                js.ExecuteScript("Array.from(document.querySelectorAll('.CodeMirror-vscrollbar')).forEach(el => el.remove());");
                js.ExecuteScript("Array.from(document.querySelectorAll('.CodeMirror-hscrollbar')).forEach(el => el.remove());");

                var langBtnToClick = langBtns.Single(b => b.Text == lang);


                try
                {
                    new Actions(driver).MoveToElement(langBtnToClick).Click().Perform();
                }
                catch (Exception ex) when (ignoreErrors)
                {
                }
                Thread.Sleep(1000);

                js.ExecuteScript($"Array.from(document.querySelectorAll('.lang-btn-set button.btn')).filter(el => el.textContent != '{lang}').forEach(el => el.style.display='none');");
                var codeBlocks2 = driver.FindElements(By.CssSelector(".CodeMirror-code"));
                if (codeBlocks2.Count == 1)
                {
                }
                else if (Debugger.IsAttached) // no code at all? it happens.
                    Debugger.Break();

                CleanCurrentFrame(driver);

                var clientHeight2 = (long)driver.ExecuteScript("return document.querySelector('#app').clientHeight;");

                // Go to outer html and resize the iframe
                driver.SwitchTo().DefaultContent();
                Thread.Sleep(200);
                driver.ExecuteScript("arguments[0].style.height = '5000px';", iframe);
                Thread.Sleep(200);
                
                int height = (int)clientHeight2;
                int padding = 50; // just to be on the safe side and show borders...

                driver.ExecuteScript($"arguments[0].style.height = '{height + padding}px';", iframe);
                Thread.Sleep(500);
                driver.ExecuteScript($"arguments[0].style.height = '{height + padding}px';", iframe);
                Thread.Sleep(500);
            }
            catch (WebDriverTimeoutException ex)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
            }
        }

        private void CleanCurrentFrame(ChromeDriver driver)
        {
            driver.ExecuteScript("document.querySelectorAll('.copy-code-btn').forEach(el => el.remove());");
            driver.ExecuteScript("Array.from(document.querySelectorAll('.CodeMirror-vscrollbar')).forEach(el => el.remove());");
            driver.ExecuteScript("Array.from(document.querySelectorAll('.CodeMirror-hscrollbar')).forEach(el => el.remove());");


            driver.ExecuteScript("Array.from(document.querySelectorAll('.CodeMirror-scroll, .CodeMirror-wrap, .ReactCodeMirror, .editor, .editor-base, .playground-mini-base.snippet-mode, #app')).forEach(el => el.style.height='auto');");
            driver.ExecuteScript("Array.from(document.querySelectorAll('div')).filter(el => el.textContent=='Could not connect to the reCAPTCHA service. Please check your internet connection and reload to get a reCAPTCHA challenge.').forEach(el => el.remove());");

            // bottom border was not showing (maybe it was just lack of padding?) - but adding these will show a scrollbar (overflow hidden is not setting)

            // In order to get a color pdf print, we have to remove this bootstrap rule which defines that all-print is in black
            driver.ExecuteScript("Array.from(Array.from(document.styleSheets).filter(st => st.href==='https://leetcode.com/static/bootstrap/dist/css/bootstrap.min.css?v=3.3.7')[0].cssRules).filter(rule => !!rule.cssText && rule.cssText.indexOf(\"@media print\")!=-1)[0].cssRules[0].style.color='inherit'");

        }
        #endregion

        #region Misc UI
        private int Expand(ChromeDriver driver, Func<IList<IWebElement>> getElements, bool breakIfNone)
        {
            int expanded = 0;
            var expands = new ReadOnlyCollection<IWebElement>(getElements().Reverse().ToList());
            //if (breakIfNone && expands.Count == 0 && Debugger.IsAttached)
            //    Debugger.Break();

            while (expands.Any())
            {
                foreach (var expand in expands)
                {
                    Actions actions = new Actions(driver);
                    try
                    {
                        actions.MoveToElement(expand).Click().Perform();
                        expanded++;

                        if (expanded > 50)
                        {
                            if (Debugger.IsAttached)
                                Debugger.Break();
                            return expanded;
                        }

                        Thread.Sleep(500);
                    }
                    catch (Exception ex) when (getElements().Count == 0)
                    {

                    }
                }
                expands = new ReadOnlyCollection<IWebElement>(getElements().Reverse().ToList());
            }
            return expanded;
        }
        #endregion
        
        #region Files and Folders
        public string LanguageToFileName(string language) => language.Replace("MS SQL Server", "MSSQL").Replace(" ", "");
        public string GetFileName(Problem problem, string fileType, string? lang = null)
        {
            if (lang == null)
                return $"{GetProblemFolder(problem)}\\{problem.stat.frontend_question_id.ToString("0000")}-{fileType}.pdf";
            else
                return $"{GetProblemFolder(problem)}\\{problem.stat.frontend_question_id.ToString("0000")}-{fileType}-{LanguageToFileName(lang!)}.pdf";
        }
        private string GetProblemFolder(Problem problem)
        {
            return Path.Combine(_baseFolder.FullName, $"{problem.stat.frontend_question_id.ToString("0000")}-{problem.stat.question__title_slug}");
        }
        public List<FileInfo> GetQuestionPdfs(Problem problem)
        {
            string folder = GetProblemFolder(problem);
            return Directory.Exists(folder) ? new DirectoryInfo(folder).GetFiles("*-Question-*.pdf").ToList() : new List<FileInfo>();
        }
        public List<FileInfo> GetSolutionPdfs(Problem problem)
        {
            string folder = GetProblemFolder(problem);
            return Directory.Exists(folder) ? new DirectoryInfo(folder).GetFiles("*-Solution-*.pdf").Where(f => !f.FullName.Contains("-Comments")).ToList() : new List<FileInfo>();
        }
        public List<FileInfo> GetSolutionCommentsPdfs(Problem problem)
        {
            string folder = GetProblemFolder(problem);
            return Directory.Exists(folder) ? new DirectoryInfo(folder).GetFiles("*-Solution-Comments*.pdf").ToList() : new List<FileInfo>();
        }
        #endregion


        #region PDF Printing
        private void SaveAsPdf(ChromeDriver driver, string filename, bool overwrite, bool launch)
        {
            var dir = new FileInfo(filename).Directory;
            if (!dir.Exists)
                dir.Create();

            try
            {
                if (overwrite || !File.Exists(filename))
                {
                    // Output a PDF of the first page in A4 size at 90% scale
                    var printOptions = new Dictionary<string, object>
                    {
                        { "paperWidth", 210 / 25.4 }, // A4, but default is 8.5in
                        { "paperHeight", 297 / 25.4 }, // A4, but default is 11in
                        //{ "scale", 0.9 }, // default is 1
                        { "marginBottom", 0 }, // default is 1cm
                        { "marginTop", 0 }, // default is 1cm
                        //{ "pageRanges", "1" }
                        {"printBackground", true},
                    };

                    var printOutput = (Dictionary<string, object>)driver.ExecuteCdpCommand("Page.printToPDF", printOptions);
                    var pdf = Convert.FromBase64String((string)printOutput["data"]);
                    try
                    {
                        File.WriteAllBytes(filename, pdf);
                        if (launch)
                            Process.Start(new ProcessStartInfo(new FileInfo(filename).FullName) { UseShellExecute = true });
                    }
                    catch (IOException)
                    {
                        // file already in use.
                        if (Debugger.IsAttached)
                            Debugger.Break();
                    }
                }
            }
            catch (WebDriverException ex) when (ex.Message.Contains("Printing is not available"))
            {
                // non-headless chrome can't print to pdf, ignore.
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        #endregion

    }
}
