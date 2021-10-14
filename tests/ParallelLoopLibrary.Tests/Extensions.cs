using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelLoopLibrary.Tests
{
    internal static class AssertExtensions
    {
        public static void IsBetween(this Assert source, int number, int from, int to)
        {
            Assert.IsTrue(number >= from && number <= to, $"{number} is not between {from} and {to}.");
        }
    }

    internal static class TaskExtensions
    {
        // https://stackoverflow.com/questions/4238345/asynchronously-wait-for-taskt-to-complete-with-timeout/11191070#11191070
        public static Task WithTimeout(this Task task, int millisecondsTimeout)
        {
            var cts = new CancellationTokenSource(millisecondsTimeout);
            return task
                .ContinueWith(_ => { }, cts.Token,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .ContinueWith(continuation =>
                {
                    cts.Dispose();
                    if (task.IsCompleted) return task;
                    if (continuation.IsCanceled) throw new TimeoutException();
                    return task;
                }, default, TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default).Unwrap();
        }
    }

    internal static class EnumerableExtensions
    {
        public static Func<T> ToLoop<T>(this IEnumerable<T> source,
            CancellationTokenSource cts = default)
        {
            Debug.Assert(source != null);
            var enumerator = source.GetEnumerator();
            T cached = default;
            Func<T> loop = () =>
            {
                var current = cached;
                if (enumerator == null)
                    throw new InvalidOperationException("The enumerator has completed.");
                if (enumerator.MoveNext())
                    cached = enumerator.Current;
                else
                {
                    cached = default;
                    try { enumerator.Dispose(); }
                    finally { enumerator = null; cts?.Cancel(); }
                }
                return current;
            };
            loop();
            return loop;
        }
    }
}
