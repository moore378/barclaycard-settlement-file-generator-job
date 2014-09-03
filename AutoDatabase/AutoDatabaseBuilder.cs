using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EasyIL;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SessionManager4Tests")]
namespace AutoDatabase
{
    /*
     * The auto database builder creates an implementation of a particular 
     * database interface. Implementations derive from DataAccessBase. The 
     * base class contains common methods for logging and timing. 
     * 
     * Each method in the database interface is implemented as an asynchronous
     * database stored procedure call using SqlDataReader and SqlCommand.
     * Attribute annotations on the interface, methods, and fields change
     * how the implementation is generated.
     * 
     * In general, there are 3 possible return types for database interface
     * methods: 
     * Task - There is no particular return from the database. 
     * Task<T> - There is one row in the returned table, which is mapped to type T.
     * Task<IEnumerable<T>> - The whole table is returned as an enumeration of T objects.
     * 
     * Type T can be a primative type which can be mapped to a single database 
     * type in which case only the first column of returned data will be used, 
     * or it can be a composite object or primitive types.
     * 
     */
    public class AutoDatabaseBuilder<TInterface> : AutoDatabaseBuilder
    {
        TypeBuilder typeBuilder;
        Type interfaceType;
        Type autoDatabaseType;
        static ConcurrentDictionary<Type, IStoredProcNameConverter> converters = new ConcurrentDictionary<Type, IStoredProcNameConverter>();
        
        /// <summary>
        /// DataAccessBase.QueryToken type
        /// </summary>
        static Type methodTrackerType = typeof(IDatabaseMethodTracker);

        /// <param name="assembly">The assembly to put the implementation into<param>
        public AutoDatabaseBuilder(AutoDatabaseAssembly assembly)
        {
            this.interfaceType = typeof(TInterface);
            
            // Define the type
            typeBuilder = assembly.Module.DefineType(CreateClassName(interfaceType), TypeAttributes.Class | TypeAttributes.Public);
            typeBuilder.AddInterfaceImplementation(interfaceType);
            // connectionString to keep the string for new SqlCommand objects
            var field_connectionSource = typeBuilder.DefineField("connectionSource", typeof(IConnectionSource), FieldAttributes.Private);
            var field_databaseTracker = typeBuilder.DefineField("databaseTracker", typeof(IDatabaseTracker), FieldAttributes.Private);

            // Create the constructor
            BuildConstructor(field_connectionSource, field_databaseTracker);

            // Root attribute
            var rootAttribute = (DatabaseInterfaceAttribute)interfaceType.GetCustomAttributes(typeof(DatabaseInterfaceAttribute), false).SingleOrDefault();

            var rootContext = new DebugContext(interfaceType.Name);
            // Implement methods
            foreach (var intf in EnumerateInterfaces(interfaceType))
                ImplementMethods(intf, field_connectionSource, field_databaseTracker, rootAttribute, rootContext + intf.Name);

            // Create the type. Instances are created by calling CreateInstance
            autoDatabaseType = typeBuilder.CreateType();

            if (assembly.AllowSave)
            {
                assembly.assemblyBuilder.Save(assembly.FileName);
                //System.Diagnostics.Process.Start(@"..\..\GenIL.bat");
            }
        }

        public AutoDatabaseBuilder()
            : this(AutoDatabaseAssembly.Default)
        {

        }

        /// <summary>
        /// Create an instance of the auto-database
        /// </summary>
        /// <param name="connection">The connection source to use to connect to the database. The source must produce connections of type MethodImplementer.ConnectionType.</param>
        /// <returns>A new implementation of the given interface</returns>
        public TInterface CreateInstance(IConnectionSource connectionSource, IDatabaseTracker tracker)
        {
            return (TInterface)Activator.CreateInstance(autoDatabaseType, connectionSource, tracker);
        }

        /// <summary>
        /// Given an interface, returns all the interfaces that would need to
        /// be implemented to implement this interface (all interfaces in the
        /// inheritance hierarchy)
        /// </summary>
        private IEnumerable<Type> EnumerateInterfaces(Type intf)
        {
            List<Type> result = new List<Type>();
            Queue<Type> toProcess = new Queue<Type>();
            toProcess.Enqueue(intf);
            result.Add(intf);
            while (toProcess.Count > 0)
                foreach (var inheritedIntf in toProcess.Dequeue().GetInterfaces())
                    if (!result.Contains(inheritedIntf))
                    {
                        toProcess.Enqueue(inheritedIntf);
                        result.Add(inheritedIntf);
                    }
            
            return result;
        }

