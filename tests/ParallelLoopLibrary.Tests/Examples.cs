using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using ParallelLoopLibrary;

namespace ParallelLoopLibrary.Tests
{
    public static class Examples
    {
        static void Example1()
        {
            long iteration = 0;
            var cts = new CancellationTokenSource();
            var pts = new PauseTokenSource();
            Task paralleLoop = ParallelLoopBuilder
                .BeginWith(() => FetchRemoteExpiredEntries())
                .Add(() => Task.Delay(TimeSpan.FromSeconds(10)))
                .AddSynchronous(() => pts.Token.WaitWhilePaused())
                .AddSynchronous(() => LogIteration(++iteration))
                .Add(entries => DeleteEntriesFromLocalDatabase(entries))
                .Add(deletedRecords => MoveFilesToRecycleBin(deletedRecords))
                .ToParallelLoop(cts.Token);

            pts.IsPaused = true; // or false

            static object[] FetchRemoteExpiredEntries() => default;
            static object[] DeleteEntriesFromLocalDatabase(object[] arg) => default;
            static void MoveFilesToRecycleBin(object[] arg) { };
            static void LogIteration(long arg) { };
        }

        static void Example2()
        {
            var stoppingSource = new CancellationTokenSource();
            var cancelingSource = new CancellationTokenSource();
            var uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Task paralleLoop = ParallelLoopBuilder
                .BeginWith(() => { /* ... */ })
                .Add(() => Task.Factory.StartNew(() =>
                {
                    progressBar1.Value++;
                }, default, TaskCreationOptions.None, uiScheduler))
                //...
                .ToParallelLoop(stoppingSource.Token, cancelingSource.Token);
        }
        private static class progressBar1 { public static int Value; }
    }
}
