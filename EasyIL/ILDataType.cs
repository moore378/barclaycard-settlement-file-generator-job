using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyIL
{
    public static class ILDataType
    {
        public static bool IsILAssignableFrom(this Type type1, Type type2)
        {
            if (type1 == typeof(Word32))
            {
                // Word32 means either an integer type or an object type
                if (typeof(int).IsILAssignableFrom(type2)) return true;
                else if (typeof(object).IsILAssignableFrom(type2)) return true;
                else return false;
            }
            else if (type1.IsClass)
            {
                if (type2.IsClass) return type1.IsAssignableFrom(type2);
                else if (type2.IsInterface) return type1.IsAssignableFrom(type2);
                else return false;
            }
            else if (type1.IsInterface)
            {
                if (type2.IsInterface || type2.IsClass) return type1.IsAssignableFrom(type2);
                else return false;
            }
            else if (type1.IsValueType)
            {
                if (type1 == type2)
                    return true;
                // Int, Enum, and Bool are all the same in IL
                else if (type1 == typeof(int) || type1.IsEnum || type1 == typeof(bool))
                {
                    if (type2 == typeof(int) || type2.IsEnum || type2 == typeof(bool)) return true;
                    else return false;
                }
                else return false;
            }
            else return false;
        }

    }
}
