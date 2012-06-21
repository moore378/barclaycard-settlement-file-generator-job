using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace TransactionManagementCommon.ControllerBase
{
    /// <summary>
    /// A default implementation of the IServerGovernor interface
    /// </summary>
    public class ServerGovernor : IServerGovernor
    {
        int jobCount = 0;
        int errorCount = 0;
        int activatedTasks = 0;
        int maxSimultaneousTasks = 5;
        Queue<Task> unstartedTasks = new Queue<Task>();

        public ServerGovernor(int maxSimultaneousTasks = Int32.MaxValue)
        {
            this.maxSimultaneousTasks = maxSimultaneousTasks;
        }

        public Task<TResult> EnqueueJob<TResult>(ServerJob<TResult> job)
        {
            var task = new Task<TResult>(() =>
                {
                    bool success = true;
                    Interlocked.Increment(ref jobCount);
                    try
                    {
                        return job(ref success);
                    }
                    finally
                    {
                        TaskFinished();
                        Interlocked.Decrement(ref jobCount);
                        if (!success)
                            Interlocked.Increment(ref errorCount);
                    }
                });

            lock (unstartedTasks)
            {
                if (activatedTasks < maxSimultaneousTasks)
                {
                    activatedTasks++;
                    task.Start();
                }
                else
                    unstartedTasks.Enqueue(task);
            }

            return task;
        }

        private void TaskFinished()
        {
            lock (unstartedTasks)
            {
                activatedTasks--;
                while (unstartedTasks.Count > 0 && activatedTasks < maxSimultaneousTasks)
                {
                    activatedTasks++;
                    unstartedTasks.Dequeue().Start();
                }
            }
        }
    }
}
