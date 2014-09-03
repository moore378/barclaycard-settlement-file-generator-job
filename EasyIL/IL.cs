using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace EasyIL
{
    /// <summary>
    /// Some high-level IL statements
    /// </summary>
    public static partial class IL
    {
        public static void Emit(this ILGenerator il, IStatement statement)
        {
            statement.Code.GenerateStatement(il);
        }

        public static void Emit(this ILGenerator il, ILSeq statement)
        {
            statement.GenerateStatement(il);
        }

        public static Statement IfThen(this ILGenerator il, IRValue<bool> predicate, IStatement statementIfTrue)
        {
            Label lbl_false = il.DefineLabel();
            return new Statement(
                    predicate.Read +
                    ILSeq.Brfalse_S(lbl_false) +
                    statementIfTrue.Code + 
                    ILSeq.MarkLabel(lbl_false));
        }

        public static Statement TryCatch(out ILStatements tryBlock, out ILStatements catchBlock, Type catchType)
        {
            var tryBlock_ = new ILStatements();
            var catchBlock_ = new ILStatements();
            tryBlock = tryBlock_;
            catchBlock = catchBlock_;

            return new Statement(new CustomSequence(il =>
                {
                    Label tryCatch = il.BeginExceptionBlock();
                    tryBlock_.Code.GenerateStatement(il);
                    il.Emit(OpCodes.Leave, tryCatch);
                    il.BeginCatchBlock(catchType);
                    var catchSeq = catchBlock_.Code;
                    if (catchSeq.Pops.Count() != 1 || catchSeq.Pops.Single() != catchType)
                        throw new InvalidOperationException("Catch block must pop exception off stack");
                    (ILSeq.BeginCatchBlock(catchType) + catchSeq).GenerateStatement(il);
                    il.Emit(OpCodes.Leave, tryCatch);
                    il.EndExceptionBlock();
                }, "Try { } catch { }", new Type[] { }));
        }

        public static ILVariable Var(this ILGenerator il, Type variableType)
        {
            LocalBuilder local = il.DeclareLocal(variableType);
            return new ILVariable(local);
        }

        public static ILVariable<T> Var<T>(this ILGenerator il)
        {
            LocalBuilder local = il.DeclareLocal(typeof(T));
            return new ILVariable<T>(local);
        }

        public static IRValue This(Type instType)
        {
            return new RValue(instType, ILSeq.Ldarg_0(instType));
        }

        public static IRValue<T> This<T>()
        {
            return new RValue<T>(ILSeq.Ldarg_0(typeof(T)));
        }

        public static IRValue<T> EnumConst<T>(T value)
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException(typeof(T).ToString() + " is not an enum");
            return new RValue<int>(ILSeq.Ldc_I4(Convert.ToInt32(value))).As<T>();
        }

        public static IRValue<int> Const(int i)
        {
            return new RValue<int>(ILSeq.Ldc_I4(i));
        }


        /// <summary>
        /// Creates a new object with the given arguments.
        /// </summary>
        /// <param name="instType"></param>
        /// <param name="args"></param>
        /// <returns>Value representing the object creation</returns>
        public static IRValue NewObject(Type instType, params IRValue[] args)
        {
            var argTypes = args.Select(a => a.ValueType).ToArray();
            ConstructorInfo constructor = instType.GetConstructor(argTypes);
            if (constructor == null)
                throw new InvalidOperationException(String.Format(
                    "Cannot find constructor {0}({1})",
                    instType.Name,
                    argTypes.Select(a => a.Name).JoinStr(",")));

            return new RValue(instType, args.Select(a => a.Read) + ILSeq.NewObj(constructor));
        }

        /// <summary>
        /// Creates a new object with the given arguments.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Value representing the object creation</returns>
        public static IRValue<T> NewObject<T>(params IRValue[] args)
        {
            var argTypes = args.Select(a => a.ValueType).ToArray();
            ConstructorInfo constructor = typeof(T).GetConstructor(argTypes);
            if (constructor == null)
                throw new InvalidOperationException(String.Format(
                    "Cannot find constructor {0}({1})",
                    typeof(T).Name,
                    argTypes.Select(a => a.Name).Aggregate("", (a, s) => a + ", " + s)));

            return new RValue<T>(args.Select(a => a.Read) + ILSeq.NewObj(constructor));
        }

        /// <summary>
        /// Argument to the current function.
        /// </summary>
        /// <param name="argType">Type of the argument (not checked)</param>
        /// <param name="index">Index of the argument - argument 0 is the "this" pointer</param>
        public static IRValue Arg(Type argType, int index)
        {
            return new RValue(argType, ILSeq.Ldarg(argType, index));
        }

        public static IRValue<T> Arg<T>(int index)
        {
            if (index == 0)
                throw new InvalidOperationException("Arg0 should be loaded using \"ValueEx.This\". Did you perhaps mean arg 1?");
            return new RValue<T>(ILSeq.Ldarg(typeof(T), index));
        }

        /// <summary>
        /// Generates an expression that creates an array
        /// </summary>
        public static IRValue NewArray(Type elementType, IRValue<int> count)
        {
            return new RValue(elementType.MakeArrayType(), count.Read + ILSeq.Newarr(elementType));
        }

        /// <summary>
        /// Generates an expression that creates an array
        /// </summary>
        public static IRValue<T[]> NewArray<T>(IRValue<int> count)
        {
            return new RValue<T[]>(count.Read + ILSeq.Newarr(typeof(T)));
        }

        /// <summary>
        /// Creates a new expression and throws it
        /// </summary>
        public static IStatement ThrowNew<T>(params IRValue[] args)
            where T : Exception
        {
            return new Statement(IL.NewObject<T>(args).Read + ILSeq.Throw);
        }

        public static IStatement CallStaticNoReturn(Type cls, string methodName, params IRValue[] args)
        {
            var argTypes = args.Select(a => a.ValueType).ToArray();
            MethodInfo method = cls.GetMethod(methodName, BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public, null, argTypes, null);

            if (method == null)
                throw new InvalidOperationException(String.Format(
                    "Cannot find static method {0}.{1}({2})",
                    cls.Name,
                    methodName,
                    argTypes.Select(a => a.Name).JoinStr(",")));

            if (method.ReturnType == typeof(void))
                return new Statement(args.Select(a => a.Read) + ILSeq.Call(method));
            else
                return new Statement(args.Select(a => a.Read) + ILSeq.Call(method) + ILSeq.Pop(method.ReturnType));
        }

        public static IRValue<T> CallStaticWithReturn<T>(Type cls, string methodName, params IRValue[] args)
        {
            var argTypes = args.Select(a => a.ValueType).ToArray();
            MethodInfo method = cls.GetMethod(methodName, BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public, null, argTypes, null);

            if (method == null)
                throw new InvalidOperationException(String.Format(
                    "Cannot find static method {0}.{1}({2})",
                    cls.Name,
                    methodName,
                    argTypes.Select(a => a.Name).Aggregate("", (a, s) => a + ", " + s)));

            if (method.ReturnType == typeof(void))
                throw new InvalidOperationException("Method does not return a value");
            if (method.ReflectedType != typeof(T))
                throw new InvalidOperationException("Method returns wrong type");

            return new RValue<T>(args.Select(a => a.Read) + ILSeq.Call(method));
        }

        public static IRValue<string> Str(string value)
        {
            return new RValue<string>(ILSeq.Ldstr(value));
        }

        public static IStatement Goto(Label lbl)
        {
            return new Statement(ILSeq.Br(lbl));
        }

        public static IRValue<string> StrConcat(params IRValue<string>[] values)
        {
            IRValue<string> result;
            int i = 0;
            if (values.Length >= 4)
            {
                result = IL.CallStaticWithReturn<string>(typeof(string), "Concat",
                    values[0], values[1], values[2], values[3]);
                i = 4;
            }
            else if (values.Length == 3)
            {
                return IL.CallStaticWithReturn<string>(typeof(string), "Concat",
                    values[0], values[1], values[2]);
            }
            else if (values.Length == 2)
            {
                return IL.CallStaticWithReturn<string>(typeof(string), "Concat",
                    values[0], values[1]);
            }
            else if (values.Length - i >= 1)
            {
                return values[0];
            }
            else
                return IL.Str("");
            
            while (values.Length - i >= 3)
            {
                result = IL.CallStaticWithReturn<string>(typeof(string), "Concat",
                    result, values[i], values[i + 1], values[i + 2]);
                i += 3;
            }

            if (values.Length - i == 2)
                return IL.CallStaticWithReturn<string>(typeof(string), "Concat",
                    result, values[i], values[i + 1]);
            else if (values.Length - i == 1)
                return IL.CallStaticWithReturn<string>(typeof(string), "Concat",
                    result, values[i]);
            else
                return result;
        }
    }
}
