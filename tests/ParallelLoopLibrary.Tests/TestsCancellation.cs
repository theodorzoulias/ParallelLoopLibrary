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
    public class TestsCancellation
    {
        [TestMethod]
        public async Task CanceledActions()
        {
            {
                var cts = new CancellationTokenSource();
                var parallelLoop = ParallelLoopBuilder
                    .BeginWith(() => Task.Delay(100, cts.Token))
                    .Add(() => cts.Cancel())
                    .ToParallelLoop(cts.Token);
                await parallelLoop;
            }
            {
                var cts = new CancellationTokenSource();
                var parallelLoop = ParallelLoopBuilder
                    .BeginWith(() => { cts.Token.WaitHandle.WaitOne(100); cts.Token.ThrowIfCancellationRequested(); })
                    .Add(() => cts.Cancel())
                    .ToParallelLoop(cts.Token);
                var ex = await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                    () => parallelLoop);
                Assert.IsTrue(ex.CancellationToken == cts.Token);
                Assert.IsTrue(parallelLoop.IsFaulted);
                Assert.IsTrue(parallelLoop.Exception.InnerExceptions.Count == 1);
            }
        }

        [TestMethod]
        public async Task CancelingToken()
        {
            var cancelingSource = new CancellationTokenSource();
            var counter1 = 0;
            var counter2 = 0;
            var parallelLoop = ParallelLoopBuilder
                .BeginWithSynchronous(() => { return ++counter1; })
                .Add(x => x)
                .Add(x => x)
                .AddSynchronous(x => { if (x == 10) cancelingSource.Cancel(); })
                .Add(x => x)
                .Add(x => x)
                .AddSynchronous(() => { ++counter2; })
                .ToParallelLoop(CancellationToken.None, cancelingSource.Token);
            var ex = await Assert.ThrowsExceptionAsync<TaskCanceledException>(
                () => parallelLoop);
            Assert.IsTrue(ex.CancellationToken == cancelingSource.Token);
            Assert.IsTrue(parallelLoop.IsCanceled);
            var aex = Assert.ThrowsException<AggregateException>(
                () => parallelLoop.Wait());
            Assert.IsTrue(aex.InnerExceptions.Count == 1);
            Assert.IsTrue(aex.InnerExceptions[0] is TaskCanceledException);
            Assert.IsTrue(counter1 == 12, counter1.ToString());
            Assert.IsTrue(counter2 == 8, counter2.ToString());
        }

        [TestMethod]
        public async Task StoppingTokenAndCancelingToken()
        {
            var stoppingSource = new CancellationTokenSource();
            var cancelingSource = new CancellationTokenSource();
            var counter1 = 0;
            var counter2 = 0;
            var parallelLoop = ParallelLoopBuilder
                .BeginWithSynchronous(() => { return ++counter1; })
                .Add(x => x)
                .Add(x => x)
                .AddSynchronous(x => { if (x == 10) stoppingSource.Cancel(); })
                .AddSynchronous(x => { if (x == 11) cancelingSource.Cancel(); })
                .Add(x => x)
                .Add(x => x)
                .AddSynchronous(() => { ++counter2; })
                .ToParallelLoop(stoppingSource.Token, cancelingSource.Token);
            var ex = await Assert.ThrowsExceptionAsync<TaskCanceledException>(
                () => parallelLoop);
            Assert.IsTrue(ex.CancellationToken == cancelingSource.Token);
            Assert.IsTrue(parallelLoop.IsCanceled);
            var aex = Assert.ThrowsException<AggregateException>(
                () => parallelLoop.Wait());
            Assert.IsTrue(aex.InnerExceptions.Count == 1);
            Assert.IsTrue(aex.InnerExceptions[0] is TaskCanceledException);
            Assert.IsTrue(counter1 == 12, counter1.ToString());
            Assert.IsTrue(counter2 == 9, counter2.ToString());
        }
    }
}
