using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class ObjectEx
{
    /// <summary>
    /// If value is not null, returns func(value), else returns the default TOut
    /// </summary>
    /// <remarks>This is like a maybe-monad</remarks>
    public static TOut Transform<TIn, TOut>(this TIn value, Func<TIn, TOut> func)
    {
        if (value != null)
            return func(value);
        else
            return default(TOut);
    }

    public static IEnumerable<KeyValuePair<string, object>> FieldsAndProps(object obj)
    {
        Type objType = obj.GetType();
        foreach (var field in objType.GetFields())
            yield return new KeyValuePair<string, object>(field.Name, field.GetValue(obj));
    }
}

