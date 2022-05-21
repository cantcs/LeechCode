using LeechCode;
using static LeechCode.ChromeHelper;

// if you have Leetcode Premium fill these values; Empty values will just crawl public data.
string userName = "";
string password = "";

var baseFolder = new DirectoryInfo(@"..\..\..\..\..\LeechCode-Problems");
var chromeHelper = new ChromeHelper(new ChromeHelperOptions(userName, password)); 
var chromePool = new ChromePool(() => chromeHelper.CreateChromeDriver());
var crawler = new Crawler(baseFolder, chromeHelper);


var orchestrator = new CrawlerOrchestrator(crawler, chromeHelper, chromePool);


/************************************************************/
/****************** Crawl single solution: ******************/
/************************************************************/
// crawler.AutoLaunch = true;
// orchestrator.CrawlByUrl("https://leetcode.com/problems/two-sum/"); 
// Hint: to debug Chrome elements use "chromeHelper.Options.Headless = true;" before crawling.
/************************************************************/





/*********************************************************************************************************************************************************/
/****************** Fetch metadata with all problems, then crawl ALL questions and ALL solutions, while updating back the metadata json ******************/
/*********************************************************************************************************************************************************/

// At first execution this will load problems from the API, on next executions this will load the problems from a JSON cache which also keeps extra metadata used in the crawling process
var allProblemsMetadata = await crawler.LoadProblems();
var problemsToCrawl = allProblemsMetadata; // you may prefer to run in batches - like .Skip(0).Take(100), etc.

var options = new ParallelOptions() { MaxDegreeOfParallelism = 5 }; // if you're debugging reduce this to 1
orchestrator.UpdateMetadata = () => crawler.SaveProblemsMetadata(allProblemsMetadata);


// These commands below are helpful for debugging:
//problemsToCrawl = allProblemsMetadata.Where(x => x.something...).Skip(100).Take(100).ToList();
//chromeHelper.Options.Headless = false;
//crawler.AutoLaunch = true;
//options.MaxDegreeOfParallelism = 1;


Parallel.ForEach(problemsToCrawl, options, (problem, state, i) =>
{
    orchestrator.CrawlQuestionAndSolution(problem);
});
/*********************************************************************************************************************************************************/


/************************************************************/
/****************** Crawl companies lists: ******************/
/************************************************************/
// crawler.AutoLaunch = true;
//var driver = chromeHelper.CreateChromeDriver(); // log in
//var companies = await crawler.FetchAllCompanies(chromeHelper);
//var path = Path.Combine(baseFolder.FullName, "Companies\\Companies.json");
//if (!new FileInfo(path).Directory.Exists) new FileInfo(path).Directory.Create();
//File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(companies, Newtonsoft.Json.Formatting.Indented));
//foreach (var company in companies)
//    crawler.ScrapeCompanyProblems(driver, company.Slug);
// Hint: to debug Chrome elements use "chromeHelper.Options.Headless = true;" before crawling.
/************************************************************/
