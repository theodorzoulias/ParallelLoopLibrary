![Logo](logo.png)

# Parallel Loop Library

This .NET library helps at creating loops of actions that are running in parallel to each other,
but sequentially to themselves. On each iteration of the loop, all actions are invoked once.
The result of one action can by the input of another action, in which case the second
action is dependent on the first action. Dependent actions start running when their
dependency produces the first result. The loop ends when a
[`CancellationToken`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken)
is canceled.

## How to make a parallel loop

You starting building the loop with the static `ParallelLoopBuilder.BeginWith` method,
specifying the first action of the loop. Then you use the `Add` method to add more actions
in the loop. The `Add` invocations should be chained, because they don't modify the current
builder. Instead they return a new builder each time. Finally, when all the actions have
been added, you call the `ToParallelLoop` method to create and start the
[`Task`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task)
that represents the loop. Example:

```C#
using ParallelLoopLibrary;

var cts = new CancellationTokenSource();
Task paralleLoop = ParallelLoopBuilder
    .BeginWith(() => FetchRemoteExpiredEntries())
    .Add(entries => DeleteEntriesFromLocalDatabase(entries))
    .Add(deletedRecords => MoveFilesToRecycleBin(deletedRecords))
    .ToParallelLoop(cts.Token);
```

In this example the loop consists of three actions, with the second action dependent on
the first action, and the third action dependent on the second action. It will take two
iterations before all three actions are up and running in parallel. The duration of each
iteration of the loop will be determined by the slowest of the three actions. As soon
as all three action have completed, a new iteration of the loop will start immediately.

In each iteration the first action will be fetching the most recent expired entries,
the second action will be deleting from the database the entries that were fetched one
iteration back, and the third action will be recycling the files that were deleted from the
database one iteration back. So the third action will always be two steps behind the
first action. Eventually the third action will have the chance to catch up, when the
[`CancellationTokenSource`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource)
is canceled. The loop is not stopped abruptly. Instead
the loop will terminate with fairness, when all the actions have been executed an
equal number of times. When this happens, the `Task` will transition to the
[`RanToCompletion`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskstatus) state.

## What types of actions are supported?

Regarding their scheduling, three types of actions are supported:

1. Actions that are invoked on the [`ThreadPool`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.threadpool) (added with `Add`).
2. Asynchronous delegates (like a [`Func<Task>`](https://docs.microsoft.com/en-us/dotnet/api/system.func-1)) that are invoked synchronously inside the
loop, and are awaited on the next iteration of the loop (added with `Add`).
3. Actions that are invoked synchronously inside the loop (added with `AddSynchronous`).
These actions can produce results that are immediately available by the next action
(without hysteresis).

Regarding their argument and return type, four types of actions are supported:

1. Actions with no input and no output, like an [`Action`](https://docs.microsoft.com/en-us/dotnet/api/system.action). These are completely independent
from other actions.
2. Actions with input and no output, like an [`Action<string>`](https://docs.microsoft.com/en-us/dotnet/api/system.action-1). These are dependent on
results produced by previous actions.
3. Actions with output and no input, like a [`Func<int>`](https://docs.microsoft.com/en-us/dotnet/api/system.func-1). These are independent, but
other actions may depend on them.
4. Actions with both input and output, like a [`Func<int, string>`](https://docs.microsoft.com/en-us/dotnet/api/system.func-2). These are dependent
on results produced by previous actions, and also other actions may depend on their results.

When a no-result action is added after an action that produces a result, that result
can still be used by a subsequent action. For example:

```C#
long iteration = 0;
Task paralleLoop = ParallelLoopBuilder
    .BeginWith(() => FetchRemoteExpiredEntries())
    .AddSynchronous(() => LogIteration(++iteration))
    .Add(entries => DeleteEntriesFromLocalDatabase(entries))
    //...
```

The entries produced by the first action are "passing through" the second independent action, and
are still available for the third dependent action.

## Where is the loop running?

The loop in running on the [`ThreadPool`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.threadpool).
It is possible to configure it to run on the current
[`SynchronizationContext`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.synchronizationcontext),
by using the `ToParallelLoop` overload that accepts a `executeOnCurrentContext` argument.

## How to enforce a minimum interval between iterations

You can just intercept an [asynchronous delay](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.delay)
action in the parallel loop:

```C#
Task paralleLoop = ParallelLoopBuilder
    .BeginWith(() => FetchRemoteExpiredEntries())
    .Add(() => Task.Delay(TimeSpan.FromSeconds(10)))
    //...
```

## How to suspend temporarily a parallel loop

You could use the `PauseTokenSource`/[`PauseToken`](https://github.com/StephenCleary/AsyncEx/blob/master/src/Nito.AsyncEx.Coordination/PauseToken.cs)
mechanism, from Stephen Cleary's
[Nito.AsyncEx.Coordination](https://www.nuget.org/packages/Nito.AsyncEx.Coordination/) package:

```C#
using Nito.AsyncEx;
using ParallelLoopLibrary;

var cts = new CancellationTokenSource();
var pts = new PauseTokenSource();
Task paralleLoop = ParallelLoopBuilder
    .BeginWith(() => FetchRemoteExpiredEntries())
    .AddSynchronous(() => pts.Token.WaitWhilePaused())
    //...
    .ToParallelLoop(cts.Token);

//...
pts.IsPaused = true; // or false
```

## How to terminate ASAP a parallel loop

The `ToParallelLoop` method has an overload that accepts two `CancellationToken`
arguments. The first argument is named `stoppingToken`, and has the semantics described
earlier (stops the loop with fairness, after an equal number of executions).
The second argument is named `cancelingToken`, and has canceling semantics. When the
`cancelingToken` is canceled, the parallel loop completes as [`Canceled`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskstatus)
as soon as all the currently running actions are completed.
You can provide either one of these two tokens, or both of them. Example:

```C#
var stoppingSource = new CancellationTokenSource();
var cancelingSource = new CancellationTokenSource();
Task paralleLoop = ParallelLoopBuilder
    //...
    .ToParallelLoop(stoppingSource.Token, cancelingSource.Token);

//...
cancelingSource.Cancel();
```

## How to run an action on the UI thread

You can use the [`TaskScheduler.FromCurrentSynchronizationContext`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler.fromcurrentsynchronizationcontext)
method to create a `TaskScheduler` associated with the UI thread. Then
add an action that invokes the asynchronous [`Task.Factory.StartNew`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskfactory.startnew) method,
and pass the UI scheduler as argument:

```
var uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
Task paralleLoop = ParallelLoopBuilder
    //...
    .Add(() => Task.Factory.StartNew(() =>
    {
        progressBar1.Value++;
    }, default, TaskCreationOptions.None, uiScheduler))
    //...
```

## How to embed this library into your project

This library has no NuGet package. You can either [download](https://github.com/theodorzoulias/ParallelLoopLibrary/releases) the project and build it locally, or just
embed the single code file [`ParallelLoopBuilder.cs`](https://github.com/theodorzoulias/ParallelLoopLibrary/blob/main/src/ParallelLoopLibrary/ParallelLoopBuilder.cs)
(~800 lines of code) into your project.
This library has been tested on the .NET Core 3.0, .NET 5 and .NET Framework 4.6 platforms.

## Performance

This library was created having coarse-grained operations in mind. It has not been
micro-optimized regarding the allocation of the few, small, short-lived objects that
are created while the loop is running. If the work is too lightweight, the overhead
might be significant. As a rule of thumb, around 250 bytes are allocated temporarily
for each execution of each action.
