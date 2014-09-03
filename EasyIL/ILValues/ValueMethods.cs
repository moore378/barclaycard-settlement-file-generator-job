using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EasyIL
{
    public static class ValueMethods
    {
        public static IRValue<T> CallWithReturn<T>(this IRValue inst, string methodName, params IRValue[] args)
        {
            MethodInfo method = GetMethodFromClass(inst.ValueType, methodName, args, true);
            if (inst.ValueType.IsAbstract || inst.ValueType.IsInterface)
                return new RValue<T>(inst.Read + args.Select(a => a.Read) + ILSeq.Callvirt(method));
            else
                return new RValue<T>(inst.Read + args.Select(a => a.Read) + ILSeq.Call(method));
        }

        public static IRValue<T> CallVirtWithReturn<T>(this IRValue inst, string methodName, params IRValue[] args)
        {
            MethodInfo method = GetMethodFromClass(inst.ValueType, methodName, args, true);
            return new RValue<T>(inst.Read + args.Select(a => a.Read) + ILSeq.Callvirt(method));
        }

        public static RValue CallWithReturn(this IRValue inst, string methodName, params IRValue[] args)
        {
            MethodInfo method = GetMethodFromClass(inst.ValueType, methodName, args, true);
            if (inst.ValueType.IsAbstract || inst.ValueType.IsInterface)
                return new RValue(method.ReturnType, inst.Read + args.Select(a => a.Read) + ILSeq.Callvirt(method));
            else
                return new RValue(method.ReturnType, inst.Read + args.Select(a => a.Read) + ILSeq.Call(method));
        }

        private static MethodInfo GetMethodFromClass(Type instType, string methodName, IRValue[] args, bool hasReturn)
        {
            if (!instType.IsClass && !instType.IsInterface)
                throw new NotSupportedException("Can currently only make calls to class or interface types");
            var argTypes = args.Select(a => a.ValueType).ToArray();
            MethodInfo method = instType.GetMethod(methodName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public, null, argTypes, null);

            // Could be inherited
            if (method == null)
                method = instType.GetMethod(methodName, BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public, null, argTypes, null);

            // Could be protected
            if (method == null)
                method = instType.GetMethod(methodName, BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.NonPublic, null, argTypes, null);

            if (method == null)
                throw new InvalidOperationException(String.Format(
                    "Cannot find method {0}.{1}({2})",
                    instType.Name,
                    methodName,
                    argTypes.Select(a => a.Name).JoinStr(", ")));

            if ((method.ReturnType != typeof(void)) != hasReturn)
                throw new InvalidOperationException(String.Format(
                    "Method returns {0}, but {1} expected: {2}.{3}({4})",
                    method.ReturnType,
                    hasReturn ? "a type" : "void",
                    instType.Name,
                    methodName,
                    argTypes.Select(a => a.Name).Aggregate("", (a, s) => a + ", " + s)));

            return method;
        }

        public static ILSeq CallIgnoreReturn(this IRValue inst, string methodName, params IRValue[] args)
        {
            Type instType = inst.ValueType;
            var argTypes = args.Select(a => a.ValueType).ToArray();
            MethodInfo method = instType.GetMethod(methodName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public, null, argTypes, null);

            // Could be inherited
            if (method == null)
                method = instType.GetMethod(methodName, BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public, null, argTypes, null);

            // Could be protected
            if (method == null)
                method = instType.GetMethod(methodName, BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.NonPublic, null, argTypes, null);

            if (method == null)
                throw new InvalidOperationException(String.Format(
                    "Cannot find method {0}.{1}({2})",
                    instType.Name,
                    methodName,
                    argTypes.Select(a => a.Name).JoinStr(", ")));

            var callSeq = (inst.ValueType.IsInterface || inst.ValueType.IsAbstract) ? ILSeq.Callvirt(method) : ILSeq.Call(method);

            if (method.ReturnType == typeof(void))
                return inst.Read + args.Select(a => a.Read) + callSeq;
            else
                return inst.Read + args.Select(a => a.Read) + callSeq + ILSeq.Pop(method.ReturnType);
        }

        public static ILSeq CallIgnoreReturn(this IRValue inst, string methodName, BindingFlags bindingFlags, params IRValue[] args)
        {
            Type instType = inst.ValueType;
            var argTypes = args.Select(a => a.ValueType).ToArray();
            MethodInfo method = instType.GetMethod(methodName, bindingFlags, null, argTypes, null);

            if (method == null)
                throw new InvalidOperationException(String.Format(
                    "Cannot find method {0}.{1}({2})",
                    instType.Name,
                    methodName,
                    argTypes.Select(a => a.Name).Aggregate("", (a, s) => a + ", " + s)));

            if (method.ReturnType == typeof(void))
                return inst.Read + args.Select(a => a.Read) + ILSeq.Call(method);
            else
                return inst.Read + args.Select(a => a.Read) + ILSeq.Call(method) + ILSeq.Pop(method.ReturnType);
        }

    }
}
