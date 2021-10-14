using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelLoopLibrary.Tests
{
    [TestClass]
    public class TestsOther
    {
        [TestMethod]
        public async Task DiverseLoop()
        {
            var cts = new CancellationTokenSource();
            var source = Enumerable.Range(1, 20);
            var output = new List<int>();
            int counter = 0;
            await ParallelLoopBuilder
                .BeginWith(source.ToLoop(cts))
                .AddSynchronous(() => { counter++; })
                .Add(() => { })
                .Add(async () => await Task.Yield())
                .AddSynchronous(x => { })
                .Add(x => { })
                .Add(async x => await Task.Yield())
                .AddSynchronous(x => (long)x)
                .Add(x => (int)x)
                .Add(async x => { await Task.Yield(); return x; })
                .AddSynchronous(x => output.Add(x))
                .ToParallelLoop(cts.Token);
            Assert.IsTrue(counter == source.Count());
            Assert.IsTrue(output.Count == source.Count(), output.Count.ToString());
            Assert.IsTrue(output.SequenceEqual(source), String.Join(", ", output));
        }

        [TestMethod]
        public void VeryWideDiverseLoop()
        {
            var cts = new CancellationTokenSource();
            var source = Enumerable.Range(1, 50);
            var output = new List<int>();
            int counter = 0;
            var builder = ParallelLoopBuilder
                .BeginWith(source.ToLoop(cts))
                .AddSynchronous(() => { counter++; });
            for (int i = 0; i < 20; i++)
            {
                builder = builder
                    .AddSynchronous(() => { })
                    .Add(() => { })
                    .Add(async () => await Task.Yield())
                    .AddSynchronous(x => { })
                    .Add(x => { })
                    .Add(async x => await Task.Yield())
                    .AddSynchronous(x => x)
                    .Add(x => x)
                    .Add(async x => { await Task.Yield(); return x; });
            }
            var parallelLoop = builder
                .AddSynchronous(x => output.Add(x))
                .ToParallelLoop(cts.Token);
            parallelLoop.Wait();
            Assert.IsTrue(counter == source.Count());
            Assert.IsTrue(output.Count == source.Count(), output.Count.ToString());
            Assert.IsTrue(output.SequenceEqual(source), String.Join(", ", output));
        }

        [TestMethod]
        public void MultipleErrors()
        {
            var cts = new CancellationTokenSource();
            var source = Enumerable.Range(1, 20);
            var barrier = new AsyncBarrier(5);
            var parallelLoop = ParallelLoopBuilder
                .BeginWith(source.ToLoop(cts))
                .Add(async x => { await ThrowIfEqualAsync(x, 10); return x; })
                .Add(async x => { await ThrowIfEqualAsync(x, 9); return x; })
                .Add(async x => { await ThrowIfEqualAsync(x, 8); return x; })
                .Add(async x => { await ThrowIfEqualAsync(x, 7); return x; })
                .Add(async x => { await ThrowIfEqualAsync(x, 6); return x; })
                .ToParallelLoop(cts.Token);
            var aex = Assert.ThrowsException<AggregateException>(() =>
            {
                bool success = parallelLoop.Wait(200);
                if (!success) throw new TimeoutException();
            });
            Console.WriteLine(String.Join(", ", aex.InnerExceptions.Select(ex => ex.Message)));
            Assert.IsTrue(aex.InnerExceptions.Count == 5);
            Assert.IsTrue(aex.InnerExceptions.All(ex => ex is ApplicationException));
            Assert.IsTrue(aex.InnerExceptions
                .Select(ex => ex.Message)
                .OrderBy(x => Int32.TryParse(x, out int v) ? v : 0)
                .SequenceEqual(new[] { "6", "7", "8", "9", "10" }));

            async Task ThrowIfEqualAsync(int value, int comparand)
            {
                if (value == comparand)
                {
                    await barrier.SignalAndWaitAsync().WithTimeout(100);
                    throw new ApplicationException(value.ToString());
                }
            }
        }
    }
}
