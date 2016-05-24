using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MjhGeneral
{
    public class AsyncSemaphore : IDisposable
    {
        private int count;
        private Queue<TaskCompletionSource<bool>> waiting = new Queue<TaskCompletionSource<bool>>();
        private Task<bool> completed = Task.FromResult(false);

        public AsyncSemaphore(int initialCount)
        {
            count = initialCount;
        }

        public Task WaitAsync()
        {
            lock (waiting)
            {
                if (count > 0)
                {
                    count--;
                    return completed;
                }
                else
                {
                    var newTask = new TaskCompletionSource<bool>();
                    waiting.Enqueue(newTask);
                    return newTask.Task;
                }
            }
        }

        public void Release()
        {
            TaskCompletionSource<bool> toFinish = null;
            lock (waiting)
            {
                if (waiting.Count > 0)
                    toFinish = waiting.Dequeue();
                else
                    count++;
            }
            if (toFinish != null)
                toFinish.SetResult(true);
        }

        public void Dispose()
        {
            lock (waiting)
            {
                while (waiting.Count > 0)
                    waiting.Dequeue().SetException(new ObjectDisposedException("AsyncSemaphore"));
            }
        }
    }
}
