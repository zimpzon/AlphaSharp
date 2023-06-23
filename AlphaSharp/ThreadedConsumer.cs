using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace AlphaSharp
{
    /// <summary>
    /// PyTorch needs threads, not tasks, to run in parallel. There is some thread-local state and tasks are not hard-bound to threads.
    /// </summary>
    internal class ThreadedConsumer<TIn, TOut>
    {
        private readonly Func<TIn, TOut> _worker;
        private readonly List<TOut> _results = new();
        private readonly object _lock = new();
        private readonly ConcurrentQueue<TIn> _workQueue = new();
        private readonly int _maxThreads;

        public ThreadedConsumer(Func<TIn, TOut> worker, List<TIn> work, int maxThreads)
        {
            _worker = worker;
            _workQueue = new ConcurrentQueue<TIn>(work);
            _maxThreads = maxThreads;
        }

        private void ThreadMethod(object _)
        {
            while (_workQueue.TryDequeue(out var item))
            {
                var result = _worker(item);
                lock (_lock)
                    _results.Add(result);
            }
        }

        public List<TOut> Run()
        {
            _results.Clear();

            var threads = new List<Thread>();

            for (int i = 0; i < _maxThreads; ++i)
            {
                var thread = new Thread(ThreadMethod);
                threads.Add(thread);
                thread.Start();
            }

            foreach (var thread in threads)
                thread.Join();

            return _results;
        }
    }
}
