using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace EasyIL
{
    public static class TypeBuilderEx
    {
        public static MethodBuilder ImplementMethod<TInterface>(this TypeBuilder typeBuilder, string methodName)
        {
            MethodInfo interfaceMethod = typeof(TInterface).GetMethod(methodName);
            if (interfaceMethod == null)
                throw new InvalidOperationException(String.Format("Cannot find method {0}.{1}", typeof(TInterface).Name, methodName));

            var interfaceParameters = interfaceMethod.GetParameters();

            MethodBuilder methodBuilder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final,
                interfaceMethod.ReturnType,
                interfaceParameters.Select(p => p.ParameterType).ToArray());

            for (int i = 0; i < interfaceParameters.Length; i++)
                methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, interfaceParameters[i].Name);

            return methodBuilder;
        }

        public static ConstructorBuilder DefineConstructor(this TypeBuilder typeBuilder,
            Type[] parameterTypes, string[] parameterNames)
        {
            if (parameterNames.Length != parameterNames.Length)
                throw new InvalidOperationException("Parameter types must match number of names");

            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, 
                CallingConventions.Standard,
                parameterTypes);

            for (int i = 0; i < parameterNames.Length; i++)
                constructorBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameterNames[i]);

            return constructorBuilder;
        }
    }
}
