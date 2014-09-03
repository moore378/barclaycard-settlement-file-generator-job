using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace EasyIL
{
    public abstract class ILSeq
    {
        public static readonly ILSeq Empty = new CustomSequence(il => { }, "", null);
        
        public int StackDelta { get { return Pushes.Count() - Pops.Count(); } }

        /// <summary>
        /// What it takes off the stack before it starts, in order of pop
        /// </summary>
        public abstract IEnumerable<Type> Pops { get; }

        public abstract string StatementStr { get; }

        /// <summary>
        /// What it pushes onto the stack when it finishes, in order of push
        /// </summary>
        public abstract IEnumerable<Type> Pushes { get; }

        protected abstract void Generate(ILGenerator ilGenerator);

        public override string ToString()
        {
            return StatementStr;
        }

        /// <summary>
        /// Checks that the sequence is a statement (no net pushes/pops) and generates the IL
        /// </summary>
        /// <param name="ilGenerator"></param>
        public void GenerateStatement(ILGenerator ilGenerator)
        {
            if (Pops.Count() != 0 || Pushes.Count() != 0)
                throw new InvalidOperationException("IL does not form a statement. It pops [" + Pops.Select(p => p.Name).Aggregate("", (a, s) => a + s) + "] and pushes [" + Pushes.Select(p => p.Name).Aggregate("", (a, s) => a + s) + "]");
            Generate(ilGenerator);
        }

        public void GenerateNonStatement(ILGenerator ilGenerator)
        {
            Generate(ilGenerator);
        }

        // Wild-card type for sequences
        public struct AnyType { }
        // Summy type to represent the beginning of the stack
        public struct BottomOfStack { }
        
        /// <summary>
        /// Concatenate 2 sequences of IL
        /// </summary>
        public static ILSeq operator + (ILSeq seq1, ILSeq seq2)
        {
            // Cancel the stack operations that seq1 pushes which seq2 pops
            Queue<Type> pushes1 = new Queue<Type>(seq1.Pushes.Reverse()); // In top-first order
            Queue<Type> pops2 = new Queue<Type>(seq2.Pops); // In top-first order
            while (pushes1.Count > 0 && pops2.Count > 0)
            {
                var pop = pops2.Dequeue();
                var push = pushes1.Peek();
                if (push != typeof(BottomOfStack)) // Beginning of stack is "sticky"
                    pushes1.Dequeue();

                if (pop != typeof(ILSeq.AnyType))
                {
                    if (!pop.IsILAssignableFrom(push))
                        throw new InvalidOperationException("IL sequence stack mismatch. Expected " + pop + " but got " + push);
                }
            }

            // The stack operations that dont cancel have an effect beyond these 2 sequences
            var newPops = seq1.Pops.Concat(pops2).ToArray(); // Top-first order (chronological)
            var newPushes = pushes1.Reverse().Concat(seq2.Pushes).ToArray(); // Bottom-first order (chronological)

            return new CustomSequence(il => { seq1.Generate(il); seq2.Generate(il); }, seq1.StatementStr + "; " + seq2.StatementStr, newPops, newPushes);
        }

        public static ILSeq operator +(ILSeq seq1, IEnumerable<ILSeq> seqs2)
        {
            return seqs2.Aggregate(seq1, (a, s) => a + s);
        }

        public static ILSeq operator +(IEnumerable<ILSeq> seqs1, ILSeq seq2)
        {
            if (seqs1.Count() == 0)
                return seq2;
            else
                return seqs1.Aggregate((a, s) => a + s) + seq2;
        }

        public static ILSeq Compound(params ILSeq[] seqs)
        {
            if (seqs.Length == 0)
                return Empty;
            else
                return seqs.Aggregate((a, s) => a + s);
        }

        public static ILSeq Throw = new Consumes(typeof(Exception), il => il.Emit(OpCodes.Throw), "Throw");

        public static ILSeq Rethrow = new Consumes(typeof(Exception), il => il.Emit(OpCodes.Rethrow), "Rethrow");

        public static ILSeq Add(Type paramTypes)
        {
            if (paramTypes == typeof(int))
                return new CustomSequence(il => il.Emit(OpCodes.Add), "Add", new Type[] { paramTypes, paramTypes }, paramTypes);

            throw new NotSupportedException("Addition of type " + paramTypes.Name + " is not supported");
        }

        public static ILSeq BeginCatchBlock(Type exceptionType)
        {
            return new CustomSequence(
                il => il.BeginCatchBlock(exceptionType),
                "try",
                new Type[] { typeof(BottomOfStack) },
                exceptionType);
        }

        public static readonly ILSeq EndExceptionBlock = new CustomSequence(
            il => il.EndExceptionBlock(),
            "<end>",
            new Type[] { typeof(BottomOfStack) });

        public static ILSeq Ldc_I4(int i)
        {
            return new Returns(typeof(int), il => il.Emit(OpCodes.Ldc_I4, i), "Ldc_I4 " + i);
        }

        public static ILSeq Ret(Type returnType)
        {
            return new Consumes(returnType, il => il.Emit(OpCodes.Ret), "Ret");
        }

        public static readonly ILSeq CheckBottomOfStack = new Consumes(typeof(BottomOfStack), il => { }, "<bottom of stack>");

        /// <summary>
        /// Return from a void method. If you use this to return, you 
        /// should start your method with BeginMethod.
        /// </summary>
        public static ILSeq Ret()
        {
            return new Consumes(typeof(BottomOfStack), il => il.Emit(OpCodes.Ret), "Ret");
        }

        public static ILSeq Ldelem(Type elementType)
        {
            return new CustomSequence(
                il => il.Emit(OpCodes.Ldelem, elementType),
                "Ldelem " + elementType,
                new Type[] { typeof(int), elementType.MakeArrayType() },
                elementType);
        }


        public static ILSeq Stelem(Type elementType)
        {
            return new CustomSequence(
                il => il.Emit(OpCodes.Stelem, elementType),
                "Stelem " + elementType,
                new Type[] { elementType, typeof(int), elementType.MakeArrayType()});
        }

        public static ILSeq Ldloc(LocalBuilder local)
        {
            return new CustomSequence(
                il => il.Emit(OpCodes.Ldloc, local),
                "Ldloc [" + local.LocalType.Name + "]",
                null,
                local.LocalType);
        }

        public static ILSeq Stloc(LocalBuilder local)
        {
            return new CustomSequence(
                il => il.Emit(OpCodes.Stloc, local),
                "Stloc [" + local.LocalType.Name + "]",
                new Type[] { local.LocalType });
        }

        public static ILSeq Call(System.Reflection.MethodInfo methodInfo)
        {
            IEnumerable<Type> inst = methodInfo.IsStatic ? Enumerable.Empty<Type>() : EnumerableEx.Only(methodInfo.DeclaringType);
            IEnumerable<Type> ret = methodInfo.ReturnType == typeof(void) ? Enumerable.Empty<Type>() : EnumerableEx.Only(methodInfo.ReturnType);

            return new CustomSequence(
                il => il.Emit(OpCodes.Call, methodInfo),
                "Call " + methodInfo,
                inst.Concat(methodInfo.GetParameters().Select(p => p.ParameterType)).Reverse().ToArray(),
                ret.ToArray());
        }

        public static ILSeq Ldlen<T>()
        {
            return new CustomSequence(
                il => il.Emit(OpCodes.Ldlen),
                "Ldlen",
                new Type[] { typeof(T[]) },
                typeof(int));
        }

        public static ILSeq Box(Type valueType)
        {
            return new CustomSequence(
                il => il.Emit(OpCodes.Box, valueType),
                "Box",
                new Type[] { valueType },
                typeof(object));
        }

        public static ILSeq Ldstr(string str)
        {
            return new Returns(typeof(string), il => il.Emit(OpCodes.Ldstr, str), "Ldstr \"" + str + "\"");
        }

        /// <summary>
        /// The NewObj opcode
        /// </summary>
        /// <param name="constructor"></param>
        /// <returns></returns>
        public static ILSeq NewObj(ConstructorInfo constructor)
        {
            // Pops the arguments in reverse order of push
            var paramTypes = constructor.GetParameters().Select(p => p.ParameterType).Reverse().ToArray();
            return new CustomSequence(
                il => il.Emit(OpCodes.Newobj, constructor),
                "Newobj " + constructor,
                paramTypes,
                constructor.DeclaringType);
        }

        public static ILSeq Ldfld(FieldInfo field)
        {
            return new CustomSequence(
                il => il.Emit(OpCodes.Ldfld, field),
                "Ldfld " + field,
                new Type[] { field.DeclaringType },
                field.FieldType);
        }

        public static ILSeq Stfld(FieldInfo field)
        {
            return new CustomSequence(
                il => il.Emit(OpCodes.Stfld, field),
                "Stfld " + field,
                new Type[] { field.FieldType, field.DeclaringType});
        }

        public static ILSeq Pop(Type type)
        {
            return new Consumes(type, il => il.Emit(OpCodes.Pop), "Pop");
        }

        public static ILSeq Ldarg(Type argType, int index)
        {
            return new Returns(argType, il => il.Emit(OpCodes.Ldarg, index), "Ldarg " + index);
        }

        public static ILSeq Ldarg_0(Type instType)
        {
            return new Returns(instType, il => il.Emit(OpCodes.Ldarg_0), "Ldarg_0");
        }

        public static ILSeq Castclass(Type castFrom, Type castTo)
        {
            return new CustomSequence(il => il.Emit(OpCodes.Castclass, castTo), "Castclass " + castTo, new Type[] { castFrom }, castTo);
        }

        /// <summary>
        /// Newarr opcode
        /// </summary>
        public static ILSeq Newarr(Type elementType)
        {
            return new CustomSequence(
                il => il.Emit(OpCodes.Newarr, elementType),
                "Newarr " + elementType,
                new Type[] { typeof(int) },
                elementType.MakeArrayType());
        }

        internal static ILSeq Brtrue_S(Label lbl)
        {
            return new CustomSequence(
                il => il.Emit(OpCodes.Brtrue_S, lbl),
                "Brtrue_S",
                new Type[] { typeof(bool) }
                );
        }

        public static ILSeq Brfalse_S(Label lbl)
        {
            return new CustomSequence(
                il => il.Emit(OpCodes.Brfalse_S, lbl),
                "Brfalse_S",
                new Type[] { typeof(bool) }
                );
        }

        public static ILSeq MarkLabel(Label lbl)
        {
            return new CustomSequence(
                il => il.MarkLabel(lbl),
                "label " + lbl + ":",
                new Type[] { }
                );
        }

        public static ILSeq Br_S(Label lbl)
        {
            return new CustomSequence(
                il => il.Emit(OpCodes.Br_S, lbl),
                "br_S " + lbl,
                new Type[] { }
                );
        }

        public static ILSeq Callvirt(MethodInfo methodInfo)
        {
            IEnumerable<Type> inst = methodInfo.IsStatic ? Enumerable.Empty<Type>() : EnumerableEx.Only(methodInfo.DeclaringType);
            IEnumerable<Type> ret = methodInfo.ReturnType == typeof(void) ? Enumerable.Empty<Type>() : EnumerableEx.Only(methodInfo.ReturnType);

            return new CustomSequence(
                il => il.Emit(OpCodes.Callvirt, methodInfo),
                "Callvert " + methodInfo,
                inst.Concat(methodInfo.GetParameters().Select(p => p.ParameterType)).Reverse().ToArray(),
                ret.ToArray());
        }

        public static ILSeq Ceq()
        {
            return new CustomSequence(
                il => il.Emit(OpCodes.Ceq),
                "ceq",
                new [] { typeof(Word32), typeof(Word32) },
                typeof(bool));
        }

        internal static ILSeq Br(Label lbl)
        {
            return new CustomSequence(
                il => il.Emit(OpCodes.Br, lbl),
                "br " + lbl,
                new Type[] { }
                );
        }
    }
}
