using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelLoopLibrary.Tests
{
    [TestClass]
    public class TestsArgumentsValidation
    {
        [TestMethod]
        public void ArgumentsValidationBeginWith()
        {
            // BeginWith, void
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => ParallelLoopBuilder.BeginWithSynchronous((Action)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => ParallelLoopBuilder.BeginWith((Action)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => ParallelLoopBuilder.BeginWith((Func<Task>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }

            // BeginWith, producer
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => ParallelLoopBuilder.BeginWithSynchronous((Func<int>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => ParallelLoopBuilder.BeginWith((Func<int>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => ParallelLoopBuilder.BeginWith((Func<Task<int>>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
        }

        [TestMethod]
        public void ArgumentsValidationAdd()
        {
            var baseBuilder = ParallelLoopBuilder.BeginWith(() => { });

            // Independent, void
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.AddSynchronous((Action)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.Add((Action)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.Add((Func<Task>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }

            // Independent, producer
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.AddSynchronous((Func<int>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.Add((Func<int>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.Add((Func<Task<int>>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
        }

        [TestMethod]
        public void ArgumentsValidationAdd_TResult()
        {
            var baseBuilder = ParallelLoopBuilder.BeginWith(() => 0);

            // Independent, void
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.AddSynchronous((Action)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.Add((Action)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.Add((Func<Task>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }

            // Independent, producer
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.AddSynchronous((Func<int>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.Add((Func<int>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.Add((Func<Task<int>>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }

            // Dependent, void
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.AddSynchronous((Action<int>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.Add((Action<int>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.Add((Func<int, Task>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }

            // Dependent, producer
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.AddSynchronous((Func<int, int>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.Add((Func<int, int>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
            {
                var exception = Assert.ThrowsException<ArgumentNullException>(
                    () => baseBuilder.Add((Func<int, Task<int>>)null));
                Assert.IsTrue(exception.ParamName == "action");
            }
        }

        [TestMethod]
        public void NotInitializedBuilder()
        {
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                var builder = new ParallelLoopBuilder();
                builder.ToParallelLoop(default);
            });
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                var builder = new ParallelLoopBuilder();
                builder.ToParallelLoop(default, default);
            });
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                var builder = new ParallelLoopBuilder();
                builder.ToParallelLoop(default, default, default);
            });
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                var builder = new ParallelLoopBuilder<int>();
                builder.ToParallelLoop(default);
            });
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                var builder = new ParallelLoopBuilder<int>();
                builder.ToParallelLoop(default, default);
            });
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                var builder = new ParallelLoopBuilder<int>();
                builder.ToParallelLoop(default, default, default);
            });
        }
    }
}
