using Abot.Poco;
using System;

namespace Abot.Crawler
{
    /// <summary>
    /// 网页抓取完成参数
    /// </summary>
    [Serializable]
    public class PageCrawlCompletedArgs : CrawlArgs
    {
        /// <summary>
        /// 抓去过的网页
        /// </summary>
        public CrawledPage CrawledPage { get; private set; }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="crawlContext">抓取上下文</param>
        /// <param name="crawledPage">抓取页面</param>

        public PageCrawlCompletedArgs(CrawlContext crawlContext, CrawledPage crawledPage)
            : base(crawlContext)
        {
            if (crawledPage == null)
                throw new ArgumentNullException("crawledPage");

            CrawledPage = crawledPage;
        }
    }
}
