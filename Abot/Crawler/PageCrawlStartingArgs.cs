using Abot.Poco;
using System;

namespace Abot.Crawler
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class PageCrawlStartingArgs : CrawlArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public PageToCrawl PageToCrawl { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="crawlContext"></param>
        /// <param name="pageToCrawl"></param>

        public PageCrawlStartingArgs(CrawlContext crawlContext, PageToCrawl pageToCrawl)
            : base(crawlContext)
        {
            if (pageToCrawl == null)
                throw new ArgumentNullException("pageToCrawl");

            PageToCrawl = pageToCrawl;
        }
    }
}
