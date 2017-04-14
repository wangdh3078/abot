using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Reflection;
using System.Threading;
using System.Timers;
using Abot.Core;
using Abot.Poco;
using Abot.Util;
using log4net;
using Timer = System.Timers.Timer;

namespace Abot.Crawler
{
    /// <summary>
    /// 网络爬虫接口
    /// </summary>
    public interface IWebCrawler : IDisposable
    {
        /// <summary>
        /// 在页面被抓取前被触发的同步事件。
        /// </summary>
        event EventHandler<PageCrawlStartingArgs> PageCrawlStarting;

        /// <summary>
        /// 当单个页面被抓取时触发的同步事件。
        /// </summary>
        event EventHandler<PageCrawlCompletedArgs> PageCrawlCompleted;

        /// <summary>
        /// 当ICrawlDecisionMaker.ShouldCrawl impl返回false时触发的同步事件。 这意味着该页面或其链接未被抓取。
        /// </summary>
        event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowed;

        /// <summary>
        /// 当ICrawlDecisionMaker.ShouldCrawlLinks impl返回false时触发的同步事件。 这意味着页面的链接没有被抓取。
        /// </summary>
        event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowed;

        /// <summary>
        /// 抓取页面之前触发的异步事件。
        /// </summary>
        event EventHandler<PageCrawlStartingArgs> PageCrawlStartingAsync;

        /// <summary>
        /// 抓取单个页面时触发的异步事件。
        /// </summary>
        event EventHandler<PageCrawlCompletedArgs> PageCrawlCompletedAsync;

        /// <summary>
        /// 当ICrawlDecisionMaker.ShouldCrawl impl返回false时触发的异步事件。 这意味着该页面或其链接未被抓取。
        /// </summary>
        event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowedAsync;

        /// <summary>
        /// 当ICrawlDecisionMaker.ShouldCrawlLinks impl返回false时触发异步事件。 这意味着页面的链接没有被抓取。
        /// </summary>
        event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowedAsync;

        /// <summary>
        /// 注册要调用的委托以确定是否应抓取页面的同步方法
        /// </summary>
        void ShouldCrawlPage(Func<PageToCrawl, CrawlContext, CrawlDecision> decisionMaker);

        /// <summary>
        ///同步方法，注册要调用的委托以确定页面的内容是否应该被下载
        /// </summary>
        /// <param name="decisionMaker"></param>
        void ShouldDownloadPageContent(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker);

        /// <summary>
        /// 注册要调用的委托以确定是否应抓取页面的链接的同步方法
        /// </summary>
        /// <param name="decisionMaker"></param>
        void ShouldCrawlPageLinks(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker);

        /// <summary>
        /// 注册要调用的委托的同步方法，以确定页面上的某个链接是否应该被调度为被爬网
        /// </summary>
        void ShouldScheduleLink(Func<Uri, CrawledPage, CrawlContext, bool> decisionMaker);

        /// <summary>
        /// 注册要调用的委托以确定是否应重新抓取页面的同步方法
        /// </summary>
        void ShouldRecrawlPage(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker);

        /// <summary>
        /// 同步方法，注册要调用的委托以确定第一个uri参数是否被认为是第二个uri参数的内部uri
        /// </summary>
        /// <param name="decisionMaker delegate"></param>
        void IsInternalUri(Func<Uri, Uri, bool> decisionMaker);

        /// <summary>
        /// 使用uri参数开始爬行
        /// </summary>
        CrawlResult Crawl(Uri uri);

        /// <summary>
        ///使用uri参数开始爬网，可以使用CancellationToken取消
        /// </summary>
        CrawlResult Crawl(Uri uri, CancellationTokenSource tokenSource);

        /// <summary>
        /// 动态对象可以容纳需要在爬网上下文中可用的任何值
        /// </summary>
        dynamic CrawlBag { get; set; }
    }
    /// <summary>
    /// 网络爬虫
    /// </summary>
    [Serializable]
    public abstract class WebCrawler : IWebCrawler
    {
        static ILog _logger = LogManager.GetLogger("AbotLogger");
        /// <summary>
        /// 抓取完成
        /// </summary>
        protected bool _crawlComplete = false;
        /// <summary>
        ///爬网停止报告 
        /// </summary>
        protected bool _crawlStopReported = false;
        /// <summary>
        /// 爬行取消报告
        /// </summary>
        protected bool _crawlCancellationReported = false;
        /// <summary>
        /// 
        /// </summary>
        protected bool _maxPagesToCrawlLimitReachedOrScheduled = false;
        /// <summary>
        /// 超时时间
        /// </summary>
        protected Timer _timeoutTimer;
        /// <summary>
        /// 抓取结果
        /// </summary>
        protected CrawlResult _crawlResult = null;
        /// <summary>
        /// 抓取内容
        /// </summary>
        protected CrawlContext _crawlContext;
        /// <summary>
        /// 线程管理
        /// </summary>
        protected IThreadManager _threadManager;
        /// <summary>
        /// 调度器
        /// </summary>
        protected IScheduler _scheduler;
        /// <summary>
        /// 页面请求者
        /// </summary>
        protected IPageRequester _pageRequester;
        /// <summary>
        /// 超链接解析器
        /// </summary>
        protected IHyperLinkParser _hyperLinkParser;
        /// <summary>
        /// 抓取决策者
        /// </summary>
        protected ICrawlDecisionMaker _crawlDecisionMaker;
        /// <summary>
        /// 内存管理
        /// </summary>
        protected IMemoryManager _memoryManager;
        /// <summary>
        /// 
        /// </summary>
        protected Func<PageToCrawl, CrawlContext, CrawlDecision> _shouldCrawlPageDecisionMaker;
        /// <summary>
        /// 
        /// </summary>
        protected Func<CrawledPage, CrawlContext, CrawlDecision> _shouldDownloadPageContentDecisionMaker;
        /// <summary>
        /// 
        /// </summary>
        protected Func<CrawledPage, CrawlContext, CrawlDecision> _shouldCrawlPageLinksDecisionMaker;
        /// <summary>
        /// 
        /// </summary>
        protected Func<CrawledPage, CrawlContext, CrawlDecision> _shouldRecrawlPageDecisionMaker;
        /// <summary>
        /// 
        /// </summary>
        protected Func<Uri, CrawledPage, CrawlContext, bool> _shouldScheduleLinkDecisionMaker;
        /// <summary>
        /// 是内部决策者
        /// </summary>
        protected Func<Uri, Uri, bool> _isInternalDecisionMaker = (uriInQuestion, rootUri) => uriInQuestion.Authority == rootUri.Authority;


