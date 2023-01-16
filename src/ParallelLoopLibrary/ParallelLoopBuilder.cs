using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelLoopLibrary
{
    internal struct ParallelLoopEntry
    {
        internal readonly bool HasInput;
        internal readonly bool HasOutput;
        internal readonly bool IsSynchronous;
        internal readonly Func<object, object> SyncAction;
        internal readonly Func<object, Task<object>> AsyncAction;

        internal ParallelLoopEntry(bool hasInput, bool hasOutput, Func<object, object> syncAction, bool isSynchronous)
        {
            Debug.Assert(syncAction != null);
            this.HasInput = hasInput;
            this.HasOutput = hasOutput;
            this.IsSynchronous = isSynchronous;
            this.SyncAction = syncAction;
            this.AsyncAction = null;
        }

        internal ParallelLoopEntry(bool hasInput, bool hasOutput, Func<object, Task<object>> asyncAction)
        {
            Debug.Assert(asyncAction != null);
            this.HasInput = hasInput;
            this.HasOutput = hasOutput;
            this.IsSynchronous = false;
            this.SyncAction = null;
            this.AsyncAction = asyncAction;
        }
    }

    /// <summary>
    /// Factory for creating builders of parallel loops.
    /// Also, an immutable struct that holds the metadata for building a parallel loop of
    /// independent actions.
    /// </summary>
    public struct ParallelLoopBuilder
    {
        private readonly ParallelLoopEntry[] _entries;

        internal ParallelLoopBuilder(ParallelLoopEntry[] previousEntries,
            Func<object, object> newAction, bool isSynchronous = false)
        {
            Debug.Assert(newAction != null);
            var newEntry = new ParallelLoopEntry(false, false, newAction, isSynchronous);
            _entries = ParallelLoopCommon.Append(previousEntries, newEntry);
        }
        internal ParallelLoopBuilder(ParallelLoopEntry[] previousEntries,
            Func<object, Task<object>> newAction)
        {
            Debug.Assert(newAction != null);
            var newEntry = new ParallelLoopEntry(false, false, newAction);
            _entries = ParallelLoopCommon.Append(previousEntries, newEntry);
        }

        /// <summary>
        /// Creates a parallel loop builder that holds the metadata for
        /// an independent action, that will be invoked synchronously inside the loop.
        /// </summary>
        public static ParallelLoopBuilder BeginWithSynchronous(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder(null,
                _ => { action(); return (object)null; }, isSynchronous: true);
        }
        /// <summary>
        /// Creates a parallel loop builder that holds the metadata for
        /// an independent action, that will be invoked on the ThreadPool.
        /// </summary>
        public static ParallelLoopBuilder BeginWith(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder(null,
                _ => { action(); return (object)null; });
        }
        /// <summary>
        /// Creates a parallel loop builder that holds the metadata for
        /// an independent asynchronous delegate, that will be invoked synchronously
        /// inside the loop, and awaited in the next iteration of the loop.
        /// </summary>
        public static ParallelLoopBuilder BeginWith(Func<Task> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder(null,
                async _ => { await action().ConfigureAwait(false); return (object)null; });
        }


        /// <summary>
        /// Creates a parallel loop builder that holds the metadata for
        /// an independent action that produces a result, that will be invoked
        /// synchronously inside the loop.
        /// </summary>
        public static ParallelLoopBuilder<TResult> BeginWithSynchronous<TResult>(Func<TResult> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TResult>(null, hasInput: false, hasOutput: true,
                newAction: _ => (object)action(), isSynchronous: true);
        }
        /// <summary>
        /// Creates a parallel loop builder that holds the metadata for
        /// an independent action that produces a result, that will be invoked
        /// on the ThreadPool.
        /// </summary>
        public static ParallelLoopBuilder<TResult> BeginWith<TResult>(Func<TResult> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TResult>(null, hasInput: false, hasOutput: true,
                newAction: _ => (object)action());
        }
        /// <summary>
        /// Creates a parallel loop builder that holds the metadata for
        /// an independent asynchronous delegate that produces a result, that will be
        /// invoked synchronously inside the loop, and awaited in the next iteration of
        /// the loop.
        /// </summary>
        public static ParallelLoopBuilder<TResult> BeginWith<TResult>(Func<Task<TResult>> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TResult>(null, hasInput: false, hasOutput: true,
                newAction: async _ => (object)await action().ConfigureAwait(false));
        }


        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// an independent action, that will be invoked synchronously inside the loop.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder AddSynchronous(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder(_entries,
                _ => { action(); return (object)null; }, isSynchronous: true);
        }
        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// an independent action, that will be invoked on the ThreadPool.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder Add(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder(_entries,
                _ => { action(); return (object)null; });
        }
        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// an independent asynchronous delegate, that will be invoked synchronously
        /// inside the loop, and awaited in the next iteration of the loop.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder Add(Func<Task> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder(_entries,
                async _ => { await action().ConfigureAwait(false); return (object)null; });
        }


        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// an independent action that produces a result, that will be invoked
        /// synchronously inside the loop.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TResult> AddSynchronous<TResult>(Func<TResult> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TResult>(_entries, hasInput: false, hasOutput: true,
                newAction: _ => (object)action(), isSynchronous: true);
        }
        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// an independent action that produces a result, that will be invoked
        /// on the ThreadPool.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TResult> Add<TResult>(Func<TResult> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TResult>(_entries, hasInput: false, hasOutput: true,
                newAction: _ => (object)action());
        }
        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// an independent asynchronous delegate that produces a result, that will be
        /// invoked synchronously inside the loop, and awaited in the next iteration of
        /// the loop.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TResult> Add<TResult>(Func<Task<TResult>> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TResult>(_entries, hasInput: false, hasOutput: true,
                newAction: async _ => (object)await action().ConfigureAwait(false));
        }

        /// <summary>
        /// Materializes the metadata stored in this builder by creating and starting
        /// an asynchronous parallel loop, while observing a stopping CancellationToken.
        /// </summary>
        /// <remarks>
        /// The actions that this builder contains are invoked in parallel to each other,
        /// but sequentially to themselves. Actions that depend on other actions are
        /// started when their dependency produces the first result. When the
        /// stopping CancellationToken is canceled, the parallel loop stops after all
        /// the actions have been executed an equal number of times.
        /// </remarks>
        public Task ToParallelLoop(CancellationToken stoppingToken)
        {
            return ToParallelLoop(stoppingToken, default(CancellationToken), false);
        }

        /// <summary>
        /// Materializes the metadata stored in this builder by creating and starting
        /// an asynchronous parallel loop, while observing a stopping CancellationToken
        /// and a canceling CancellationToken.
        /// </summary>
        /// <remarks>
        /// The actions that this builder contains are invoked in parallel to each other,
        /// but sequentially to themselves. Actions that depend on other actions are
        /// started when their dependency produces the first result. When the
        /// stopping CancellationToken is canceled, the parallel loop stops after all
        /// the actions have been executed an equal number of times.
        /// When the canceling CancellationToken is canceled, the parallel loop is canceled
        /// after all the currently running actions have completed.
        /// </remarks>
        public Task ToParallelLoop(CancellationToken stoppingToken,
            CancellationToken cancelingToken)
        {
            return ToParallelLoop(stoppingToken, cancelingToken, false);
        }

        /// <summary>
        /// Materializes the metadata stored in this builder by creating and starting
        /// an asynchronous parallel loop, while observing a stopping CancellationToken
        /// and a canceling CancellationToken, specifying whether the loop should be
        /// executed on the current SynchronizationContext.
        /// </summary>
        /// <remarks>
        /// The actions that this builder contains are invoked in parallel to each other,
        /// but sequentially to themselves. Actions that depend on other actions are
        /// started when their dependency produces the first result. When the
        /// stopping CancellationToken is canceled, the parallel loop stops after all
        /// the actions have been executed an equal number of times.
        /// When the canceling CancellationToken is canceled, the parallel loop is canceled
        /// after all the currently running actions have completed.
        /// </remarks>
        public Task ToParallelLoop(CancellationToken stoppingToken,
            CancellationToken cancelingToken, bool executeOnCurrentContext)
        {
            if (_entries == null) throw new InvalidOperationException();
            return ParallelLoopCommon.ToParallelLoop(_entries, stoppingToken, cancelingToken, executeOnCurrentContext);
        }
    }

    /// <summary>
    /// An immutable struct that holds the metadata for building a parallel loop,
    /// with the last action of the loop producing a result.
    /// </summary>
    public struct ParallelLoopBuilder<TResult>
    {
        private readonly ParallelLoopEntry[] _entries;

        internal ParallelLoopBuilder(ParallelLoopEntry[] previousEntries,
            bool hasInput, bool hasOutput,
            Func<object, object> newAction, bool isSynchronous = false)
        {
            Debug.Assert(newAction != null);
            var newEntry = new ParallelLoopEntry(hasInput, hasOutput, newAction, isSynchronous);
            _entries = ParallelLoopCommon.Append(previousEntries, newEntry);
        }
        internal ParallelLoopBuilder(ParallelLoopEntry[] previousEntries,
            bool hasInput, bool hasOutput, Func<object, Task<object>> newAction)
        {
            Debug.Assert(newAction != null);
            var newEntry = new ParallelLoopEntry(hasInput, hasOutput, newAction);
            _entries = ParallelLoopCommon.Append(previousEntries, newEntry);
        }

        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// an independent action, that will be invoked synchronously inside the loop.
        /// The result of the previous action can still be used by a subsequent action.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TResult> AddSynchronous(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TResult>(_entries, hasInput: false, hasOutput: false,
                newAction: _ => { action(); return (object)null; }, isSynchronous: true);
        }
        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// an independent action, that will be invoked on the ThreadPool.
        /// The result of the previous action can still be used by a subsequent action.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TResult> Add(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TResult>(_entries, hasInput: false, hasOutput: false,
                newAction: _ => { action(); return (object)null; });
        }
        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// an independent asynchronous delegate, that will be invoked synchronously
        /// inside the loop, and awaited in the next iteration of the loop.
        /// The result of the previous action can still be used by a subsequent action.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TResult> Add(Func<Task> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TResult>(_entries, hasInput: false, hasOutput: false,
                newAction: async _ => { await action().ConfigureAwait(false); return (object)null; });
        }


        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// an independent action that produces a new result, that will be invoked
        /// synchronously inside the loop.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TNewResult> AddSynchronous<TNewResult>(Func<TNewResult> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TNewResult>(_entries, hasInput: false, hasOutput: true,
                newAction: _ => (object)action(), isSynchronous: true);
        }
        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// an independent action that produces a new result, that will be invoked
        /// on the ThreadPool.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TNewResult> Add<TNewResult>(Func<TNewResult> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TNewResult>(_entries, hasInput: false, hasOutput: true,
                newAction: _ => (object)action());
        }
        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// an independent asynchronous delegate that produces a new result, that will be
        /// invoked synchronously inside the loop, and awaited in the next iteration of
        /// the loop.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TNewResult> Add<TNewResult>(Func<Task<TNewResult>> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TNewResult>(_entries, hasInput: false, hasOutput: true,
                newAction: async _ => (object)await action().ConfigureAwait(false));
        }


        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// a dependent action, that will be invoked synchronously inside the loop.
        /// The result of the previous action can still be used by a subsequent action.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TResult> AddSynchronous(Action<TResult> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TResult>(_entries, hasInput: true, hasOutput: false,
                newAction: arg => { action(Cast<TResult>(arg)); return (object)null; }, isSynchronous: true);
        }
        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// a dependent action, that will be invoked on the ThreadPool.
        /// The result of the previous action can still be used by a subsequent action.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TResult> Add(Action<TResult> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TResult>(_entries, hasInput: true, hasOutput: false,
                newAction: arg => { action(Cast<TResult>(arg)); return (object)null; });
        }
        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// a dependent asynchronous delegate, that will be invoked synchronously inside
        /// the loop, and awaited in the next iteration of the loop.
        /// The result of the previous action can still be used by a subsequent action.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TResult> Add(Func<TResult, Task> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TResult>(_entries, hasInput: true, hasOutput: false,
                newAction: async arg => { await action(Cast<TResult>(arg)).ConfigureAwait(false); return (object)null; });
        }


        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// a dependent action that produces a new result, that will be invoked synchronously
        /// inside the loop.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TNewResult> AddSynchronous<TNewResult>(Func<TResult, TNewResult> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TNewResult>(_entries, hasInput: true, hasOutput: true,
                newAction: arg => (object)action(Cast<TResult>(arg)), isSynchronous: true);
        }
        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// a dependent action that produces a new result, that will be invoked on
        /// the ThreadPool.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TNewResult> Add<TNewResult>(Func<TResult, TNewResult> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TNewResult>(_entries, hasInput: true, hasOutput: true,
                newAction: arg => (object)action(Cast<TResult>(arg)));
        }
        /// <summary>
        /// Creates a new builder that holds all the metadata of the current builder,
        /// plus the metadata for
        /// a dependent asynchronous delegate that produces a new result, that will be
        /// invoked synchronously inside the loop, and awaited in the next iteration of
        /// the loop.
        /// </summary>
        /// <remarks>The current builder is not changed.</remarks>
        public ParallelLoopBuilder<TNewResult> Add<TNewResult>(Func<TResult, Task<TNewResult>> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ParallelLoopBuilder<TNewResult>(_entries, hasInput: true, hasOutput: true,
                newAction: async arg => (object)await action(Cast<TResult>(arg)).ConfigureAwait(false));
        }

        /// <summary>
        /// Materializes the metadata stored in this builder by creating and starting
        /// an asynchronous parallel loop, while observing a stopping CancellationToken.
        /// </summary>
        /// <remarks>
        /// The actions that this builder contains are invoked in parallel to each other,
        /// but sequentially to themselves. Actions that depend on other actions are
        /// started when their dependency produces the first result. When the
        /// stopping CancellationToken is canceled, the parallel loop stops after all
        /// the actions have been executed an equal number of times.
        /// </remarks>
        public Task ToParallelLoop(CancellationToken stoppingToken)
        {
            return ToParallelLoop(stoppingToken, default(CancellationToken), false);
        }

        /// <summary>
        /// Materializes the metadata stored in this builder by creating and starting
        /// an asynchronous parallel loop, while observing a stopping CancellationToken
        /// and a canceling CancellationToken.
        /// </summary>
        /// <remarks>
        /// The actions that this builder contains are invoked in parallel to each other,
        /// but sequentially to themselves. Actions that depend on other actions are
        /// started when their dependency produces the first result. When the
        /// stopping CancellationToken is canceled, the parallel loop stops after all
        /// the actions have been executed an equal number of times.
        /// When the canceling CancellationToken is canceled, the parallel loop is canceled
        /// after all the currently running actions have completed.
        /// </remarks>
        public Task ToParallelLoop(CancellationToken stoppingToken,
            CancellationToken cancelingToken)
        {
            return ToParallelLoop(stoppingToken, cancelingToken, false);
        }

        /// <summary>
        /// Materializes the metadata stored in this builder by creating and starting
        /// an asynchronous parallel loop, while observing a stopping CancellationToken
        /// and a canceling CancellationToken, specifying whether the loop should be
        /// executed on the current SynchronizationContext.
        /// </summary>
        /// <remarks>
        /// The actions that this builder contains are invoked in parallel to each other,
        /// but sequentially to themselves. Actions that depend on other actions are
        /// started when their dependency produces the first result. When the
        /// stopping CancellationToken is canceled, the parallel loop stops after all
        /// the actions have been executed an equal number of times.
        /// When the canceling CancellationToken is canceled, the parallel loop is canceled
        /// after all the currently running actions have completed.
        /// </remarks>
        public Task ToParallelLoop(CancellationToken stoppingToken,
            CancellationToken cancelingToken, bool executeOnCurrentContext)
        {
            if (_entries == null) throw new InvalidOperationException();
            return ParallelLoopCommon.ToParallelLoop(_entries, stoppingToken, cancelingToken, executeOnCurrentContext);
        }

        private static T Cast<T>(object obj)
        {
            try { return (T)obj; }
            catch
            {
                throw new InvalidCastException(
                    $"Casting from {obj?.GetType()?.Name ?? "(null)"} to {typeof(T).Name} failed.");
            }
        }
    }

    internal static class ParallelLoopCommon
    {
        internal static Task ToParallelLoop(
            ParallelLoopEntry[] entries,
            CancellationToken stoppingToken,
            CancellationToken cancelingToken,
            bool executeOnCurrentContext)
        {
            Debug.Assert(entries != null);
            Debug.Assert(entries.Length > 0);
            if (cancelingToken.IsCancellationRequested) return Task.FromCanceled(cancelingToken);
            if (stoppingToken.IsCancellationRequested) return Task.CompletedTask;
            // https://stackoverflow.com/questions/55687698/how-to-return-aggregateexception-from-async-method/
            if (executeOnCurrentContext)
                return ToParallelLoopImplementation(entries, stoppingToken, cancelingToken).Unwrap();
            else
                return Task.Run(() => ToParallelLoopImplementation(entries, stoppingToken, cancelingToken).Unwrap());
        }

        private static async Task<Task> ToParallelLoopImplementation(
            ParallelLoopEntry[] entries,
            CancellationToken stoppingToken,
            CancellationToken cancelingToken)
        {
            // Initialization
            var tasks = new Task<object>[entries.Length];
            var hysteresis = new int[entries.Length];
            // Calculate hysteresis
            {
                int currentHysteresis = 0;
                bool incrementNextDependent = false;
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].HasInput && incrementNextDependent)
                    {
                        currentHysteresis++;
                        incrementNextDependent = false;
                    }
                    hysteresis[i] = currentHysteresis;
                    if (entries[i].HasOutput && !entries[i].IsSynchronous)
                        incrementNextDependent = true;
                }
            }

            // Loop
            try
            {
                long counter = -1;
                long cancelNumber = Int64.MaxValue;
                while (true)
                {
                    counter++;

                    Maybe result = default(Maybe);
                    for (int i = 0; i < entries.Length; i++)
                    {
                        if (counter < hysteresis[i]) break;

                        // Await the previous task
                        Maybe newResult = default(Maybe);
                        if (tasks[i] != null && !entries[i].IsSynchronous)
                            newResult = new Maybe(await tasks[i], true);

                        if (i == 0)
                        {
                            // First entry in the array, observe cancellation request
                            if (cancelNumber == Int64.MaxValue && stoppingToken.IsCancellationRequested)
                                cancelNumber = counter;
                        }

                        // Break early in case any other task has failed, before starting
                        // a new task.
                        foreach (var task in tasks)
                            if (task != null && IsFaultedOrIsCanceled(task))
                                task.GetAwaiter().GetResult(); // Throw to exit the loop


                        // Start a new task
                        if (counter - hysteresis[i] < cancelNumber)
                        {
                            Debug.Assert(!(entries[i].HasInput && !result.HasValue), i.ToString());
                            tasks[i] = CreateTask(entries[i], result.Value, cancelingToken);

                            if (entries[i].IsSynchronous)
                            {
                                // Grab the result of the task immediately
                                Debug.Assert(tasks[i].IsCompleted);
                                newResult = new Maybe(tasks[i].GetAwaiter().GetResult(), true);
                            }
                        }

                        if (entries[i].HasOutput && newResult.HasValue) result = newResult;

                        if (i == entries.Length - 1)
                        {
                            // Last entry in the array, satisfy cancellation request
                            if (counter - hysteresis[i] >= cancelNumber)
                                stoppingToken.ThrowIfCancellationRequested();
                        }
                        cancelingToken.ThrowIfCancellationRequested();
                    }
                }
            }
            catch (Exception ex)
            {
                // Await all started tasks, and propagate all of their exceptions.
                // Tasks that are canceled by unknown tokens, are converted to faulted.
                Task allTasks = Task.WhenAll(tasks.Where(t => t != null).Select(
                    t => CanceledToFaultedConditional(t, stoppingToken, cancelingToken)));
                try { await allTasks.ConfigureAwait(false); }
                catch
                {
                    if (allTasks.IsCanceled && !cancelingToken.IsCancellationRequested
                        && stoppingToken.IsCancellationRequested)
                    {
                        return Task.CompletedTask; // The stoppingToken has stop semantics
                    }
                    return allTasks;
                }

                // The only expected exception at this point is an OperationCanceledException,
                // originated from either the stoppingToken or the cancelingToken.
                var oce = ex as OperationCanceledException;
                if (oce != null)
                {
                    if (oce.CancellationToken == cancelingToken)
                        return TaskFromCanceledSafe(oce.CancellationToken); // https://stackoverflow.com/questions/69552580/a-canceled-task-propagates-two-different-types-of-exceptions-depending-on-how-i

                    if (oce.CancellationToken == stoppingToken) return Task.CompletedTask;
                }

                // If the code reached this point, we have a bug.
                Debug.Fail($"Unexpected exception: {ex.ToString()}");
                throw;
            }
        }

        private static Task<object> CreateTask(ParallelLoopEntry entry, object argument,
            CancellationToken cancelingToken)
        {
            Debug.Assert(entry.SyncAction != null || entry.AsyncAction != null);
            if (!entry.HasInput) argument = null;

            Task<object> task;
            if (entry.AsyncAction != null)
            {
                task = entry.AsyncAction(argument); // The AsyncAction is always async-generated
            }
            else if (!entry.IsSynchronous)
            {
                var syncAction = entry.SyncAction;
                task = Task.Run(() => syncAction(argument), cancelingToken);
            }
            else // SyncAction & IsSynchronous
            {
                try { task = Task.FromResult(entry.SyncAction(argument)); }
                catch (OperationCanceledException oce) { task = TaskFromCanceledSafe(oce.CancellationToken); }
                catch (Exception ex) { task = Task.FromException<object>(ex); }
            }
            return task;
        }

        private static Task<object> TaskFromCanceledSafe(CancellationToken token)
        {
            TaskCompletionSource<object> tcs = new();
            tcs.SetCanceled(token);
            return tcs.Task;
        }

        private static Task<object> CanceledToFaultedConditional(Task<object> task,
            CancellationToken stoppingToken, CancellationToken cancelingToken)
        {
            return task.ContinueWith(t =>
            {
                if (t.IsCanceled && !cancelingToken.IsCancellationRequested
                    && !stoppingToken.IsCancellationRequested)
                {
                    // The task is canceled, but none of the known tokens are canceled
                    t.GetAwaiter().GetResult(); // Propagate failure
                }
                return t; // In any other case, propagate the task as is
            }, default(CancellationToken), TaskContinuationOptions.ExecuteSynchronously |
                TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default)
            .Unwrap();
        }

        // Struct that simplifies the logic inside the loop. A ValueTuple<object, bool>
        // could be used instead, but we want to support .NET Framework 4.6 / C# 6.0.
        private struct Maybe
        {
            public readonly object Value;
            public readonly bool HasValue;
            public Maybe(object value, bool hasValue) { Value = value; HasValue = hasValue; }
        }

        private static bool IsFaultedOrIsCanceled(Task task)
        {
            Debug.Assert(task != null);
            var status = task.Status;
            return status == TaskStatus.Faulted || status == TaskStatus.Canceled;
        }

        internal static T[] Append<T>(T[] array, T item)
        {
            T[] newArray;
            if (array == null || array.Length == 0)
            {
                newArray = new T[1];
            }
            else
            {
                newArray = new T[array.Length + 1];
                Array.Copy(array, newArray, array.Length);
            }
            newArray[newArray.Length - 1] = item;
            return newArray;
        }
    }
}
