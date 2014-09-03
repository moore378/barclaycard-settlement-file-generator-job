using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

internal static class EnumerableEx
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
            return enumerable.Select(e => e.ToString()).Aggregate((a, s) => a + joiner + s);
        else
            return "";
    }
}