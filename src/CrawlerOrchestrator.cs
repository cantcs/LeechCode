using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeechCode
{
    public class CrawlerOrchestrator
    {
        private Crawler _crawler;
        ChromeHelper _chromeHelper;
        ChromePool _chromePool;
        public Action? UpdateMetadata { get; set; }

        public CrawlerOrchestrator(Crawler crawler, ChromeHelper chromeHelper, ChromePool chromePool)
        {
            this._crawler = crawler;
            this._chromeHelper = chromeHelper;
            _chromePool = chromePool;
        }

        public void CrawlByUrl(string url)
        {
            string slug = url.Replace("https://leetcode.com/problems/", "").TrimEnd('/');
            CrawlQuestionAndSolution(new Problem() { stat = new Stat() { question__title_slug = slug } });
        }

        public void CrawlQuestionAndSolution(Problem problem, bool forceRefresh = false)
        {
            ChromeDriver? chrome = null;

            try
            {

                List<FileInfo> questionPdfs = _crawler.GetQuestionPdfs(problem);
                List<FileInfo> solutionPdfs = _crawler.GetSolutionPdfs(problem);
                List<FileInfo> solutionCommentsPdfs = _crawler.GetSolutionCommentsPdfs(problem);

                #region Question
                string questionLanguage = _crawler.GetPreferredQuestionLanguage(problem.question_details?.languages) ?? "C#";
                string filename = _crawler.GetFileName(problem, "Question", lang: questionLanguage);
                if (!File.Exists(filename) || problem.question_details == null || forceRefresh)
                {
                    chrome ??= _chromePool.GetOrCreateChromeDriver();
                    QuestionDetails? questionDetails = _crawler.ScrapeQuestion(chrome, problem, loggedIn: _chromeHelper.LoggedIn);
                    problem.question_details = questionDetails;
                    UpdateMetadata?.Invoke();
                }
                #endregion

                #region Solution
                if (!solutionPdfs.Any() && problem.question_details?.has_solution == false)
                    return;

                if (problem.solution_details == null || problem.solution_details?.last_fetch == null || solutionPdfs.Count() == 0 || (_crawler.GetSolutionsInAllLanguages && problem.solution_details.languages?.Count > solutionPdfs.Count()) || solutionCommentsPdfs.Count < 2 || forceRefresh)
                {
                    SolutionDetails? solutionDetails;

                    chrome ??= _chromePool.GetOrCreateChromeDriver();

                    // FIRST PASS
                    // HACK: Looks like doing two passes works better for getting iframe heights correctly, or else they get truncated
                    // not sure if this is still required now that we're using clientHeight+scrollHeight of multiple elements
                    bool ignoreErrors = false;
                    try
                    {
                        solutionDetails = _crawler.ScrapeSolution(chrome, problem, onlyFirstPass: true, ignoreErrors: ignoreErrors);
                    }
                    catch (Exception ex) // looks like we're getting some exceptions while clicking language tabs... ignore
                    {
                        Console.WriteLine(ex.Message);
                        ignoreErrors = true;
                        solutionDetails = _crawler.ScrapeSolution(chrome, problem, onlyFirstPass: true, ignoreErrors: ignoreErrors);
                    }

                    // SECOND PASS
                    ignoreErrors = false;
                    try
                    {
                        solutionDetails = _crawler.ScrapeSolution(chrome, problem, onlyFirstPass: false, ignoreErrors: ignoreErrors); // two passes seem to be the best...
                    }
                    catch (Exception ex) // looks like we're getting some exceptions while clicking language tabs... ignore
                    {
                        ignoreErrors = true;
                        bool previousAutoLaunch = _crawler.AutoLaunch;
                        _crawler.AutoLaunch = true; // to review if PDF looks good!
                        solutionDetails = _crawler.ScrapeSolution(chrome, problem, onlyFirstPass: false, ignoreErrors: ignoreErrors);
                        _crawler.AutoLaunch = previousAutoLaunch;
                    }

                    problem.solution_details = solutionDetails;
                    UpdateMetadata?.Invoke();
                }

                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                _chromePool.ReturnChromeDriver(chrome); chrome = null;
            }
        }

    }
}
