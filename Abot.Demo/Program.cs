
using Abot.Crawler;
using Abot.Poco;
using System;

namespace Abot.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            PrintDisclaimer();

            Uri uriToCrawl = GetSiteToCrawl(args);

            IWebCrawler crawler;

            crawler = GetDefaultWebCrawler();
            //crawler = GetManuallyConfiguredWebCrawler();
            //crawler = GetCustomBehaviorUsingLambdaWebCrawler();

            //订阅任何这些异步事件，还有每个异步事件的同步版本。
            //可以在此处处理有关抓取的特定事件的数据
            crawler.PageCrawlStartingAsync += crawler_ProcessPageCrawlStarting;
            crawler.PageCrawlCompletedAsync += crawler_ProcessPageCrawlCompleted;
            crawler.PageCrawlDisallowedAsync += crawler_PageCrawlDisallowed;
            crawler.PageLinksCrawlDisallowedAsync += crawler_PageLinksCrawlDisallowed;

            //开始抓取
            //这是一个同步调用
            CrawlResult result = crawler.Crawl(uriToCrawl);

            //现在，查看与该可执行文件在同一目录中的log.txt文件。 
            //它具有您在控制台窗口中尝试读取的所有语句
            //没有足够的数据被记录？ 将app.config文件log4net日志级别从“INFO”更改为“DEBUG”

            PrintDisclaimer();
        }
        /// <summary>
        /// 获取默认Web爬虫程序
        /// </summary>
        /// <returns></returns>
        private static IWebCrawler GetDefaultWebCrawler()
        {
            return new PoliteWebCrawler();
        }
        /// <summary>
        /// 获取手动配置的Web爬虫程序
        /// </summary>
        /// <returns></returns>
        private static IWebCrawler GetManuallyConfiguredWebCrawler()
        {
            //手动创建配置对象
            CrawlConfiguration config = new CrawlConfiguration();
            config.CrawlTimeoutSeconds = 0;
            config.DownloadableContentTypes = "text/html, text/plain";
            config.IsExternalPageCrawlingEnabled = false;
            config.IsExternalPageLinksCrawlingEnabled = false;
            config.IsRespectRobotsDotTextEnabled = false;
            config.IsUriRecrawlingEnabled = false;
            config.MaxConcurrentThreads = 10;
            config.MaxPagesToCrawl = 10;
            config.MaxPagesToCrawlPerDomain = 0;
            config.MinCrawlDelayPerDomainMilliSeconds = 1000;

            //Add you own values without modifying Abot's source code.
            //These are accessible in CrawlContext.CrawlConfuration.ConfigurationException object throughout the crawl
            config.ConfigurationExtensions.Add("Somekey1", "SomeValue1");
            config.ConfigurationExtensions.Add("Somekey2", "SomeValue2");

            //Initialize the crawler with custom configuration created above.
            //This override the app.config file values
            return new PoliteWebCrawler(config, null, null, null, null, null, null, null, null);
        }

        private static IWebCrawler GetCustomBehaviorUsingLambdaWebCrawler()
        {
            IWebCrawler crawler = GetDefaultWebCrawler();

            //Register a lambda expression that will make Abot not crawl any url that has the word "ghost" in it.
            //For example http://a.com/ghost, would not get crawled if the link were found during the crawl.
            //If you set the log4net log level to "DEBUG" you will see a log message when any page is not allowed to be crawled.
            //NOTE: This is lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPage method is run.
            crawler.ShouldCrawlPage((pageToCrawl, crawlContext) =>
            {
                if (pageToCrawl.Uri.AbsoluteUri.Contains("ghost"))
                    return new CrawlDecision { Allow = false, Reason = "Scared of ghosts" };

                return new CrawlDecision { Allow = true };
            });

            //Register a lambda expression that will tell Abot to not download the page content for any page after 5th.
            //Abot will still make the http request but will not read the raw content from the stream
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldDownloadPageContent method is run
            crawler.ShouldDownloadPageContent((crawledPage, crawlContext) =>
            {
                if (crawlContext.CrawledCount >= 5)
                    return new CrawlDecision { Allow = false, Reason = "We already downloaded the raw page content for 5 pages" };

                return new CrawlDecision { Allow = true };
            });

            //Register a lambda expression that will tell Abot to not crawl links on any page that is not internal to the root uri.
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPageLinks method is run
            crawler.ShouldCrawlPageLinks((crawledPage, crawlContext) =>
            {
                if (!crawledPage.IsInternal)
                    return new CrawlDecision { Allow = false, Reason = "We dont crawl links of external pages" };

                return new CrawlDecision { Allow = true };
            });

            return crawler;
        }
        /// <summary>
        /// 获取抓取站点
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Uri GetSiteToCrawl(string[] args)
        {
            string userInput = "";
            if (args.Length < 1)
            {
                Console.WriteLine("请输入绝对网址抓取：");
                userInput = Console.ReadLine();
            }
            else
            {
                userInput = args[0];
            }

            if (string.IsNullOrWhiteSpace(userInput))
            {
                throw new ApplicationException("网站URL是一个必需的参数");
            }
            return new Uri(userInput);
        }
        /// <summary>
        /// 打印免责声明
        /// </summary>
        private static void PrintDisclaimer()
        {
            PrintAttentionText("The demo is configured to only crawl a total of 10 pages and will wait 1 second in between http requests. This is to avoid getting you blocked by your isp or the sites you are trying to crawl. You can change these values in the app.config or Abot.Console.exe.config file.");
        }

        private static void PrintAttentionText(string text)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
            Console.ForegroundColor = originalColor;
        }
        /// <summary>
        /// 抓取页面之前触发的异步事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            //Process data
        }
        /// <summary>
        /// 抓取单个页面时触发的异步事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            //Process data
        }
        /// <summary>
        /// 当ICrawlDecisionMaker.ShouldCrawlLinks impl返回false时触发异步事件。 这意味着页面的链接没有被抓取
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            //Process data
        }
        /// <summary>
        /// 当ICrawlDecisionMaker.ShouldCrawl impl返回false时触发的异步事件。 这意味着该页面或其链接未被抓取。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            //Process data
        }
    }
}
