using Abot.Poco;
using System;
using System.Configuration;
using System.Runtime.Remoting.Channels;

namespace Abot.Core
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class AbotConfigurationSectionHandler : ConfigurationSection
    {
        /// <summary>
        /// 
        /// </summary>
        public AbotConfigurationSectionHandler()
        {
            
        }
        /// <summary>
        /// 
        /// </summary>

        [ConfigurationProperty("crawlBehavior")]
        public CrawlBehaviorElement CrawlBehavior
        {
            get { return (CrawlBehaviorElement)this["crawlBehavior"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("politeness")]
        public PolitenessElement Politeness
        {
            get { return (PolitenessElement)this["politeness"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("authorization")]
        public AuthorizationElement Authorization
        {
            get { return (AuthorizationElement)this["authorization"]; }
        }
        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("extensionValues")]
        [ConfigurationCollection(typeof(ExtensionValueCollection), AddItemName = "add")]
        public ExtensionValueCollection ExtensionValues
        {
            get { return (ExtensionValueCollection)this["extensionValues"]; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public CrawlConfiguration Convert()
        {
            CrawlConfiguration config = new CrawlConfiguration();
            Map(CrawlBehavior, config);
            Map(Politeness, config);
            Map(Authorization, config);

            foreach (ExtensionValueElement element in ExtensionValues)
                config.ConfigurationExtensions.Add(element.Key, element.Value);

            return config;
        }

        private void Map(CrawlBehaviorElement src, CrawlConfiguration dest)
        {
            dest.MaxConcurrentThreads = src.MaxConcurrentThreads;
            dest.MaxPagesToCrawl = src.MaxPagesToCrawl;
            dest.MaxPagesToCrawlPerDomain = src.MaxPagesToCrawlPerDomain;
            dest.MaxPageSizeInBytes = src.MaxPageSizeInBytes;
            dest.UserAgentString = src.UserAgentString;
            dest.CrawlTimeoutSeconds = src.CrawlTimeoutSeconds;
            dest.IsUriRecrawlingEnabled = src.IsUriRecrawlingEnabled;
            dest.IsExternalPageCrawlingEnabled = src.IsExternalPageCrawlingEnabled;
            dest.IsExternalPageLinksCrawlingEnabled = src.IsExternalPageLinksCrawlingEnabled;
            dest.IsRespectUrlNamedAnchorOrHashbangEnabled = src.IsRespectUrlNamedAnchorOrHashbangEnabled;
            dest.DownloadableContentTypes = src.DownloadableContentTypes;
            dest.HttpServicePointConnectionLimit = src.HttpServicePointConnectionLimit;
            dest.HttpRequestTimeoutInSeconds = src.HttpRequestTimeoutInSeconds;
            dest.HttpRequestMaxAutoRedirects = src.HttpRequestMaxAutoRedirects;
            dest.IsHttpRequestAutoRedirectsEnabled = src.IsHttpRequestAutoRedirectsEnabled;
            dest.IsHttpRequestAutomaticDecompressionEnabled = src.IsHttpRequestAutomaticDecompressionEnabled;
            dest.IsSendingCookiesEnabled = src.IsSendingCookiesEnabled;
            dest.IsSslCertificateValidationEnabled = src.IsSslCertificateValidationEnabled;
            dest.MinAvailableMemoryRequiredInMb = src.MinAvailableMemoryRequiredInMb;
            dest.MaxMemoryUsageInMb = src.MaxMemoryUsageInMb;
            dest.MaxMemoryUsageCacheTimeInSeconds = src.MaxMemoryUsageCacheTimeInSeconds;
            dest.MaxCrawlDepth = src.MaxCrawlDepth;
            dest.MaxLinksPerPage = src.MaxLinksPerPage;
            dest.IsForcedLinkParsingEnabled = src.IsForcedLinkParsingEnabled;
            dest.MaxRetryCount = src.MaxRetryCount;
            dest.MinRetryDelayInMilliseconds = src.MinRetryDelayInMilliseconds;
        }

        private void Map(PolitenessElement src, CrawlConfiguration dest)
        {
            dest.IsRespectRobotsDotTextEnabled = src.IsRespectRobotsDotTextEnabled;
            dest.IsRespectMetaRobotsNoFollowEnabled = src.IsRespectMetaRobotsNoFollowEnabled;
            dest.IsRespectHttpXRobotsTagHeaderNoFollowEnabled = src.IsRespectHttpXRobotsTagHeaderNoFollowEnabled;
            dest.IsRespectAnchorRelNoFollowEnabled = src.IsRespectAnchorRelNoFollowEnabled;
            dest.IsIgnoreRobotsDotTextIfRootDisallowedEnabled = src.IsIgnoreRobotsDotTextIfRootDisallowedEnabled;
            dest.RobotsDotTextUserAgentString = src.RobotsDotTextUserAgentString;
            dest.MinCrawlDelayPerDomainMilliSeconds = src.MinCrawlDelayPerDomainMilliSeconds;
            dest.MaxRobotsDotTextCrawlDelayInSeconds = src.MaxRobotsDotTextCrawlDelayInSeconds;
        }

        private void Map(AuthorizationElement src, CrawlConfiguration dest)
        {
            dest.IsAlwaysLogin = src.IsAlwaysLogin;
            dest.LoginUser = src.LoginUser;
            dest.LoginPassword = src.LoginPassword;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static AbotConfigurationSectionHandler LoadFromXml()
        {
            return ((AbotConfigurationSectionHandler)System.Configuration.ConfigurationManager.GetSection("abot"));
        }
    }
    /// <summary>
    /// 
    /// </summary>

    [Serializable]
    public class AuthorizationElement : ConfigurationElement
    {
        /// <summary>
        /// Defines whatewer each request shold be autorized via login 
        /// </summary>
        [ConfigurationProperty("isAlwaysLogin", IsRequired = false)]
        public bool IsAlwaysLogin
        {
            get { return (bool)this["isAlwaysLogin"]; }
        }

        /// <summary>
        /// The user name to be used for autorization 
        /// </summary>
        [ConfigurationProperty("loginUser", IsRequired = false)]
        public string LoginUser
        {
            get { return (string)this["loginUser"]; }
        }
        /// <summary>
        /// The password to be used for autorization 
        /// </summary>
        [ConfigurationProperty("loginPassword", IsRequired = false)]
        public string LoginPassword
        {
            get { return (string)this["loginPassword"]; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class PolitenessElement : ConfigurationElement
    {
        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("isRespectRobotsDotTextEnabled", IsRequired = false)]
        public bool IsRespectRobotsDotTextEnabled
        {
            get { return (bool)this["isRespectRobotsDotTextEnabled"]; }
        }
        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("isRespectMetaRobotsNoFollowEnabled", IsRequired = false)]
        public bool IsRespectMetaRobotsNoFollowEnabled
        {
            get { return (bool)this["isRespectMetaRobotsNoFollowEnabled"]; }
        }
        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("isRespectHttpXRobotsTagHeaderNoFollowEnabled", IsRequired = false)]
        public bool IsRespectHttpXRobotsTagHeaderNoFollowEnabled
        {
            get { return (bool)this["isRespectHttpXRobotsTagHeaderNoFollowEnabled"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("isRespectAnchorRelNoFollowEnabled", IsRequired = false)]
        public bool IsRespectAnchorRelNoFollowEnabled
        {
            get { return (bool)this["isRespectAnchorRelNoFollowEnabled"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("isIgnoreRobotsDotTextIfRootDisallowedEnabled", IsRequired = false)]
        public bool IsIgnoreRobotsDotTextIfRootDisallowedEnabled
        {
            get { return (bool)this["isIgnoreRobotsDotTextIfRootDisallowedEnabled"]; }
        }
        /// <summary>
        /// 
        /// </summary>

        [ConfigurationProperty("robotsDotTextUserAgentString", IsRequired = false, DefaultValue = "abot")]
        public string RobotsDotTextUserAgentString
        {
            get { return (string)this["robotsDotTextUserAgentString"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("maxRobotsDotTextCrawlDelayInSeconds", IsRequired = false, DefaultValue = 5)]
        public int MaxRobotsDotTextCrawlDelayInSeconds
        {
            get { return (int)this["maxRobotsDotTextCrawlDelayInSeconds"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("minCrawlDelayPerDomainMilliSeconds", IsRequired = false)]
        public int MinCrawlDelayPerDomainMilliSeconds
        {
            get { return (int)this["minCrawlDelayPerDomainMilliSeconds"]; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class CrawlBehaviorElement : ConfigurationElement
    {
        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("maxConcurrentThreads", IsRequired = false, DefaultValue = 10)]
        public int MaxConcurrentThreads
        {
            get { return (int)this["maxConcurrentThreads"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("maxPagesToCrawl", IsRequired = false, DefaultValue = 1000)]
        public int MaxPagesToCrawl
        {
            get { return (int)this["maxPagesToCrawl"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("maxPagesToCrawlPerDomain", IsRequired = false)]
        public int MaxPagesToCrawlPerDomain
        {
            get { return (int)this["maxPagesToCrawlPerDomain"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("maxPageSizeInBytes", IsRequired = false)]
        public int MaxPageSizeInBytes
        {
            get { return (int)this["maxPageSizeInBytes"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("userAgentString", IsRequired = false, DefaultValue = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko")]
        public string UserAgentString
        {
            get { return (string)this["userAgentString"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("crawlTimeoutSeconds", IsRequired = false)]
        public int CrawlTimeoutSeconds
        {
            get { return (int)this["crawlTimeoutSeconds"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("downloadableContentTypes", IsRequired = false, DefaultValue = "text/html")]
        public string DownloadableContentTypes
        {
            get { return (string)this["downloadableContentTypes"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("isUriRecrawlingEnabled", IsRequired = false)]
        public bool IsUriRecrawlingEnabled
        {
            get { return (bool)this["isUriRecrawlingEnabled"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("isExternalPageCrawlingEnabled", IsRequired = false)]
        public bool IsExternalPageCrawlingEnabled
        {
            get { return (bool)this["isExternalPageCrawlingEnabled"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("isExternalPageLinksCrawlingEnabled", IsRequired = false)]
        public bool IsExternalPageLinksCrawlingEnabled
        {
            get { return (bool)this["isExternalPageLinksCrawlingEnabled"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("isSslCertificateValidationEnabled", IsRequired = false, DefaultValue = true)]
        public bool IsSslCertificateValidationEnabled
        {
            get { return (bool)this["isSslCertificateValidationEnabled"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("httpServicePointConnectionLimit", IsRequired = false, DefaultValue = 200)]
        public int HttpServicePointConnectionLimit
        {
            get { return (int)this["httpServicePointConnectionLimit"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("httpRequestTimeoutInSeconds", IsRequired = false, DefaultValue = 15)]
        public int HttpRequestTimeoutInSeconds
        {
            get { return (int)this["httpRequestTimeoutInSeconds"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("httpRequestMaxAutoRedirects", IsRequired = false, DefaultValue = 7)]
        public int HttpRequestMaxAutoRedirects
        {
            get { return (int)this["httpRequestMaxAutoRedirects"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("isHttpRequestAutoRedirectsEnabled", IsRequired = false, DefaultValue = true)]
        public bool IsHttpRequestAutoRedirectsEnabled
        {
            get { return (bool)this["isHttpRequestAutoRedirectsEnabled"]; }
        }

        /// <summary>
        /// 
        /// </summary>

        [ConfigurationProperty("isHttpRequestAutomaticDecompressionEnabled", IsRequired = false)]
        public bool IsHttpRequestAutomaticDecompressionEnabled
        {
            get { return (bool)this["isHttpRequestAutomaticDecompressionEnabled"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("isSendingCookiesEnabled", IsRequired = false)]
        public bool IsSendingCookiesEnabled
        {
            get { return (bool)this["isSendingCookiesEnabled"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("isRespectUrlNamedAnchorOrHashbangEnabled", IsRequired = false)]
        public bool IsRespectUrlNamedAnchorOrHashbangEnabled
        {
            get { return (bool)this["isRespectUrlNamedAnchorOrHashbangEnabled"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("minAvailableMemoryRequiredInMb", IsRequired = false)]
        public int MinAvailableMemoryRequiredInMb
        {
            get { return (int)this["minAvailableMemoryRequiredInMb"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("maxMemoryUsageInMb", IsRequired = false)]
        public int MaxMemoryUsageInMb
        {
            get { return (int)this["maxMemoryUsageInMb"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("maxMemoryUsageCacheTimeInSeconds", IsRequired = false)]
        public int MaxMemoryUsageCacheTimeInSeconds
        {
            get { return (int)this["maxMemoryUsageCacheTimeInSeconds"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("maxCrawlDepth", IsRequired = false, DefaultValue = 100)]
        public int MaxCrawlDepth
        {
            get { return (int)this["maxCrawlDepth"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("maxLinksPerPage", IsRequired = false, DefaultValue = 0)]
        public int MaxLinksPerPage
        {
            get { return (int)this["maxLinksPerPage"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("isForcedLinkParsingEnabled", IsRequired = false)]
        public bool IsForcedLinkParsingEnabled
        {
            get { return (bool)this["isForcedLinkParsingEnabled"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("maxRetryCount", IsRequired = false)]
        public int MaxRetryCount
        {
            get { return (int)this["maxRetryCount"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("minRetryDelayInMilliseconds", IsRequired = false)]
        public int MinRetryDelayInMilliseconds
        {
            get { return (int)this["minRetryDelayInMilliseconds"]; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class ExtensionValueElement : ConfigurationElement
    {
        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("key", IsRequired = false, IsKey = true)]
        public string Key
        {
            get { return (string)this["key"]; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("value", IsRequired = false, IsKey = false)]
        public string Value
        {
            get { return (string)this["value"]; }
        }

    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class ExtensionValueCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ExtensionValueElement this[int index]
        {
            get { return (ExtensionValueElement)BaseGet(index); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new ExtensionValueElement();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ExtensionValueElement)element).Key;
        }
    }
}
