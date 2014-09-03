using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
    public static class TaskEx2
    {
        public async static Task<TOut> Select<TOut, TIn>(this Task<TIn> task, Func<TIn, TOut> selector)
        {
            return selector(await task);
        }

        public async static Task<TOut> Select<TOut, TIn>(this Task<TIn> task, Func<TIn, Task<TOut>> selector)
        {
            return await selector(await task);
        }

        public async static Task Select<TIn>(this Task<TIn> task, Action<TIn> selector)
        {
            selector(await task);
        }

        public async static Task Select<TIn>(this Task<TIn> task, Func<TIn, Task> selector)
        {
            await selector(await task);
        }

        /// <summary>
        /// Returns a new task that will timeout after the given period
        /// </summary>
        /// <remarks>This overload does not (cannot) cancel the task when the timeout occurs.</remarks>
        public async static Task<T> TimeoutAfter<T>(this Task<T> task, int milliseconds)
        {
            if (task.IsCompleted || milliseconds == Timeout.Infinite)
                return task.Result;
            var cancelTimeout = new CancellationTokenSource();
            var timeoutTask = TaskEx.Delay(milliseconds, cancelTimeout.Token);
            Task first = await TaskEx.WhenAny(task, timeoutTask);
            if (first == task)
            {
                cancelTimeout.Cancel();
                return task.Result;
            }
            else
                throw new TimeoutException("Task timed out after " + milliseconds + "ms");
        }

        /// <summary>
        /// Returns a new task that will timeout after the given period
        /// </summary>
        /// <param name="cancelTask">Called on timeout to allow caller to cancel the inner task</param>
        public async static Task<T> TimeoutAfter<T>(this Task<T> task, int milliseconds, Action cancelTask)
        {
            if (task.IsCompleted || milliseconds == Timeout.Infinite)
                return task.Result;
            var cancelTimeout = new CancellationTokenSource();
            var timeoutTask = TaskEx.Delay(milliseconds, cancelTimeout.Token);
            Task first = await TaskEx.WhenAny(task, timeoutTask);
            if (first == task)
            {
                cancelTimeout.Cancel();
                return task.Result;
            }
            else
            {
                cancelTask();
                throw new TimeoutException("Task timed out after " + milliseconds + "ms");
            }
        }

        /// <summary>
        /// Returns a new task that will timeout after the given period
        /// </summary>
        /// <remarks>This overload does not (cannot) cancel the task when the timeout occurs.</remarks>
        public async static Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
        {
            if (task.IsCompleted)
                return task.Result;
            var cancelTimeout = new CancellationTokenSource();
            var timeoutTask = TaskEx.Delay(timeout, cancelTimeout.Token);
            Task first = await TaskEx.WhenAny(task, timeoutTask);
            if (first == task)
            {
                cancelTimeout.Cancel();
                return task.Result;
            }
            else
                throw new TimeoutException("Task timed out after " + timeout);
        }

        /// <summary>
        /// Returns a new task that will timeout after the given period
        /// </summary>
        /// <param name="cancelTask">Called on timeout to allow caller to cancel the inner task</param>
        public async static Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout, Action cancelTask)
        {
            if (task.IsCompleted)
                return task.Result;
            var cancelTimeout = new CancellationTokenSource();
            var timeoutTask = TaskEx.Delay(timeout, cancelTimeout.Token);
            Task first = await TaskEx.WhenAny(task, timeoutTask);
            if (first == task)
            {
                cancelTimeout.Cancel();
                return task.Result;
            }
            else
            {
                cancelTask();
                throw new TimeoutException("Task timed out after " + timeout);
            }
        }

        public async static Task<T> Catch<T, TException>(this Task<T> task, Func<TException, Task<T>> handler)
            where TException : Exception
        {
            TException ex = null;
            try
            {
                return await task;
            }
            catch (TException e)
            {
                ex = e;
            }
            return await handler(ex);
        }

        public async static Task<T> TryCatch<T, TException>(Func<Task<T>> task, Func<TException, Task<T>> handler)
            where TException : Exception
        {
            TException ex = null;
            try
            {
                return await task();
            }
            catch (TException e)
            {
                ex = e;
            }
            return await handler(ex);
        }

        public async static Task Catch<TException>(this Task task, Func<TException, Task> handler)
            where TException : Exception
        {
            TException ex = null;
            try
            {
                await task;
                return;
            }
            catch (TException e)
            {
                ex = e;
            }
            await handler(ex);
        }

        public async static Task TryCatch<TException>(Func<Task> task, Func<TException, Task> handler)
            where TException : Exception
        {
            TException ex = null;
            try
            {
                await task();
                return;
            }
            catch (TException e)
            {
                ex = e;
            }
            await handler(ex);
        }

        public async static Task<T> Catch<T, TException>(this Task<T> task, Func<TException, Task> handler)
            where TException : Exception
        {
            Task<T> running = task;
            TException ex = null;
            try
            {
                return await running;
            }
            catch (TException e)
            {
                ex = e;
            }
            await handler(ex);
            return await running;
        }

        public async static Task<T> TryCatch<T, TException>(Func<Task<T>> task, Func<TException, Task> handler)
            where TException : Exception
        {
            Task<T> running = task();
            TException ex = null;
            try
            {
                return await running;
            }
            catch (TException e)
            {
                ex = e;
            }
            await handler(ex);
            return await running;
        }

        public async static Task<T> TryFinally<T>(this Task<T> task, Func<Task> doFinally)
        {
            Task<T> running = task;
            try
            {
                return await running;
            }
            catch { } // Supress the error for now.
            await doFinally();
            return await running; // This will re-throw the exception
        }

        public async static Task<T> TryFinally<T>(Func<Task<T>> task, Func<Task> doFinally)
        {
            Task<T> running = task();
            try
            {
                return await running;
            }
            catch { } // Supress the error for now.
            await doFinally();
            return await running; // This will re-throw the exception
        }
    }
}