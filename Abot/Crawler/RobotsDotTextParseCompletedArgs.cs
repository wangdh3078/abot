using Abot.Poco;
using Robots;

namespace Abot.Crawler
{
    /// <summary>
    ///成功解析后保存机器人TXT数据的类
    /// </summary>
    public class RobotsDotTextParseCompletedArgs : CrawlArgs
    {
        /// <summary>
        /// robots.txt对象
        /// </summary>
        public IRobots Robots { get; set; }

        /// <summary>
        /// Contructor to be used to create an object which will path arugments when robots txt is parsed
        /// </summary>
        /// <param name="crawlContext"></param>
        /// <param name="robots"></param>
        public RobotsDotTextParseCompletedArgs(CrawlContext crawlContext, IRobots robots) : base(crawlContext)
        {
            Robots = robots;
        }
    }
}
