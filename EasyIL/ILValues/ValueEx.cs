using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EasyIL
{
    public static class ValueEx
    {
        /// <summary>
        /// Creates an IL sequence to write this L-value with the given R-value
        /// </summary>
        /// <remarks>If the compile-time type is not known, use <see cref="AssignUnsafe"/></remarks>
        public static IStatement Assign<T>(this IWValue<T> lvalue, IRValue<T> rvalue)
        {
            if (!lvalue.ValueType.IsAssignableFrom(rvalue.ValueType))
                throw new InvalidOperationException("Type mismatch in assignment. Expected " + lvalue.ValueType + ", but got " + rvalue.ValueType);
            return new Statement(lvalue.WritePreCalc + rvalue.Read + lvalue.WritePostCalc);
        }

        public static IStatement AssignUnsafe(this IWValue lvalue, IRValue rvalue)
        {
            if (!lvalue.ValueType.IsILAssignableFrom(rvalue.ValueType))
                throw new InvalidOperationException("Type mismatch in assignment. Expected " + lvalue.ValueType + ", but got " + rvalue.ValueType);

            return new Statement(lvalue.WritePreCalc + rvalue.Read + lvalue.WritePostCalc);
        }

        public static IRValue<int> Add(this IRValue<int> first, IRValue<int> addTo)
        {
            return new RValue<int>(first.Read + addTo.Read + ILSeq.Add(first.ValueType));
        }

        public static IStatement Ret(this IRValue value)
        {
            return new Statement(value.Read + ILSeq.Ret(value.ValueType));
        }

        public static IStatement Throw<T>(this IRValue<T> inst)
            where T : Exception
        {
            if (!inst.ValueType.IsClass || !typeof(Exception).IsAssignableFrom(inst.ValueType))
                throw new InvalidOperationException("Can only throw an non-exception type");
            return new Statement(inst.Read + ILSeq.Throw);
        }

        /// <summary>
        /// Gets a field or property with the given name
        /// </summary>
        /// <param name="inst"></param>
        /// <param name="name"></param>
        /// <returns>A generic IRValue, IWValue or IRWValue referencing the field/></returns>
        private static IValue Field(this IRValue inst, string name)
        {
            Type instType = inst.ValueType;
            FieldInfo field;
            // Try without inheritance first
            field = instType.GetField(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
                field = instType.GetField(name);
            if (field != null)
            {
                return (IValue)Activator.CreateInstance(typeof(RWValue<>).MakeGenericType(new Type[] { field.FieldType }),
                    inst.Read + ILSeq.Ldfld(field), inst.Read, ILSeq.Stfld(field));
            }

            PropertyInfo prop;
            // Try without inheritance first
            prop = instType.GetProperty(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
            if (prop == null)
                prop = instType.GetProperty(name);

            if (prop != null)
            {
                var getter = prop.GetGetMethod();
                var setter = prop.GetSetMethod();
                ILSeq set = null;
                ILSeq get = null;
                if (getter != null)
                    get = (instType.IsAbstract || instType.IsInterface) ? ILSeq.Callvirt(getter) : ILSeq.Call(getter);
                if (setter != null)
                    set = (instType.IsAbstract || instType.IsInterface) ? ILSeq.Callvirt(setter) : ILSeq.Call(setter);

                if (getter != null && setter != null)
                {
                    return (IValue)Activator.CreateInstance(typeof(RWValue<>).MakeGenericType(new Type[] { prop.PropertyType }),
                        inst.Read + get, inst.Read, set);
                }
                else if (getter != null)
                {
                    return (IValue)Activator.CreateInstance(typeof(RValue<>).MakeGenericType(new Type[] { prop.PropertyType }),
                        inst.Read + get);
                }
                else if (setter != null)
                {
                    return (IValue)Activator.CreateInstance(typeof(WValue<>).MakeGenericType(new Type[] { prop.PropertyType }),
                        inst.Read, set);
                }
                else
                    throw new InvalidOperationException("Cannot find getter or setter of " + prop.Name);
            }

            throw new InvalidOperationException("Cannot find field or property \"" + inst.ValueType.Name + "." + name + "\"");
        }

        public static IRValue RField(this IRValue inst, string name)
        {
            var field = inst.Field(name);
            if (!(field is IRValue))
                throw new InvalidOperationException("Field \"" + name + "\" is not readable");
            return (IRValue)field;
        }

        public static IRValue<T> RField<T>(this IRValue inst, string name)
        {
            var field = inst.Field(name);
            if (!(field is IRValue<T>))
                throw new InvalidOperationException("Field \"" + name + "\" is not readable");
            return (IRValue<T>)field;
        }

        public static IWValue WField(this IRValue inst, string name)
        {
            var field = inst.Field(name);
            if (!(field is IWValue))
                throw new InvalidOperationException("Field \"" + name + "\" is not writable");
            return (IWValue)field;
        }

        public static IWValue<T> WField<T>(this IRValue inst, string name)
        {
            var field = inst.Field(name);
            if (!(field is IWValue<T>))
                throw new InvalidOperationException("Field \"" + name + "\" is not writable");
            return (IWValue<T>)field;
        }

        public static IRWValue RWField(this IRValue inst, string name)
        {
            var field = inst.Field(name);
            if (!(field is IRWValue))
                throw new InvalidOperationException("Field \"" + name + "\" is not read-writable");
            return (IRWValue)field;
        }

        public static IRWValue<T> RWField<T>(this IRValue inst, string name)
        {
            var field = inst.Field(name);
            if (!(field is IRWValue<T>))
                throw new InvalidOperationException("Field \"" + name + "\" is not read-writable");
            return (IRWValue<T>)field;
        }

        /// <summary>
        /// Box type if it is not boxed
        /// </summary>
        public static IRValue<object> Box(this IRValue inst)
        {
            if (inst.ValueType.IsClass || inst.ValueType.IsInterface)
                return inst.As<object>();
            else
                return new RValue<object>(inst.Read + ILSeq.Box(inst.ValueType));
        }

        public static IRValue<object> Box<T>(this IRValue<T> inst)
            where T : struct
        {
            return new RValue<object>(inst.Read + ILSeq.Box(inst.ValueType));
        }
        
        

        public static IRValue<T> Cast<T>(this IRValue value)
        {
            return new RValue<T>(value.Read + ILSeq.Castclass(value.ValueType, typeof(T)));
        }

        public static IRValue Cast(this IRValue value, Type castTo)
        {
            return new RValue(castTo, value.Read + ILSeq.Castclass(value.ValueType, castTo));
        }

        public static IWValue Cast(this IWValue value, Type castTo)
        {
            return new WValue(castTo, value.WritePreCalc, ILSeq.Castclass(castTo, value.ValueType) + value.WritePostCalc);
        }

        public static IWValue Cast<T>(this IWValue value)
        {
            return new WValue<T>(value.WritePreCalc, ILSeq.Castclass(typeof(T), value.ValueType) + value.WritePostCalc);
        }

        public static IRWValue Cast(this IRWValue value, Type castTo)
        {
            return new RWValue(castTo, value.Read + ILSeq.Castclass(value.ValueType, castTo), value.WritePreCalc, ILSeq.Castclass(castTo, value.ValueType) + value.WritePostCalc);
        }

        public static IRWValue<T> Cast<T>(this IRWValue value)
        {
            return new RWValue<T>(value.Read + ILSeq.Castclass(value.ValueType, typeof(T)), value.WritePreCalc, ILSeq.Castclass(typeof(T), value.ValueType) + value.WritePostCalc);
        }

        /// <summary>
        /// Dictates the type of a value without a cast.
        /// </summary>
        public static IRValue<T> As<T>(this IRValue inst)
        {
            if (inst is IRValue<T>)
                return (IRValue<T>)inst;
            if (!typeof(T).IsILAssignableFrom(inst.ValueType))
                throw new InvalidOperationException("Cannot view \"" + inst.ValueType + "\" as a \"" + typeof(T) + "\"");
            return new RValue<T>(inst.Read);
        }

        /// <summary>
        /// Get an element from an array
        /// </summary>
        public static IRWValue<T> Elem<T>(this IRValue<IEnumerable<T>> arr, IRValue<int> index)
        {
            return new RWValue<T>(arr.Read + index.Read + ILSeq.Ldelem(typeof(T)),
                arr.Read + index.Read, ILSeq.Stelem(typeof(T)));
        }

        public static IRValue<bool> Not(this IRValue<bool> value)
        {
            return new RValue<bool>(value.As<int>().Read + ILSeq.Ldc_I4(0) + ILSeq.Ceq());
        }

        public static IRValue<bool> NotNull(this IRValue value)
        {
            // Check that it's a reference type
            if (!typeof(object).IsAssignableFrom(value.ValueType))
                throw new InvalidOperationException("Type " + value.ValueType + " must be reference type");
            return new RValue<bool>(value.Read + ILSeq.Ldc_I4(0) + ILSeq.Ceq());

        }
    }
}
