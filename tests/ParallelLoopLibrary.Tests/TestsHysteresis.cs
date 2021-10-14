using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelLoopLibrary.Tests
{
    [TestClass]
    public class TestsHysteresis
    {
        [TestMethod]
        public async Task HysteresisDependentTasks()
        {
            var cts = new CancellationTokenSource();
            int counter1 = 0;
            int counter2 = 0;
            int counter3 = 0;
            int counter4 = 0;
            int hysteresis12 = 0;
            int hysteresis13 = 0;
            int hysteresis23 = 0;
            int hysteresis14 = 0;
            int hysteresis24 = 0;
            int hysteresis34 = 0;
            await ParallelLoopBuilder
            .BeginWith(() =>
            {
                Interlocked.Increment(ref counter1);
                if (counter1 == 10) cts.Cancel();
                return counter1;
            })
            .Add(async result =>
            {
                Interlocked.Increment(ref counter2);
                UpdateHysteresis(ref hysteresis12, ref counter1, ref counter2);
                await Task.Yield();
                return result.ToString();
            })
            .Add(result =>
            {
                Interlocked.Increment(ref counter3);
                UpdateHysteresis(ref hysteresis13, ref counter1, ref counter3);
                UpdateHysteresis(ref hysteresis23, ref counter2, ref counter3);
                return Int32.Parse(result);
            })
            .Add(async result =>
            {
                Interlocked.Increment(ref counter4);
                UpdateHysteresis(ref hysteresis14, ref counter1, ref counter4);
                UpdateHysteresis(ref hysteresis24, ref counter2, ref counter4);
                UpdateHysteresis(ref hysteresis34, ref counter3, ref counter4);
                await Task.Yield();
            })
            .ToParallelLoop(cts.Token);
            Assert.IsTrue(counter1 == 10);
            Assert.IsTrue(counter2 == 10);
            Assert.IsTrue(counter3 == 10);
            Console.WriteLine($"Hysteresis: {hysteresis12}/{hysteresis13}/{hysteresis23}/{hysteresis14}/{hysteresis24}/{hysteresis34}");
            Assert.That.IsBetween(hysteresis12, 1, 2);
            Assert.That.IsBetween(hysteresis13, 2, 3);
            Assert.That.IsBetween(hysteresis23, 1, 2);
            Assert.That.IsBetween(hysteresis14, 3, 4);
            Assert.That.IsBetween(hysteresis24, 2, 3);
            Assert.That.IsBetween(hysteresis34, 1, 2);
        }

        [TestMethod]
        public async Task HysteresisIndependentTasks()
        {
            var cts = new CancellationTokenSource();
            int counter1 = 0;
            int counter2 = 0;
            int counter3 = 0;
            int counter4 = 0;
            int hysteresis12 = 0;
            int hysteresis13 = 0;
            int hysteresis23 = 0;
            int hysteresis14 = 0;
            int hysteresis24 = 0;
            int hysteresis34 = 0;
            await ParallelLoopBuilder
            .BeginWith(() =>
            {
                Interlocked.Increment(ref counter1);
                if (counter1 == 10) cts.Cancel();
            })
            .Add(async () =>
            {
                Interlocked.Increment(ref counter2);
                UpdateHysteresis(ref hysteresis12, ref counter1, ref counter2);
                await Task.Yield();
            })
            .Add(() =>
            {
                Interlocked.Increment(ref counter3);
                UpdateHysteresis(ref hysteresis13, ref counter1, ref counter3);
                UpdateHysteresis(ref hysteresis23, ref counter2, ref counter3);
            })
            .Add(async () =>
            {
                Interlocked.Increment(ref counter4);
                UpdateHysteresis(ref hysteresis14, ref counter1, ref counter4);
                UpdateHysteresis(ref hysteresis24, ref counter2, ref counter4);
                UpdateHysteresis(ref hysteresis34, ref counter3, ref counter4);
                await Task.Yield();
            })
            .ToParallelLoop(cts.Token);
            Assert.IsTrue(counter1 == 10);
            Assert.IsTrue(counter2 == 10);
            Assert.IsTrue(counter3 == 10);
            Console.WriteLine($"Hysteresis: {hysteresis12}/{hysteresis13}/{hysteresis23}/{hysteresis14}/{hysteresis24}/{hysteresis34}");
            Assert.That.IsBetween(hysteresis12, 0, 1);
            Assert.That.IsBetween(hysteresis13, 0, 1);
            Assert.That.IsBetween(hysteresis23, 0, 1);
            Assert.That.IsBetween(hysteresis14, 0, 1);
            Assert.That.IsBetween(hysteresis24, 0, 1);
            Assert.That.IsBetween(hysteresis34, 0, 1);
        }

        private static void UpdateHysteresis(ref int hysteresis, ref int countA, ref int countB)
        {
            int current = Volatile.Read(ref hysteresis);
            while (true)
            {
                int diff = Volatile.Read(ref countA) - Volatile.Read(ref countB);
                var original = Interlocked.CompareExchange(ref hysteresis, Math.Max(hysteresis, diff), current);
                if (original == current) break;
                current = original;
            }
        }
    }
}
