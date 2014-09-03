using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace AutoDatabase
{
    public class AutoDatabaseAssembly
    {
        private static AutoDatabaseAssembly defaultInst = new AutoDatabaseAssembly("AutoDatabaseAssembly");
        public AssemblyBuilder assemblyBuilder;
        private ModuleBuilder moduleBuilder;

        public ModuleBuilder Module { get { return moduleBuilder; } }
        public static AutoDatabaseAssembly Default { get { return defaultInst; } }

        public AutoDatabaseAssembly(string assemblyName, bool allowSave = false)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            this.assemblyBuilder = currentDomain.DefineDynamicAssembly(new AssemblyName(assemblyName), allowSave ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run);
            if (allowSave)
                this.moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName, assemblyName + ".dll");
            else
                this.moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName);
            this.AllowSave = allowSave;
            this.FileName = assemblyName + ".dll";
        }

        public bool AllowSave { get; private set; }

        public string FileName { get; private set; }
    }
}
