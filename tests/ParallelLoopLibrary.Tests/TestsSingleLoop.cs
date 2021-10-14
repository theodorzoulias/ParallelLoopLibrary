using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelLoopLibrary.Tests
{
    [TestClass]
    public class TestsSingleLoop
    {
        [TestMethod]
        public void OneActionOneLoop()
        {
            // BeginWith, void
            {
                var (t0, t1, loops, cts) = InitializeState1();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => Loop(ref loops, ref t1, cts))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops == 1);
                Assert.IsTrue(t1 == t0);
            }
            {
                var (t0, t1, loops, cts) = InitializeState1();
                ParallelLoopBuilder
                    .BeginWith(() => Loop(ref loops, ref t1, cts))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops == 1);
                Assert.IsTrue(t1 != t0);
            }
            {
                var (t0, t1, loops, cts) = InitializeState1();
                ParallelLoopBuilder
                    .BeginWith(() => LoopAsync(ref loops, ref t1, cts))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops == 1);
                Assert.IsTrue(t1 == t0);
            }

            // BeginWith, producer
            {
                var (t0, t1, loops, cts) = InitializeState1();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => LoopWithResult(ref loops, ref t1, cts))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops == 1);
                Assert.IsTrue(t1 == t0);
            }
            {
                var (t0, t1, loops, cts) = InitializeState1();
                ParallelLoopBuilder
                    .BeginWith(() => LoopWithResult(ref loops, ref t1, cts))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops == 1);
                Assert.IsTrue(t1 != t0, $"{t1}/{t0}");
            }
            {
                var (t0, t1, loops, cts) = InitializeState1();
                ParallelLoopBuilder
                    .BeginWith(() => LoopWithResultAsync(ref loops, ref t1, cts))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops == 1);
                Assert.IsTrue(t1 == t0);
            }
        }

        [TestMethod]
        public void TwoActionsOneLoop()
        {
            // Independent, void
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => Loop(ref loops1, ref t1, cts))
                    .AddSynchronous(() => Loop(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 == t0);
            }
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => Loop(ref loops1, ref t1, cts))
                    .Add(() => Loop(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 != t0);
            }
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => Loop(ref loops1, ref t1, cts))
                    .Add(() => LoopAsync(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 == t0);
            }

            // Independent, producer
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => Loop(ref loops1, ref t1, cts))
                    .AddSynchronous(() => LoopWithResult(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 == t0);
            }
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => Loop(ref loops1, ref t1, cts))
                    .Add(() => LoopWithResult(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 != t0);
            }
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => Loop(ref loops1, ref t1, cts))
                    .Add(() => LoopWithResultAsync(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 == t0);
            }
        }

        [TestMethod]
        public void TwoActionsOneLoop_TResult()
        {
            // Independent, propagator, void
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => LoopWithResult(ref loops1, ref t1, cts))
                    .AddSynchronous(() => Loop(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 == t0);
            }
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => LoopWithResult(ref loops1, ref t1, cts))
                    .Add(() => Loop(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 != t0);
            }
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => LoopWithResult(ref loops1, ref t1, cts))
                    .Add(() => LoopAsync(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 == t0);
            }

            // Independent, propagator, producer
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => LoopWithResult(ref loops1, ref t1, cts))
                    .AddSynchronous(() => LoopWithResult(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 == t0);
            }
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => LoopWithResult(ref loops1, ref t1, cts))
                    .Add(() => LoopWithResult(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 != t0);
            }
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => LoopWithResult(ref loops1, ref t1, cts))
                    .Add(() => LoopWithResultAsync(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 == t0);
            }

            // Dependent, void
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => LoopWithResult(ref loops1, ref t1, cts))
                    .AddSynchronous(x => Loop(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 == t0);
            }
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => LoopWithResult(ref loops1, ref t1, cts))
                    .Add(x => Loop(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 != t0);
            }
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => LoopWithResult(ref loops1, ref t1, cts))
                    .Add(x => LoopAsync(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 == t0);
            }

            // Dependent, producer
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => LoopWithResult(ref loops1, ref t1, cts))
                    .AddSynchronous(x => LoopWithResult(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 == t0);
            }
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => LoopWithResult(ref loops1, ref t1, cts))
                    .Add(x => LoopWithResult(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 != t0);
            }
            {
                var (t0, t1, t2, loops1, loops2, cts) = InitializeState2();
                ParallelLoopBuilder
                    .BeginWithSynchronous(() => LoopWithResult(ref loops1, ref t1, cts))
                    .Add(x => LoopWithResultAsync(ref loops2, ref t2, null))
                    .ToParallelLoop(cts.Token, default, true)
                    .Wait();
                Assert.IsTrue(loops1 == 1);
                Assert.IsTrue(loops2 == 1);
                Assert.IsTrue(t1 == t0);
                Assert.IsTrue(t2 == t0);
            }
        }

        [TestMethod]
        public void ExecuteOnCurrentContext()
        {
            AsyncContext.Run(async () =>
            {
                var cts = new CancellationTokenSource();
                int t0 = Thread.CurrentThread.ManagedThreadId;
                var tids = new List<int>();
                int loops = 0;
                await ParallelLoopBuilder
                    .BeginWith(async () =>
                    {
                        loops++;
                        tids.Add(Thread.CurrentThread.ManagedThreadId);
                        await Task.Yield();
                        if (loops == 5) cts.Cancel();
                    })
                    .ToParallelLoop(cts.Token, default, executeOnCurrentContext: false);
                Assert.IsTrue(loops >= 5);
                Assert.IsTrue(tids.All(t1 => t1 != t0));
            });
            AsyncContext.Run(async () =>
            {
                var cts = new CancellationTokenSource();
                int t0 = Thread.CurrentThread.ManagedThreadId;
                var tids = new List<int>();
                int loops = 0;
                await ParallelLoopBuilder
                    .BeginWith(async () =>
                    {
                        loops++;
                        tids.Add(Thread.CurrentThread.ManagedThreadId);
                        await Task.Yield();
                        if (loops >= 5) cts.Cancel();
                    })
                    .ToParallelLoop(cts.Token, default, executeOnCurrentContext: true);
                Assert.IsTrue(loops == 5);
                Assert.IsTrue(tids.All(t1 => t1 == t0));
            });
        }

        static (int, int, int, CancellationTokenSource) InitializeState1()
        {
            return (Thread.CurrentThread.ManagedThreadId, 0, 0, new CancellationTokenSource());
        }
        static (int, int, int, int, int, CancellationTokenSource) InitializeState2()
        {
            return (Thread.CurrentThread.ManagedThreadId, 0, 0, 0, 0, new CancellationTokenSource());
        }
        private static void Loop(ref int count, ref int threadId, CancellationTokenSource cts)
        {
            count++; threadId = Thread.CurrentThread.ManagedThreadId; cts?.Cancel();
        }
        private static Task LoopAsync(ref int count, ref int threadId, CancellationTokenSource cts)
        {
            Loop(ref count, ref threadId, cts); return Task.Run(() => { });
        }
        private static int LoopWithResult(ref int count, ref int threadId, CancellationTokenSource cts)
        {
            Loop(ref count, ref threadId, cts); return 13;
        }
        private static Task<int> LoopWithResultAsync(ref int count, ref int threadId, CancellationTokenSource cts)
        {
            Loop(ref count, ref threadId, cts); return Task.Run(() => 13);
        }
    }
}
