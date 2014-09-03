using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

public static class TypeEx
{
    /// <summary>
    /// Gets the single attribute of type T, or null if none.
    /// </summary>
    public static T SingleAttribute<T>(this Type type)
        where T: Attribute
    {
        T result = type.GetCustomAttributes(typeof(T), false).Cast<T>().SingleOrDefault();
        if (result == null)
            return type.GetCustomAttributes(typeof(T), true).Cast<T>().SingleOrDefault();
        else
            return result;
    }

    /// <summary>
    /// Gets the single attribute of type T, or null if none.
    /// </summary>
    public static T SingleAttribute<T>(this MemberInfo member)
        where T : Attribute
    {
        T result = member.GetCustomAttributes(typeof(T), false).Cast<T>().SingleOrDefault();
        if (result == null)
            return member.GetCustomAttributes(typeof(T), true).Cast<T>().SingleOrDefault();
        else
            return result;
    }

    /// <summary>
    /// Gets the single attribute of type T, or null if none.
    /// </summary>
    public static T SingleAttribute<T>(this ParameterInfo param)
        where T : Attribute
    {
        T result = param.GetCustomAttributes(typeof(T), false).Cast<T>().SingleOrDefault();
        if (result == null)
            return param.GetCustomAttributes(typeof(T), true).Cast<T>().SingleOrDefault();
        else
            return result;
    }

    /// <summary>
    /// Gets the single attribute of type T, or null if none.
    /// </summary>
    public static T SingleAttribute<T>(this ICustomAttributeProvider provider)
        where T : Attribute
    {
        T result = provider.GetCustomAttributes(typeof(T), false).Cast<T>().SingleOrDefault();
        if (result == null)
            return provider.GetCustomAttributes(typeof(T), true).Cast<T>().SingleOrDefault();
        else
            return result;
    }
}
