using System;
using System.Collections.Generic;
using System.Threading;

namespace AlphaSharp
{
    /// <summary>
    /// PyTorch needs threads, not tasks, to run in parallel. There is some thread-local state and tasks are not hard-bound to threads.
    /// For heavy'ish work, creates and destroys threads.
    /// </summary>
    internal class ThreadedConsumer<TIn, TOut>
    {
        private readonly Semaphore _semaphore;
        private readonly Func<TIn, TOut> _worker;
        private readonly List<TOut> _results = new();
        private readonly object _lock = new();

        public ThreadedConsumer(Func<TIn, TOut> worker, int maxThreads)
        {
            _semaphore = new Semaphore(maxThreads, maxThreads);
            _worker = worker;
        }

        private void ThreadMethod(object param)
        {
            var item = (TIn)param;

            _semaphore.WaitOne();

            try
            {
                var result = _worker(item);

                lock (_lock)
                    _results.Add(result);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public List<TOut> Run(List<TIn> work)
        {
            _results.Clear();

            var threads = new List<Thread>();
            foreach(var item in work)
            {
                var thread = new Thread(ThreadMethod);
                thread.Start(item);
                threads.Add(thread);
            }

            foreach (var thread in threads)
                thread.Join();

            return _results;
        }
    }
}
