using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace EasyIL
{
    public class NewAssembly
    {
        private AssemblyBuilder assemblyBuilder;
        private ModuleBuilder moduleBuilder;

        public NewAssembly(string assemblyName, string moduleName)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            this.assemblyBuilder = currentDomain.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndSave);
            this.moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName, moduleName + ".dll");
        }

        public TypeBuilder DefineClass(string className, Type parent, params Type[] interfaceTypes)
        {
            var typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Class | TypeAttributes.Public, parent);
            foreach (var interfaceType in interfaceTypes)
                typeBuilder.AddInterfaceImplementation(interfaceType);

            return typeBuilder;
        }
    }
}
