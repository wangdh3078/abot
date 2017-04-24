using System;
using System.Threading;

namespace Abot.Util
{
    /// <summary>
    /// A ThreadManager implementation that will use real Threads to handle concurrency.
    /// </summary>
    [Serializable]
    public class ManualThreadManager : ThreadManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxThreads"></param>
        public ManualThreadManager(int maxThreads)
            :base(maxThreads)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        protected override void RunActionOnDedicatedThread(Action action)
        {
            new Thread(() => RunAction(action)).Start();
        }
    }
}