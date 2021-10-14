using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ParallelLoopLibrary.Tests
{
    public class AsyncBarrier
    {
        private readonly object _locker = new object();
        private readonly int _participantCount;
        private TaskCompletionSource<object> _tcs;
        private int _count;

        public AsyncBarrier(int participantCount)
        {
            Debug.Assert(participantCount > 0);
            _participantCount = participantCount;
        }

        public Task SignalAndWaitAsync()
        {
            TaskCompletionSource<object> completed;
            lock (_locker)
            {
                if (_tcs == null)
                    _tcs = new TaskCompletionSource<object>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                _count++;
                if (_count < _participantCount) return _tcs.Task;
                completed = _tcs;
                _tcs = null;
            }
            if (completed != null) completed.SetResult(null);
            return completed.Task;
        }
    }
}
