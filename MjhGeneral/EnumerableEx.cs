using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class EnumerableEx
{
    /// <summary>
    /// Creates an enumerable with only one element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static IEnumerable<T> Only<T>(T value)
    {
        yield return value;
    }

    public static IEnumerable<T> Subsequence<T>(this IEnumerable<T> enumerable, int start, int count)
    {
        return enumerable.Skip(start).Take(count);
    }

    /// <summary>
    /// Joins together an enumerable of elements into a string.
    /// </summary>
    /// <param name="joiner"></param>
    /// <returns></returns>
    public static string JoinStr<T>(this IEnumerable<T> enumerable, string joiner)
    {
        if (enumerable.Any())
            return enumerable.Select(e => e == null? "null" : e.ToString()).Aggregate((a, s) => a + joiner + s);
        else
            return "";
    }

    
    /// <summary>
    /// Given a sequence of data, and an async operation to perform on each data, returns
    /// a sequence of tasks representing the operation on the data such that no more than
    /// windowSize operations are done at once, and that all operations are done 
    /// sequencially.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    /// <param name="data">Data to operate on</param>
    /// <param name="operation">Operation to perform on each data</param>
    /// <param name="windowSize">Maximum consecutive simulataneous operations (size of sliding window)</param>
    /// <returns></returns>
    /// <remarks>The operations are started at the point of the call, not when
    /// the result is enumerated. Enumerating the result multiple times will not
    /// perform the tasks multiple times.
    /// 
    /// This algorithm uses a sliding window to choose when to start evaluating each operation
    /// based on the completion of previous operations. Successive operations are dependent on
    /// eachother in such a way that if one operation fails, all consecutive operations fail.</remarks>
    public static IEnumerable<Task<TOut>> SelectAsync<TIn, TOut>(this IEnumerable<TIn> data, Func<TIn, Task<TOut>> operation, int windowSize)
    {
        var dataEnumerator = data.GetEnumerator();
        var busy = new Queue<Task<TOut>>();
        var result = new List<Task<TOut>>(data.Count());

        Task prevStart = Task.FromResult(0);

        // First few
        while (busy.Count < windowSize && dataEnumerator.MoveNext())
        {
            var starting = StartTask(null, prevStart, dataEnumerator.Current, operation);
            var completing = starting.Unwrap();
            result.Add(completing);
            busy.Enqueue(completing);
            prevStart = starting;
        }

        // Remaining section
        while (dataEnumerator.MoveNext())
        {
            var parent = busy.Dequeue();
            var starting = StartTask(parent, prevStart, dataEnumerator.Current, operation);
            var completing = starting.Unwrap();
            result.Add(completing);
            busy.Enqueue(completing);
            prevStart = starting;
        }

        return result;
    }

    /// <summary>
    /// Given a sequence of data, and an async operation to perform on each data, returns
    /// a sequence of tasks representing the operation on the data such that no more than
    /// windowSize operations are done at once, and that all operations are done 
    /// sequencially.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <param name="data">Data to operate on</param>
    /// <param name="operation">Operation to perform on each data</param>
    /// <param name="windowSize">Maximum consecutive simulataneous operations (size of sliding window)</param>
    /// <returns></returns>
    /// <remarks>The operations are started at the point of the call, not when
    /// the result is enumerated. Enumerating the result multiple times will not
    /// perform the tasks multiple times.
    /// 
    /// This algorithm uses a sliding window to choose when to start evaluating each operation
    /// based on the completion of previous operations. Successive operations are dependent on
    /// eachother in such a way that if one operation fails, all consecutive operations fail.</remarks>
    public static IEnumerable<Task> SelectAsync<TIn>(this IEnumerable<TIn> data, Func<TIn, Task> operation, int windowSize)
    {
        var dataEnumerator = data.GetEnumerator();
        var busy = new Queue<Task>();
        var result = new List<Task>(data.Count());

        Task prevStart = Task.FromResult(0);

        // First few
        while (busy.Count < windowSize && dataEnumerator.MoveNext())
        {
            var starting = StartTask(null, prevStart, dataEnumerator.Current, operation);
            var completing = starting.Unwrap();
            result.Add(completing);
            busy.Enqueue(completing);
            prevStart = starting;
        }

        // Remaining section
        while (dataEnumerator.MoveNext())
        {
            var parent = busy.Dequeue();
            var starting = StartTask(parent, prevStart, dataEnumerator.Current, operation);
            var completing = starting.Unwrap();
            result.Add(completing);
            busy.Enqueue(completing);
            prevStart = starting;
        }

        return result;
    }

    private static async Task<Task<TOut>> StartTask<TIn, TOut>(Task parent, Task sibling, TIn data, Func<TIn, Task<TOut>> operation)
    {
        if (parent != null)
            await parent;
        if (sibling != null)
            await sibling;
        return operation(data);
    }

    private static async Task<Task> StartTask<TIn>(Task parent, Task sibling, TIn data, Func<TIn, Task> operation)
    {
        if (parent != null)
            await parent;
        if (sibling != null)
            await sibling;
        return operation(data);
    }

    /// <summary>
    /// Perform an asynchrous operation on asynchronous data.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    /// <param name="data"></param>
    /// <param name="operation"></param>
    /// <returns></returns>
    /// <remarks>Evaluating the result multiple times will not perform the 
    /// operations multiple times. Evaluation starts at the point of the call.</remarks>
    public static IEnumerable<Task<TOut>> SelectAsync<TIn, TOut>(this IEnumerable<Task<TIn>> data, Func<TIn, TOut> operation)
    {
        return data.Select(async (d) =>
        {
            var r = await d;
            return operation(r);
        }).ToArray();
    }    
}