        /// <summary>
        /// 动态对象可以容纳需要在爬网上下文中可用的任何值
        /// </summary>
        public dynamic CrawlBag { get; set; }

        #region 构造函数

        static WebCrawler()
        {
            //这是处理URL中的句点的解决方法（http://stackoverflow.com/questions/856885/httpwebrequest-to-url-with-dot-at-the-end）
            //当该项目升级到4.5时，不需要
            MethodInfo getSyntax = typeof(UriParser).GetMethod("GetSyntax", BindingFlags.Static | BindingFlags.NonPublic);
            FieldInfo flagsField = typeof(UriParser).GetField("m_Flags", BindingFlags.Instance | BindingFlags.NonPublic);
            if (getSyntax != null && flagsField != null)
            {
                foreach (string scheme in new[] { "http", "https" })
                {
                    UriParser parser = (UriParser)getSyntax.Invoke(null, new object[] { scheme });
                    if (parser != null)
                    {
                        int flagsValue = (int)flagsField.GetValue(parser);
                        // 清除CanonicalizeAsFilePath属性
                        if ((flagsValue & 0x1000000) != 0)
                            flagsField.SetValue(parser, flagsValue & ~0x1000000);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a crawler instance with the default settings and implementations.
        /// </summary>
        public WebCrawler()
            : this(null, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Creates a crawler instance with custom settings or implementation. Passing in null for all params is the equivalent of the empty constructor.
        /// </summary>
        /// <param name="threadManager">Distributes http requests over multiple threads</param>
        /// <param name="scheduler">Decides what link should be crawled next</param>
        /// <param name="pageRequester">Makes the raw http requests</param>
        /// <param name="hyperLinkParser">Parses a crawled page for it's hyperlinks</param>
        /// <param name="crawlDecisionMaker">Decides whether or not to crawl a page or that page's links</param>
        /// <param name="crawlConfiguration">Configurable crawl values</param>
        /// <param name="memoryManager">Checks the memory usage of the host process</param>
        public WebCrawler(
            CrawlConfiguration crawlConfiguration,
            ICrawlDecisionMaker crawlDecisionMaker,
            IThreadManager threadManager,
            IScheduler scheduler,
            IPageRequester pageRequester,
            IHyperLinkParser hyperLinkParser,
            IMemoryManager memoryManager)
        {
            _crawlContext = new CrawlContext();
            _crawlContext.CrawlConfiguration = crawlConfiguration ?? GetCrawlConfigurationFromConfigFile();
            CrawlBag = _crawlContext.CrawlBag;

            _threadManager = threadManager ?? new TaskThreadManager(_crawlContext.CrawlConfiguration.MaxConcurrentThreads > 0 ? _crawlContext.CrawlConfiguration.MaxConcurrentThreads : Environment.ProcessorCount);
            _scheduler = scheduler ?? new Scheduler(_crawlContext.CrawlConfiguration.IsUriRecrawlingEnabled, null, null);
            _pageRequester = pageRequester ?? new PageRequester(_crawlContext.CrawlConfiguration);
            _crawlDecisionMaker = crawlDecisionMaker ?? new CrawlDecisionMaker();

            if (_crawlContext.CrawlConfiguration.MaxMemoryUsageInMb > 0
                || _crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb > 0)
                _memoryManager = memoryManager ?? new MemoryManager(new CachedMemoryMonitor(new GcMemoryMonitor(), _crawlContext.CrawlConfiguration.MaxMemoryUsageCacheTimeInSeconds));

            _hyperLinkParser = hyperLinkParser ?? new HapHyperLinkParser(_crawlContext.CrawlConfiguration, null);

            _crawlContext.Scheduler = _scheduler;
        }

        #endregion Constructors

        /// <summary>
        /// 使用uri参数开始同步爬网，订阅事件来处理数据，因为它可用
        /// </summary>
        public virtual CrawlResult Crawl(Uri uri)
        {
            return Crawl(uri, null);
        }

        /// <summary>
        /// 使用uri参数开始同步爬网，订阅事件来处理数据，因为它可用
        /// </summary>
        public virtual CrawlResult Crawl(Uri uri, CancellationTokenSource cancellationTokenSource)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            _crawlContext.RootUri = _crawlContext.OriginalRootUri = uri;

            if (cancellationTokenSource != null)
                _crawlContext.CancellationTokenSource = cancellationTokenSource;

            _crawlResult = new CrawlResult();
            _crawlResult.RootUri = _crawlContext.RootUri;
            _crawlResult.CrawlContext = _crawlContext;
            _crawlComplete = false;

            _logger.InfoFormat("About to crawl site [{0}]", uri.AbsoluteUri);
            PrintConfigValues(_crawlContext.CrawlConfiguration);

            if (_memoryManager != null)
            {
                _crawlContext.MemoryUsageBeforeCrawlInMb = _memoryManager.GetCurrentUsageInMb();
                _logger.InfoFormat("Starting memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, _crawlContext.MemoryUsageBeforeCrawlInMb);
            }

            _crawlContext.CrawlStartDate = DateTime.Now;
            Stopwatch timer = Stopwatch.StartNew();

            if (_crawlContext.CrawlConfiguration.CrawlTimeoutSeconds > 0)
            {
                _timeoutTimer = new Timer(_crawlContext.CrawlConfiguration.CrawlTimeoutSeconds * 1000);
                _timeoutTimer.Elapsed += HandleCrawlTimeout;
                _timeoutTimer.Start();
            }

            try
            {
                PageToCrawl rootPage = new PageToCrawl(uri) { ParentUri = uri, IsInternal = true, IsRoot = true };
                if (ShouldSchedulePageLink(rootPage))
                    _scheduler.Add(rootPage);

                VerifyRequiredAvailableMemory();
                CrawlSite();
            }
            catch (Exception e)
            {
                _crawlResult.ErrorException = e;
                _logger.FatalFormat("An error occurred while crawling site [{0}]", uri);
                _logger.Fatal(e);
            }
            finally
            {
                if (_threadManager != null)
                    _threadManager.Dispose();
            }

            if (_timeoutTimer != null)
                _timeoutTimer.Stop();

            timer.Stop();

            if (_memoryManager != null)
            {
                _crawlContext.MemoryUsageAfterCrawlInMb = _memoryManager.GetCurrentUsageInMb();
                _logger.InfoFormat("Ending memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, _crawlContext.MemoryUsageAfterCrawlInMb);
            }

            _crawlResult.Elapsed = timer.Elapsed;
            _logger.InfoFormat("Crawl complete for site [{0}]: Crawled [{1}] pages in [{2}]", _crawlResult.RootUri.AbsoluteUri, _crawlResult.CrawlContext.CrawledCount, _crawlResult.Elapsed);

            return _crawlResult;
        }

        #region 同步事件

        /// <summary>
        /// 抓取页面之前触发的同步事件。
        /// </summary>
        public event EventHandler<PageCrawlStartingArgs> PageCrawlStarting;

        /// <summary>
        /// 抓取单个页面时触发的同步事件。
        /// </summary>
        public event EventHandler<PageCrawlCompletedArgs> PageCrawlCompleted;

        /// <summary>
        ///当ICrawlDecisionMaker.ShouldCrawl impl返回false时触发的同步事件。 这意味着该页面或其链接未被抓取。
        /// </summary>
        public event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowed;

        /// <summary>
        /// 当ICrawlDecisionMaker.ShouldCrawlLinks impl返回false时触发的同步事件。 这意味着页面的链接没有被抓取。
        /// </summary>
        public event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowed;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageToCrawl"></param>
        protected virtual void FirePageCrawlStartingEvent(PageToCrawl pageToCrawl)
        {
            try
            {
                EventHandler<PageCrawlStartingArgs> threadSafeEvent = PageCrawlStarting;
                if (threadSafeEvent != null)
                    threadSafeEvent(this, new PageCrawlStartingArgs(_crawlContext, pageToCrawl));
            }
            catch (Exception e)
            {
                _logger.Error("An unhandled exception was thrown by a subscriber of the PageCrawlStarting event for url:" + pageToCrawl.Uri.AbsoluteUri);
                _logger.Error(e);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="crawledPage"></param>
        protected virtual void FirePageCrawlCompletedEvent(CrawledPage crawledPage)
        {
            try
            {
                EventHandler<PageCrawlCompletedArgs> threadSafeEvent = PageCrawlCompleted;
                if (threadSafeEvent != null)
                    threadSafeEvent(this, new PageCrawlCompletedArgs(_crawlContext, crawledPage));
            }
            catch (Exception e)
            {
                _logger.Error("An unhandled exception was thrown by a subscriber of the PageCrawlCompleted event for url:" + crawledPage.Uri.AbsoluteUri);
                _logger.Error(e);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageToCrawl"></param>
        /// <param name="reason"></param>
        protected virtual void FirePageCrawlDisallowedEvent(PageToCrawl pageToCrawl, string reason)
        {
            try
            {
                EventHandler<PageCrawlDisallowedArgs> threadSafeEvent = PageCrawlDisallowed;
                if (threadSafeEvent != null)
                    threadSafeEvent(this, new PageCrawlDisallowedArgs(_crawlContext, pageToCrawl, reason));
            }
            catch (Exception e)
            {
                _logger.Error("An unhandled exception was thrown by a subscriber of the PageCrawlDisallowed event for url:" + pageToCrawl.Uri.AbsoluteUri);
                _logger.Error(e);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="crawledPage"></param>
        /// <param name="reason"></param>
        protected virtual void FirePageLinksCrawlDisallowedEvent(CrawledPage crawledPage, string reason)
        {
            try
            {
                EventHandler<PageLinksCrawlDisallowedArgs> threadSafeEvent = PageLinksCrawlDisallowed;
                if (threadSafeEvent != null)
                    threadSafeEvent(this, new PageLinksCrawlDisallowedArgs(_crawlContext, crawledPage, reason));
            }
            catch (Exception e)
            {
                _logger.Error("An unhandled exception was thrown by a subscriber of the PageLinksCrawlDisallowed event for url:" + crawledPage.Uri.AbsoluteUri);
                _logger.Error(e);
            }
        }

        #endregion

        #region 异步事件

        /// <summary>
        /// 抓取页面之前触发的异步事件。
        /// </summary>
        public event EventHandler<PageCrawlStartingArgs> PageCrawlStartingAsync;

        /// <summary>
        /// 抓取单个页面时触发的异步事件。
        /// </summary>
        public event EventHandler<PageCrawlCompletedArgs> PageCrawlCompletedAsync;

        /// <summary>
        /// 当ICrawlDecisionMaker.ShouldCrawl impl返回false时触发的异步事件。 这意味着该页面或其链接未被抓取。
        /// </summary>
        public event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowedAsync;

        /// <summary>
        /// 当ICrawlDecisionMaker.ShouldCrawlLinks impl返回false时触发异步事件。 这意味着页面的链接没有被抓取。
        /// </summary>
        public event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowedAsync;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageToCrawl"></param>
        protected virtual void FirePageCrawlStartingEventAsync(PageToCrawl pageToCrawl)
        {
            EventHandler<PageCrawlStartingArgs> threadSafeEvent = PageCrawlStartingAsync;
            if (threadSafeEvent != null)
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageCrawlStartingArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageCrawlStartingArgs(_crawlContext, pageToCrawl), null, null);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="crawledPage"></param>
        protected virtual void FirePageCrawlCompletedEventAsync(CrawledPage crawledPage)
        {
            EventHandler<PageCrawlCompletedArgs> threadSafeEvent = PageCrawlCompletedAsync;

            if (threadSafeEvent == null)
                return;

            if (_scheduler.Count == 0)
            {
                //必须同步触发，以避免主线程在完成第一个或最后一个页面爬行的事件处理程序之前退出
                try
                {
                    threadSafeEvent(this, new PageCrawlCompletedArgs(_crawlContext, crawledPage));
                }
                catch (Exception e)
                {
                    _logger.Error("An unhandled exception was thrown by a subscriber of the PageCrawlCompleted event for url:" + crawledPage.Uri.AbsoluteUri);
                    _logger.Error(e);
                }
            }
            else
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageCrawlCompletedArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageCrawlCompletedArgs(_crawlContext, crawledPage), null, null);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageToCrawl"></param>
        /// <param name="reason"></param>
        protected virtual void FirePageCrawlDisallowedEventAsync(PageToCrawl pageToCrawl, string reason)
        {
            EventHandler<PageCrawlDisallowedArgs> threadSafeEvent = PageCrawlDisallowedAsync;
            if (threadSafeEvent != null)
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageCrawlDisallowedArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageCrawlDisallowedArgs(_crawlContext, pageToCrawl, reason), null, null);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="crawledPage"></param>
        /// <param name="reason"></param>
        protected virtual void FirePageLinksCrawlDisallowedEventAsync(CrawledPage crawledPage, string reason)
        {
            EventHandler<PageLinksCrawlDisallowedArgs> threadSafeEvent = PageLinksCrawlDisallowedAsync;
            if (threadSafeEvent != null)
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageLinksCrawlDisallowedArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageLinksCrawlDisallowedArgs(_crawlContext, crawledPage, reason), null, null);
                }
            }
        }

        #endregion


        /// <summary>
        /// 注册要调用的委托以确定是否应抓取页面的同步方法
        /// </summary>
        public void ShouldCrawlPage(Func<PageToCrawl, CrawlContext, CrawlDecision> decisionMaker)
        {
            _shouldCrawlPageDecisionMaker = decisionMaker;
        }

        /// <summary>
        /// 同步方法，注册要调用的委托以确定页面的内容是否应该被下载
        /// </summary>
        /// <param name="decisionMaker"></param>
        public void ShouldDownloadPageContent(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker)
        {
            _shouldDownloadPageContentDecisionMaker = decisionMaker;
        }

        /// <summary>
        /// 注册要调用的委托以确定是否应抓取页面的链接的同步方法
        /// </summary>
        /// <param name="decisionMaker"></param>
        public void ShouldCrawlPageLinks(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker)
        {
            _shouldCrawlPageLinksDecisionMaker = decisionMaker;
        }

        /// <summary>
        /// 注册要调用的委托的同步方法，以确定页面上的某个链接是否应该被调度为被爬网
        /// </summary>
        public void ShouldScheduleLink(Func<Uri, CrawledPage, CrawlContext, bool> decisionMaker)
        {
            _shouldScheduleLinkDecisionMaker = decisionMaker;
        }

        /// <summary>
        /// 注册要调用的委托以确定是否应重新抓取页面的同步方法
        /// </summary>
        public void ShouldRecrawlPage(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker)
        {
            _shouldRecrawlPageDecisionMaker = decisionMaker;
        }

        /// <summary>
        /// 同步方法，注册要调用的委托以确定第一个uri参数是否被认为是第二个uri参数的内部uri
        /// </summary>
        /// <param name="decisionMaker delegate"></param>     
        public void IsInternalUri(Func<Uri, Uri, bool> decisionMaker)
        {
            _isInternalDecisionMaker = decisionMaker;
        }

        private CrawlConfiguration GetCrawlConfigurationFromConfigFile()
        {
            AbotConfigurationSectionHandler configFromFile = AbotConfigurationSectionHandler.LoadFromXml();

            if (configFromFile == null)
                throw new InvalidOperationException("abot config section was NOT found");

            _logger.DebugFormat("abot config section was found");
            return configFromFile.Convert();
        }
        /// <summary>
        /// 抓取网站
        /// </summary>
        protected virtual void CrawlSite()
        {
            while (!_crawlComplete)
            {
                RunPreWorkChecks();

                if (_scheduler.Count > 0)
                {
                    _threadManager.DoWork(() => ProcessPage(_scheduler.GetNext()));
                }
                else if (!_threadManager.HasRunningThreads())
                {
                    _crawlComplete = true;
                }
                else
                {
                    _logger.DebugFormat("Waiting for links to be scheduled...");
                    Thread.Sleep(2500);
                }
            }
        }
        /// <summary>
        ///验证必需的可用内存 
        /// </summary>
        protected virtual void VerifyRequiredAvailableMemory()
        {
            if (_crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb < 1)
                return;

            if (!_memoryManager.IsSpaceAvailable(_crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb))
                throw new InsufficientMemoryException(string.Format("Process does not have the configured [{0}mb] of available memory to crawl site [{1}]. This is configurable through the minAvailableMemoryRequiredInMb in app.conf or CrawlConfiguration.MinAvailableMemoryRequiredInMb.", _crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb, _crawlContext.RootUri));
        }
        /// <summary>
        /// 运行前工作检查
        /// </summary>
        protected virtual void RunPreWorkChecks()
        {
            CheckMemoryUsage();
            CheckForCancellationRequest();
            CheckForHardStopRequest();
            CheckForStopRequest();
        }
        /// <summary>
        /// 检查内存使用情况
        /// </summary>
        protected virtual void CheckMemoryUsage()
        {
            if (_memoryManager == null
                || _crawlContext.IsCrawlHardStopRequested
                || _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb < 1)
                return;

            int currentMemoryUsage = _memoryManager.GetCurrentUsageInMb();
            if (_logger.IsDebugEnabled)
                _logger.DebugFormat("Current memory usage for site [{0}] is [{1}mb]", _crawlContext.RootUri, currentMemoryUsage);

            if (currentMemoryUsage > _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb)
            {
                _memoryManager.Dispose();
                _memoryManager = null;

                string message = string.Format("Process is using [{0}mb] of memory which is above the max configured of [{1}mb] for site [{2}]. This is configurable through the maxMemoryUsageInMb in app.conf or CrawlConfiguration.MaxMemoryUsageInMb.", currentMemoryUsage, _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb, _crawlContext.RootUri);
                _crawlResult.ErrorException = new InsufficientMemoryException(message);

                _logger.Fatal(_crawlResult.ErrorException);
                _crawlContext.IsCrawlHardStopRequested = true;
            }
        }
        /// <summary>
        /// 检查取消请求
        /// </summary>
        protected virtual void CheckForCancellationRequest()
        {
            if (_crawlContext.CancellationTokenSource.IsCancellationRequested)
            {
                if (!_crawlCancellationReported)
                {
                    string message = string.Format("Crawl cancellation requested for site [{0}]!", _crawlContext.RootUri);
                    _logger.Fatal(message);
                    _crawlResult.ErrorException = new OperationCanceledException(message, _crawlContext.CancellationTokenSource.Token);
                    _crawlContext.IsCrawlHardStopRequested = true;
                    _crawlCancellationReported = true;
                }
            }
        }
        /// <summary>
        ///检查硬停止请求 
        /// </summary>
        protected virtual void CheckForHardStopRequest()
        {
            if (_crawlContext.IsCrawlHardStopRequested)
            {
                if (!_crawlStopReported)
                {
                    _logger.InfoFormat("Hard crawl stop requested for site [{0}]!", _crawlContext.RootUri);
                    _crawlStopReported = true;
                }

                _scheduler.Clear();
                _threadManager.AbortAll();
                _scheduler.Clear();//to be sure nothing was scheduled since first call to clear()

                //将所有事件设置为null，以便不会再触发任何事件
                PageCrawlStarting = null;
                PageCrawlCompleted = null;
                PageCrawlDisallowed = null;
                PageLinksCrawlDisallowed = null;
                PageCrawlStartingAsync = null;
                PageCrawlCompletedAsync = null;
                PageCrawlDisallowedAsync = null;
                PageLinksCrawlDisallowedAsync = null;
            }
        }
        /// <summary>
        /// 检查停止请求
        /// </summary>
        protected virtual void CheckForStopRequest()
        {
            if (_crawlContext.IsCrawlStopRequested)
            {
                if (!_crawlStopReported)
                {
                    _logger.InfoFormat("Crawl stop requested for site [{0}]!", _crawlContext.RootUri);
                    _crawlStopReported = true;
                }
                _scheduler.Clear();
            }
        }
        /// <summary>
        /// 处理抓取超时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void HandleCrawlTimeout(object sender, ElapsedEventArgs e)
        {
            Timer elapsedTimer = sender as Timer;
            if (elapsedTimer != null)
                elapsedTimer.Stop();

            _logger.InfoFormat("Crawl timeout of [{0}] seconds has been reached for [{1}]", _crawlContext.CrawlConfiguration.CrawlTimeoutSeconds, _crawlContext.RootUri);
            _crawlContext.IsCrawlHardStopRequested = true;
        }
        /// <summary>
        /// 异步任务处理页面
        /// </summary>
        /// <param name="pageToCrawl"></param>
        protected virtual void ProcessPage(PageToCrawl pageToCrawl)
        {
            try
            {
                if (pageToCrawl == null)
                    return;

                ThrowIfCancellationRequested();

                AddPageToContext(pageToCrawl);

                //CrawledPage crawledPage = await CrawlThePage(pageToCrawl);
                CrawledPage crawledPage = CrawlThePage(pageToCrawl);

                // Validate the root uri in case of a redirection.
                if (crawledPage.IsRoot)
                    ValidateRootUriForRedirection(crawledPage);

                if (IsRedirect(crawledPage) && !_crawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled)
                    ProcessRedirect(crawledPage);

                if (PageSizeIsAboveMax(crawledPage))
                    return;

                ThrowIfCancellationRequested();

                bool shouldCrawlPageLinks = ShouldCrawlPageLinks(crawledPage);
                if (shouldCrawlPageLinks || _crawlContext.CrawlConfiguration.IsForcedLinkParsingEnabled)
                    ParsePageLinks(crawledPage);

                ThrowIfCancellationRequested();

                if (shouldCrawlPageLinks)
                    SchedulePageLinks(crawledPage);

                ThrowIfCancellationRequested();

                FirePageCrawlCompletedEventAsync(crawledPage);
                FirePageCrawlCompletedEvent(crawledPage);

                if (ShouldRecrawlPage(crawledPage))
                {
                    crawledPage.IsRetry = true;
                    _scheduler.Add(crawledPage);
                }
            }
            catch (OperationCanceledException oce)
            {
                _logger.DebugFormat("Thread cancelled while crawling/processing page [{0}]", pageToCrawl.Uri);
                throw;
            }
            catch (Exception e)
            {
                _crawlResult.ErrorException = e;
                _logger.FatalFormat("Error occurred during processing of page [{0}]", pageToCrawl.Uri);
                _logger.Fatal(e);

                _crawlContext.IsCrawlHardStopRequested = true;
            }
        }
        /// <summary>
        /// 处理重定向
        /// </summary>
        /// <param name="crawledPage"></param>
        protected virtual void ProcessRedirect(CrawledPage crawledPage)
        {
            if (crawledPage.RedirectPosition >= 20)
                _logger.WarnFormat("Page [{0}] is part of a chain of 20 or more consecutive redirects, redirects for this chain will now be aborted.", crawledPage.Uri);

            try
            {
                var uri = ExtractRedirectUri(crawledPage);

                PageToCrawl page = new PageToCrawl(uri);
                page.ParentUri = crawledPage.ParentUri;
                page.CrawlDepth = crawledPage.CrawlDepth;
                page.IsInternal = IsInternalUri(uri);
                page.IsRoot = false;
                page.RedirectedFrom = crawledPage;
                page.RedirectPosition = crawledPage.RedirectPosition + 1;

                crawledPage.RedirectedTo = page;
                _logger.DebugFormat("Page [{0}] is requesting that it be redirect to [{1}]", crawledPage.Uri, crawledPage.RedirectedTo.Uri);

                if (ShouldSchedulePageLink(page))
                {
                    _logger.InfoFormat("Page [{0}] will be redirect to [{1}]", crawledPage.Uri, crawledPage.RedirectedTo.Uri);
                    _scheduler.Add(page);
                }
            }
            catch { }
        }

        /// <summary>
        /// 是否内部Uri
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected virtual bool IsInternalUri(Uri uri)
        {
            return _isInternalDecisionMaker(uri, _crawlContext.RootUri) ||
                _isInternalDecisionMaker(uri, _crawlContext.OriginalRootUri);
        }

        /// <summary>
        /// 是否重定向
        /// </summary>
        /// <param name="crawledPage"></param>
        /// <returns></returns>
        protected virtual bool IsRedirect(CrawledPage crawledPage)
        {
            bool isRedirect = false;
            if (crawledPage.HttpWebResponse != null)
            {
                isRedirect = (_crawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled &&
                    crawledPage.HttpWebResponse.ResponseUri != null &&
                    crawledPage.HttpWebResponse.ResponseUri.AbsoluteUri != crawledPage.Uri.AbsoluteUri) ||
                    (!_crawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled &&
                    (int)crawledPage.HttpWebResponse.StatusCode >= 300 &&
                    (int)crawledPage.HttpWebResponse.StatusCode <= 399);
            }
            return isRedirect;
        }
        /// <summary>
        /// 
        /// </summary>
        protected virtual void ThrowIfCancellationRequested()
        {
            if (_crawlContext.CancellationTokenSource != null && _crawlContext.CancellationTokenSource.IsCancellationRequested)
                _crawlContext.CancellationTokenSource.Token.ThrowIfCancellationRequested();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="crawledPage"></param>
        /// <returns></returns>
        protected virtual bool PageSizeIsAboveMax(CrawledPage crawledPage)
        {
            bool isAboveMax = false;
            if (_crawlContext.CrawlConfiguration.MaxPageSizeInBytes > 0 &&
                crawledPage.Content.Bytes != null &&
                crawledPage.Content.Bytes.Length > _crawlContext.CrawlConfiguration.MaxPageSizeInBytes)
            {
                isAboveMax = true;
                _logger.InfoFormat("Page [{0}] has a page size of [{1}] bytes which is above the [{2}] byte max, no further processing will occur for this page", crawledPage.Uri, crawledPage.Content.Bytes.Length, _crawlContext.CrawlConfiguration.MaxPageSizeInBytes);
            }
            return isAboveMax;
        }
        /// <summary>
        /// 抓取页面链接
        /// </summary>
        /// <param name="crawledPage"></param>
        /// <returns></returns>
        protected virtual bool ShouldCrawlPageLinks(CrawledPage crawledPage)
        {
            CrawlDecision shouldCrawlPageLinksDecision = _crawlDecisionMaker.ShouldCrawlPageLinks(crawledPage, _crawlContext);
            if (shouldCrawlPageLinksDecision.Allow)
                shouldCrawlPageLinksDecision = (_shouldCrawlPageLinksDecisionMaker != null) ? _shouldCrawlPageLinksDecisionMaker.Invoke(crawledPage, _crawlContext) : new CrawlDecision { Allow = true };

            if (!shouldCrawlPageLinksDecision.Allow)
            {
                _logger.DebugFormat("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldCrawlPageLinksDecision.Reason);
                FirePageLinksCrawlDisallowedEventAsync(crawledPage, shouldCrawlPageLinksDecision.Reason);
                FirePageLinksCrawlDisallowedEvent(crawledPage, shouldCrawlPageLinksDecision.Reason);
            }

            SignalCrawlStopIfNeeded(shouldCrawlPageLinksDecision);
            return shouldCrawlPageLinksDecision.Allow;
        }
        /// <summary>
        /// 抓取页面
        /// </summary>
        /// <param name="pageToCrawl"></param>
        /// <returns></returns>
        protected virtual bool ShouldCrawlPage(PageToCrawl pageToCrawl)
        {
            if (_maxPagesToCrawlLimitReachedOrScheduled)
                return false;

            CrawlDecision shouldCrawlPageDecision = _crawlDecisionMaker.ShouldCrawlPage(pageToCrawl, _crawlContext);
            if (!shouldCrawlPageDecision.Allow &&
                shouldCrawlPageDecision.Reason.Contains("MaxPagesToCrawl limit of"))
            {
                _maxPagesToCrawlLimitReachedOrScheduled = true;
                _logger.Info("MaxPagesToCrawlLimit has been reached or scheduled. No more pages will be scheduled.");
                return false;
            }

            if (shouldCrawlPageDecision.Allow)
                shouldCrawlPageDecision = (_shouldCrawlPageDecisionMaker != null) ? _shouldCrawlPageDecisionMaker.Invoke(pageToCrawl, _crawlContext) : new CrawlDecision { Allow = true };

            if (!shouldCrawlPageDecision.Allow)
            {
                _logger.DebugFormat("Page [{0}] not crawled, [{1}]", pageToCrawl.Uri.AbsoluteUri, shouldCrawlPageDecision.Reason);
                FirePageCrawlDisallowedEventAsync(pageToCrawl, shouldCrawlPageDecision.Reason);
                FirePageCrawlDisallowedEvent(pageToCrawl, shouldCrawlPageDecision.Reason);
            }

            SignalCrawlStopIfNeeded(shouldCrawlPageDecision);
            return shouldCrawlPageDecision.Allow;
        }
        /// <summary>
        /// 重新抓取页面
        /// </summary>
        /// <param name="crawledPage"></param>
        /// <returns></returns>
        protected virtual bool ShouldRecrawlPage(CrawledPage crawledPage)
        {
            //TODO No unit tests cover these lines
            CrawlDecision shouldRecrawlPageDecision = _crawlDecisionMaker.ShouldRecrawlPage(crawledPage, _crawlContext);
            if (shouldRecrawlPageDecision.Allow)
                shouldRecrawlPageDecision = (_shouldRecrawlPageDecisionMaker != null) ? _shouldRecrawlPageDecisionMaker.Invoke(crawledPage, _crawlContext) : new CrawlDecision { Allow = true };

            if (!shouldRecrawlPageDecision.Allow)
            {
                _logger.DebugFormat("Page [{0}] not recrawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldRecrawlPageDecision.Reason);
            }
            else
            {
                // Look for the Retry-After header in the response.
                crawledPage.RetryAfter = null;
                if (crawledPage.HttpWebResponse != null &&
                    crawledPage.HttpWebResponse.Headers != null)
                {
                    string value = crawledPage.HttpWebResponse.GetResponseHeader("Retry-After");
                    if (!String.IsNullOrEmpty(value))
                    {
                        // Try to convert to DateTime first, then in double.
                        DateTime date;
                        double seconds;
                        if (crawledPage.LastRequest.HasValue && DateTime.TryParse(value, out date))
                        {
                            crawledPage.RetryAfter = (date - crawledPage.LastRequest.Value).TotalSeconds;
                        }
                        else if (double.TryParse(value, out seconds))
                        {
                            crawledPage.RetryAfter = seconds;
                        }
                    }
                }
            }

            SignalCrawlStopIfNeeded(shouldRecrawlPageDecision);
            return shouldRecrawlPageDecision.Allow;
        }

        /// <summary>
        /// 异步任务<CrawledPage> CrawlThePage（PageToCrawl pageToCrawl）
        /// </summary>
        /// <param name="pageToCrawl"></param>
        /// <returns></returns>
        protected virtual CrawledPage CrawlThePage(PageToCrawl pageToCrawl)
        {
            _logger.DebugFormat("About to crawl page [{0}]", pageToCrawl.Uri.AbsoluteUri);
            FirePageCrawlStartingEventAsync(pageToCrawl);
            FirePageCrawlStartingEvent(pageToCrawl);

            if (pageToCrawl.IsRetry) { WaitMinimumRetryDelay(pageToCrawl); }

            pageToCrawl.LastRequest = DateTime.Now;

            CrawledPage crawledPage = _pageRequester.MakeRequest(pageToCrawl.Uri, ShouldDownloadPageContent);
            //CrawledPage crawledPage = await _pageRequester.MakeRequestAsync(pageToCrawl.Uri, ShouldDownloadPageContent);

            Map(pageToCrawl, crawledPage);

            if (crawledPage.HttpWebResponse == null)
                _logger.InfoFormat("Page crawl complete, Status:[NA] Url:[{0}] Elapsed:[{1}] Parent:[{2}] Retry:[{3}]", crawledPage.Uri.AbsoluteUri, crawledPage.Elapsed, crawledPage.ParentUri, crawledPage.RetryCount);
            else
                _logger.InfoFormat("Page crawl complete, Status:[{0}] Url:[{1}] Elapsed:[{2}] Parent:[{3}] Retry:[{4}]", Convert.ToInt32(crawledPage.HttpWebResponse.StatusCode), crawledPage.Uri.AbsoluteUri, crawledPage.Elapsed, crawledPage.ParentUri, crawledPage.RetryCount);

            return crawledPage;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        protected void Map(PageToCrawl src, CrawledPage dest)
        {
            dest.Uri = src.Uri;
            dest.ParentUri = src.ParentUri;
            dest.IsRetry = src.IsRetry;
            dest.RetryAfter = src.RetryAfter;
            dest.RetryCount = src.RetryCount;
            dest.LastRequest = src.LastRequest;
            dest.IsRoot = src.IsRoot;
            dest.IsInternal = src.IsInternal;
            dest.PageBag = CombinePageBags(src.PageBag, dest.PageBag);
            dest.CrawlDepth = src.CrawlDepth;
            dest.RedirectedFrom = src.RedirectedFrom;
            dest.RedirectPosition = src.RedirectPosition;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageToCrawlBag"></param>
        /// <param name="crawledPageBag"></param>
        /// <returns></returns>
        protected virtual dynamic CombinePageBags(dynamic pageToCrawlBag, dynamic crawledPageBag)
        {
            IDictionary<string, object> combinedBag = new ExpandoObject();
            var pageToCrawlBagDict = pageToCrawlBag as IDictionary<string, object>;
            var crawledPageBagDict = crawledPageBag as IDictionary<string, object>;

            foreach (KeyValuePair<string, object> entry in pageToCrawlBagDict) combinedBag[entry.Key] = entry.Value;
            foreach (KeyValuePair<string, object> entry in crawledPageBagDict) combinedBag[entry.Key] = entry.Value;

            return combinedBag;
        }
        /// <summary>
        /// 添加页面到上下文
        /// </summary>
        /// <param name="pageToCrawl"></param>
        protected virtual void AddPageToContext(PageToCrawl pageToCrawl)
        {
            if (pageToCrawl.IsRetry)
            {
                pageToCrawl.RetryCount++;
                return;
            }

            int domainCount = 0;
            Interlocked.Increment(ref _crawlContext.CrawledCount);
            _crawlContext.CrawlCountByDomain.AddOrUpdate(pageToCrawl.Uri.Authority, 1, (key, oldValue) => oldValue + 1);
        }
        /// <summary>
        /// 解析页面链接
        /// </summary>
        /// <param name="crawledPage"></param>
        protected virtual void ParsePageLinks(CrawledPage crawledPage)
        {
            crawledPage.ParsedLinks = _hyperLinkParser.GetLinks(crawledPage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="crawledPage"></param>
        protected virtual void SchedulePageLinks(CrawledPage crawledPage)
        {
            int linksToCrawl = 0;
            foreach (Uri uri in crawledPage.ParsedLinks)
            {
                // First validate that the link was not already visited or added to the list of pages to visit, so we don't
                // make the same validation and fire the same events twice.
                if (!_scheduler.IsUriKnown(uri) &&
                    (_shouldScheduleLinkDecisionMaker == null || _shouldScheduleLinkDecisionMaker.Invoke(uri, crawledPage, _crawlContext)))
                {
                    try //Added due to a bug in the Uri class related to this (http://stackoverflow.com/questions/2814951/system-uriformatexception-invalid-uri-the-hostname-could-not-be-parsed)
                    {
                        PageToCrawl page = new PageToCrawl(uri);
                        page.ParentUri = crawledPage.Uri;
                        page.CrawlDepth = crawledPage.CrawlDepth + 1;
                        page.IsInternal = IsInternalUri(uri);
                        page.IsRoot = false;

                        if (ShouldSchedulePageLink(page))
                        {
                            _scheduler.Add(page);
                            linksToCrawl++;
                        }

                        if (!ShouldScheduleMorePageLink(linksToCrawl))
                        {
                            _logger.InfoFormat("MaxLinksPerPage has been reached. No more links will be scheduled for current page [{0}].", crawledPage.Uri);
                            break;
                        }
                    }
                    catch { }
                }

                // Add this link to the list of known Urls so validations are not duplicated in the future.
                _scheduler.AddKnownUri(uri);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        protected virtual bool ShouldSchedulePageLink(PageToCrawl page)
        {
            if ((page.IsInternal || _crawlContext.CrawlConfiguration.IsExternalPageCrawlingEnabled) && (ShouldCrawlPage(page)))
                return true;

            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="linksAdded"></param>
        /// <returns></returns>
        protected virtual bool ShouldScheduleMorePageLink(int linksAdded)
        {
            return _crawlContext.CrawlConfiguration.MaxLinksPerPage == 0 || _crawlContext.CrawlConfiguration.MaxLinksPerPage > linksAdded;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="crawledPage"></param>
        /// <returns></returns>
        protected virtual CrawlDecision ShouldDownloadPageContent(CrawledPage crawledPage)
        {
            CrawlDecision decision = _crawlDecisionMaker.ShouldDownloadPageContent(crawledPage, _crawlContext);
            if (decision.Allow)
                decision = (_shouldDownloadPageContentDecisionMaker != null) ? _shouldDownloadPageContentDecisionMaker.Invoke(crawledPage, _crawlContext) : new CrawlDecision { Allow = true };

            SignalCrawlStopIfNeeded(decision);
            return decision;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        protected virtual void PrintConfigValues(CrawlConfiguration config)
        {
            _logger.Info("Configuration Values:");

            string indentString = new string(' ', 2);
            string abotVersion = Assembly.GetAssembly(this.GetType()).GetName().Version.ToString();
            _logger.InfoFormat("{0}Abot Version: {1}", indentString, abotVersion);
            foreach (PropertyInfo property in config.GetType().GetProperties())
            {
                if (property.Name != "ConfigurationExtensions")
                    _logger.InfoFormat("{0}{1}: {2}", indentString, property.Name, property.GetValue(config, null));
            }

            foreach (string key in config.ConfigurationExtensions.Keys)
            {
                _logger.InfoFormat("{0}{1}: {2}", indentString, key, config.ConfigurationExtensions[key]);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="decision"></param>
        protected virtual void SignalCrawlStopIfNeeded(CrawlDecision decision)
        {
            if (decision.ShouldHardStopCrawl)
            {
                _logger.InfoFormat("Decision marked crawl [Hard Stop] for site [{0}], [{1}]", _crawlContext.RootUri, decision.Reason);
                _crawlContext.IsCrawlHardStopRequested = decision.ShouldHardStopCrawl;
            }
            else if (decision.ShouldStopCrawl)
            {
                _logger.InfoFormat("Decision marked crawl [Stop] for site [{0}], [{1}]", _crawlContext.RootUri, decision.Reason);
                _crawlContext.IsCrawlStopRequested = decision.ShouldStopCrawl;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageToCrawl"></param>
        protected virtual void WaitMinimumRetryDelay(PageToCrawl pageToCrawl)
        {
            //TODO No unit tests cover these lines
            if (pageToCrawl.LastRequest == null)
            {
                _logger.WarnFormat("pageToCrawl.LastRequest value is null for Url:{0}. Cannot retry without this value.", pageToCrawl.Uri.AbsoluteUri);
                return;
            }

            double milliSinceLastRequest = (DateTime.Now - pageToCrawl.LastRequest.Value).TotalMilliseconds;
            double milliToWait;
            if (pageToCrawl.RetryAfter.HasValue)
            {
                // Use the time to wait provided by the server instead of the config, if any.
                milliToWait = pageToCrawl.RetryAfter.Value * 1000 - milliSinceLastRequest;
            }
            else
            {
                if (!(milliSinceLastRequest < _crawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds)) return;
                milliToWait = _crawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds - milliSinceLastRequest;
            }

            _logger.InfoFormat("Waiting [{0}] milliseconds before retrying Url:[{1}] LastRequest:[{2}] SoonestNextRequest:[{3}]",
                milliToWait,
                pageToCrawl.Uri.AbsoluteUri,
                pageToCrawl.LastRequest,
                pageToCrawl.LastRequest.Value.AddMilliseconds(_crawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds));

            //TODO Cannot use RateLimiter since it currently cannot handle dynamic sleep times so using Thread.Sleep in the meantime
            if (milliToWait > 0)
                Thread.Sleep(TimeSpan.FromMilliseconds(milliToWait));
        }

        /// <summary>
        /// Validate that the Root page was not redirected. If the root page is redirected, we assume that the root uri
        /// should be changed to the uri where it was redirected.
        /// </summary>
        protected virtual void ValidateRootUriForRedirection(CrawledPage crawledRootPage)
        {
            if (!crawledRootPage.IsRoot)
            {
                throw new ArgumentException("The crawled page must be the root page to be validated for redirection.");
            }

            if (IsRedirect(crawledRootPage))
            {
                _crawlContext.RootUri = ExtractRedirectUri(crawledRootPage);
                _logger.InfoFormat("The root URI [{0}] was redirected to [{1}]. [{1}] is the new root.",
                    _crawlContext.OriginalRootUri,
                    _crawlContext.RootUri);
            }
        }

        /// <summary>
        /// Retrieve the URI where the specified crawled page was redirected.
        /// </summary>
        /// <remarks>
        /// If HTTP auto redirections is disabled, this value is stored in the 'Location' header of the response.
        /// If auto redirections is enabled, this value is stored in the response's ResponseUri property.
        /// </remarks>
        protected virtual Uri ExtractRedirectUri(CrawledPage crawledPage)
        {
            Uri locationUri;
            if (_crawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled)
            {
                // For auto redirects, look for the response uri.
                locationUri = crawledPage.HttpWebResponse.ResponseUri;
            }
            else
            {
                // For manual redirects, we need to look for the location header.
                var location = crawledPage.HttpWebResponse.Headers["Location"];

                // Check if the location is absolute. If not, create an absolute uri.
                if (!Uri.TryCreate(location, UriKind.Absolute, out locationUri))
                {
                    Uri baseUri = new Uri(crawledPage.Uri.GetLeftPart(UriPartial.Authority));
                    locationUri = new Uri(baseUri, location);
                }
            }
            return locationUri;
        }
        /// <summary>
        /// 回收
        /// </summary>
        public virtual void Dispose()
        {
            if (_threadManager != null)
            {
                _threadManager.Dispose();
            }
            if (_scheduler != null)
            {
                _scheduler.Dispose();
            }
            if (_pageRequester != null)
            {
                _pageRequester.Dispose();
            }
            if (_memoryManager != null)
            {
                _memoryManager.Dispose();
            }
        }
    }
}