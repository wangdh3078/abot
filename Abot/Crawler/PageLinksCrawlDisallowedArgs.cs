using Abot.Poco;
using System;

namespace Abot.Crawler
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class PageLinksCrawlDisallowedArgs : PageCrawlCompletedArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public string DisallowedReason { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="crawlContext"></param>
        /// <param name="crawledPage"></param>
        /// <param name="disallowedReason"></param>

        public PageLinksCrawlDisallowedArgs(CrawlContext crawlContext, CrawledPage crawledPage, string disallowedReason)
            : base(crawlContext, crawledPage)
        {
            if (string.IsNullOrWhiteSpace(disallowedReason))
                throw new ArgumentNullException("disallowedReason");

            DisallowedReason = disallowedReason;
        }
    }
}
