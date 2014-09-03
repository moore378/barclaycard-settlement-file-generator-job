using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

    public class ArrayEx<T>
    {
        public static readonly T[] Empty = new T[0];
    }

    public class ArrayEx
    {
        public static string EntriesToString<T>(T[] arr)
        {
            return arr.JoinStr(",");
        }
    }

