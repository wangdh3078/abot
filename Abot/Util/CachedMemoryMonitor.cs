using log4net;
using System;
using System.Timers;

namespace Abot.Util
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class CachedMemoryMonitor : IMemoryMonitor, IDisposable
    {
        static ILog _logger = LogManager.GetLogger("AbotLogger");
        IMemoryMonitor _memoryMonitor;
        Timer _usageRefreshTimer;
        int _cachedCurrentUsageInMb;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memoryMonitor"></param>
        /// <param name="cacheExpirationInSeconds"></param>
        public CachedMemoryMonitor(IMemoryMonitor memoryMonitor, int cacheExpirationInSeconds)
        {
            if (memoryMonitor == null)
                throw new ArgumentNullException("memoryMonitor");

            if (cacheExpirationInSeconds < 1)
                cacheExpirationInSeconds = 5;

            _memoryMonitor = memoryMonitor;

            UpdateCurrentUsageValue();

            _usageRefreshTimer = new Timer(cacheExpirationInSeconds * 1000);
            _usageRefreshTimer.Elapsed += (sender, e) => UpdateCurrentUsageValue();
            _usageRefreshTimer.Start();
        }
        /// <summary>
        /// 
        /// </summary>
        protected virtual void UpdateCurrentUsageValue()
        {
            int oldUsage = _cachedCurrentUsageInMb;
            _cachedCurrentUsageInMb = _memoryMonitor.GetCurrentUsageInMb();
            _logger.DebugFormat("Updated cached memory usage value from [{0}mb] to [{1}mb]", oldUsage, _cachedCurrentUsageInMb);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual int GetCurrentUsageInMb()
        {
            return _cachedCurrentUsageInMb;
        }
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _usageRefreshTimer.Stop();
            _usageRefreshTimer.Dispose();
        }
    }
}