        /// <summary>
        /// Builds a constructor for the auto-database
        /// </summary>
        /// <param name="field_connectionString"></param>
        private void BuildConstructor(FieldBuilder field_connectionSource, FieldBuilder field_databaseTracker)
        {
            /* 
             * The constructor just assigns the connectionString field and calls the 
             * base constructor DataAccessBase with the log argument. The constructor
             * takes 2 arguments: the connection string and the log callback, as called
             * in CreateInstance().
             */

            // Constructor
            ConstructorBuilder constructor = typeBuilder.DefineConstructor(
                new Type[] { typeof(IConnectionSource), typeof(IDatabaseTracker) },
                new string[] { "connectionSource", "databaseTracker" });
            ILGenerator il = constructor.GetILGenerator();
            // Call base()
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));
            // this.connectionSource = connectionSource;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field_connectionSource);
            // this.databaseTracker = databaseTracker;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stfld, field_databaseTracker);
            // return
            il.Emit(OpCodes.Ret);
        }
        
        /// <summary>
        /// Implement the methods of the given interface, not including its decendents.
        /// </summary>
        /// <param name="intf">The interface to implement the methods of</param>
        /// <param name="field_connectionString"></param>
        /// <param name="defaultAttr"></param>
        private void ImplementMethods(Type intf, FieldBuilder field_connectionSource, FieldBuilder field_databaseTracker, DatabaseInterfaceAttribute defaultAttr, DebugContext context)
        {
            if (!intf.IsPublic)
                throw new InvalidOperationException("Interface must be public: " + context);

            DatabaseInterfaceAttribute interfaceAttribute = interfaceType.GetCustomAttributes(typeof(DatabaseInterfaceAttribute), false).Cast<DatabaseInterfaceAttribute>().SingleOrDefault() ?? defaultAttr;

            // Implement interface
            foreach (var method in intf.GetMethods())
            {
                MethodImplementer implementer;

                // Return type is Task?
                if (method.ReturnType == typeof(Task))
                {
                    implementer = new TaskMethodImplementer();
                }
                // Return type is Task<>?
                else if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    // Extract the "T" from "Task<T>"
                    var innerType = method.ReturnType.GetGenericArguments()[0];
                    // Is the T = IEnumerable<U> ?
                    if (innerType.IsGenericType && innerType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        implementer = new TaskEnumerableTMethodImplementer();
                    else
                        implementer = new TaskTMethodImplementer();
                }
                else
                    throw new InvalidOperationException("Invalid return type for database method " + method);

                implementer.ImplementMethod(typeBuilder, method, interfaceAttribute, field_connectionSource, intf, context + method.Name, field_databaseTracker);
                
                /*var parameters = method.GetParameters();
                var returnTaskType = method.ReturnType;
                Type taskInnerType;
                Type rowObjectType;
                if (returnTaskType == typeof(Task))
                {
                    taskInnerType = null;
                    rowObjectType = null;
                }
                else if (returnTaskType.IsGenericType && returnTaskType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    taskInnerType = returnTaskType.GetGenericArguments()[0];
                    if (taskInnerType.IsGenericType && taskInnerType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        rowObjectType = taskInnerType.GetGenericArguments()[0];
                    else
                        rowObjectType = taskInnerType;
                }
                else
                    throw new NotSupportedException(String.Format("Return type of {0}.{1} must be of type Task or Task<T> and not {2}", interfaceType.Name, method.Name, method.ReturnType.Name));
                StoredProcedureAttribute storedProcAttr = method.GetCustomAttributes(typeof(StoredProcedureAttribute), true).Cast<StoredProcedureAttribute>().Single();

                Type taskCompletionSourceType;
                Type requestObjectType;
                if (taskInnerType != null)
                {
                    taskCompletionSourceType = typeof(TaskCompletionSource<>).MakeGenericType(new Type[] { taskInnerType });
                    requestObjectType = typeof(DataAccessBase).GetNestedType("RequestObject`1", BindingFlags.NonPublic).MakeGenericType(new Type[] { taskInnerType });
                }
                else
                {
                    taskCompletionSourceType = typeof(TaskCompletionSource<>).MakeGenericType(new Type[] { typeof(object) });
                    requestObjectType = typeof(DataAccessBase).GetNestedType("RequestObject`1", BindingFlags.NonPublic).MakeGenericType(new Type[] { typeof(object) });
                }

                MethodBuilder finishQuery = BuildQueryFinish(method, returnTaskType, taskInnerType, rowObjectType, requestObjectType, taskCompletionSourceType);

                BuildQueryStart(field_connectionString, queryTokenType, method, parameters, returnTaskType, taskInnerType, rowObjectType, storedProcAttr, requestObjectType, finishQuery, taskCompletionSourceType, interfaceAttribute);
                */
            }
        }

        private static void CheckRowObjectType(MethodInfo interfaceMethod, Type returnTaskType, Type rowObjectType)
        {
            if (!rowObjectType.IsClass)
                throw new NotSupportedException(interfaceMethod.Name + " cannot have " + returnTaskType.Name + " as a return type because " + rowObjectType.Name + " is not a class type");
            if (rowObjectType.GetConstructor(new Type[] { }) == null)
                throw new NotSupportedException(interfaceMethod.Name + " cannot have " + returnTaskType.Name + " as a return type because " + rowObjectType.Name + " does not have a default constructor");
        }

        private IStoredProcNameConverter ConverterFactory(Type converterType)
        {
            if (converterType.GetInterfaces().Where(t => t == typeof(IStoredProcNameConverter)).Count() == 0)
                throw new InvalidOperationException("Type " + converterType + " must implement IStoredProcNameConverter");
            return (IStoredProcNameConverter)Activator.CreateInstance(converterType);
        }

        private void LoadQueryParameter(ILGenerator il, LocalBuilder var_parameters, string sqlParamName, SqlDbType sqlParamType, Action loadRhs)
        {
            // C#: parameters.Add("@TerminalSerNo", SqlDbType.VarChar).Value = terminalSerNo;
            il.Emit(OpCodes.Ldloc, var_parameters); // stack=1
            il.Emit(OpCodes.Ldstr, sqlParamName); // stack=2
            il.Emit(OpCodes.Ldc_I4, (int)sqlParamType); // stack=3
            il.Emit(OpCodes.Call, typeof(SqlParameterCollection).GetMethod("Add", new Type[] { typeof(string), typeof(SqlDbType) })); // stack=1
            loadRhs();
            il.Emit(OpCodes.Call, typeof(SqlParameter).GetProperty("Value").GetSetMethod()); // stack=0
        }

        private static SqlDbType GetParameterDbType(MethodInfo interfaceMethod, ParameterInfo parameter, DatabaseParamAttribute attr)
        {
            if (attr != null && attr.UseDbType)
                return attr.DbType;

            SqlDbType? sqlDbType = DataEx.TypeToSqlDbType(parameter.ParameterType);
            if (!sqlDbType.HasValue)
                throw new NotSupportedException(parameter.ParameterType.ToString() + " is not a support type for parameter " + parameter.Name + " in method " + interfaceMethod.Name);

            return sqlDbType.Value;
        }

        private static string CreateClassName(Type interfaceType)
        {
            string name = interfaceType.Name;
            if (name[0] == 'I')
                name = name.Substring(1);
            name = "Auto" + name;
            return name;
        }

        [Conditional("DEBUG")]
        private static void GenWriteLine(ILGenerator il, string msg)
        {
            il.Emit(IL.CallStaticNoReturn(typeof(Console), "WriteLine", IL.Str(msg)));
        }

        protected override object CreateInternal(IConnectionSource connectionSource, IDatabaseTracker databaseTracker)
        {
            return CreateInstance(connectionSource, databaseTracker); // (TInterface)Activator.CreateInstance(autoDatabaseType, connectionSource, databaseTracker);
        }
    }

    public abstract class AutoDatabaseBuilder
    {
        static ConcurrentDictionary<Type, AutoDatabaseBuilder> builders = new ConcurrentDictionary<Type, AutoDatabaseBuilder>();

        protected abstract object CreateInternal(IConnectionSource connectionSource, IDatabaseTracker databaseTracker);

        public static TInterface CreateInstance<TInterface>(IConnectionSource connectionSource, IDatabaseTracker databaseTracker)
        {
            return (TInterface)builders.GetOrAdd(typeof(TInterface), t => builderFactory<TInterface>()).CreateInternal(connectionSource, databaseTracker);
        }

        public static TInterface CreateInstance<TInterface>(AutoDatabaseAssembly assembly, IConnectionSource connectionSource, IDatabaseTracker databaseTracker)
        {
            return (TInterface)builders.GetOrAdd(typeof(TInterface), t => builderFactory<TInterface>(assembly)).CreateInternal(connectionSource, databaseTracker);
        }

        private static AutoDatabaseBuilder builderFactory<T>()
        {
            return new AutoDatabaseBuilder<T>();
        }

        private static AutoDatabaseBuilder builderFactory<T>(AutoDatabaseAssembly assembly)
        {
            return new AutoDatabaseBuilder<T>(assembly);
        }
    }
}
