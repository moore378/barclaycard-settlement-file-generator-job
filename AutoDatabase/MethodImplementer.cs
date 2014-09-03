using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using EasyIL;

namespace AutoDatabase
{
    internal abstract class MethodImplementer
    {
        // These types represent the built-in types used to send and interpret the query. These can be replaced for tests.
        public static Type SqlDataReaderType = typeof(SqlDataReader);
        public static Type SqlCommandType = typeof(SqlCommand);
        public static Type SqlConnectionType = typeof(SqlConnection);
        public static Type SqlParameterCollectionType = typeof(SqlParameterCollection);
        public static Type SqlParameterType = typeof(SqlParameter);

        /// <summary>
        /// Method parameters
        /// </summary>
        protected ParameterInfo[] parameters;
        private Type returnTaskType;
        private Type persistDataGenericType;
        private Type taskInnerType;
        private Type persistDataType;
        private TypeBuilder typeBuilder;
        private MethodInfo interfaceMethod;
        private static Type methodTrackerType = typeof(IDatabaseMethodTracker);
        protected ILGenerator il;
        private DatabaseInterfaceAttribute interfaceAttribute;
        static ConcurrentDictionary<Type, IStoredProcNameConverter> converters = new ConcurrentDictionary<Type, IStoredProcNameConverter>();
        private FieldInfo field_connectionSource;
        private FieldInfo field_databaseTracker;
        private static bool includeReturnParameter = false;
        private Type taskCompletionSourceType;
        private Type interfaceType;

        /// <summary>
        /// Implement a database interface method
        /// </summary>
        /// <param name="inst">The object class to implement the method into</param>
        /// <param name="method">The interface method to implement</param>
        public void ImplementMethod(TypeBuilder inst, MethodInfo method, DatabaseInterfaceAttribute interfaceAttr, FieldInfo field_connectionSource, Type interfaceType, DebugContext context, FieldInfo field_databaseTracker)
        {
            this.interfaceType = interfaceType;
            this.field_connectionSource = field_connectionSource;
            this.field_databaseTracker = field_databaseTracker;
            this.interfaceAttribute = interfaceAttr;
            this.typeBuilder = inst;
            this.interfaceMethod = method;
            this.parameters = method.GetParameters();
            this.returnTaskType = method.ReturnType;
            this.persistDataGenericType = typeof(PersistData<,,>);
            // The "T" in "Task<T>", or "object" if Task is not generic
            this.taskInnerType = (returnTaskType.IsGenericType) ? returnTaskType.GetGenericArguments()[0] : typeof(object);
            // Type for persistent data between query start and return
            this.persistDataType = persistDataGenericType.MakeGenericType(taskInnerType, SqlCommandType, SqlConnectionType);
            // Type for task completion source
            this.taskCompletionSourceType = typeof(TaskCompletionSource<>).MakeGenericType(new Type[] { taskInnerType });

            if (taskInnerType.IsValueType && !taskInnerType.IsPrimitive && taskInnerType != typeof(decimal))
                throw new InvalidOperationException("Cannot have a struct return type: " + (context + taskInnerType.Name));

            /* Each interface method is implemented in 2 parts. The one part
             * starts the asynchronous database operation and returns the task
             * representing the operation, and the second part is the callback
             * for when the operation is complete. The first part needs to 
             * take all the arguments to the function and load them into the 
             * stored procedure arguments. The second part needs to create the
             * resulting structures by reading the returned table.
             */

            // Create the end-Query function
            MethodBuilder endQuery = inst.DefineMethod("End_" + method.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final, null, new Type[] { typeof(IAsyncResult) });
            this.il = endQuery.GetILGenerator();

            // The end-query method takes one parameter
            endQuery.DefineParameter(1, ParameterAttributes.None, "asyncResult");
            var asyncResult = IL.Arg<IAsyncResult>(1);

            BuildQueryEnd(asyncResult, method.ReturnTypeCustomAttributes.SingleAttribute<DatabaseReturnAttribute>(), context + "end");
            BuildQueryBegin(inst, endQuery, context);
        }

        protected abstract IRValue GenerateResultObj(Type taskInnerType, IRValue reader, DatabaseReturnAttribute returnFieldAttr, DebugContext context);

        private void BuildQueryBegin(TypeBuilder inst, MethodBuilder endQueryMethod, DebugContext context)
        {
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(interfaceMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final, returnTaskType, parameters.Select(p => p.ParameterType).ToArray());
            for (int i = 0; i < parameters.Length; i++)
                methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameters[i].Name);

            this.il = methodBuilder.GetILGenerator();

            var methodTracker = GenerateMethodTracker();

            var connection = il.Var(SqlConnectionType);
            var result = il.Var(returnTaskType);
                        
            // C#: try
            Label tryCatch = il.BeginExceptionBlock();
            {
                // Decide what procedure to call
                string storedProcName = GetStoredProcName();

                // Establish a connection
                IRValue connectionSource = new RValue<IConnectionSource>(new CustomSequence(
                    i => { i.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, field_connectionSource); },
                    "Ldarg_0; Ldfld connectionSource",
                    new Type[] { }, typeof(IConnectionSource)));
                
                il.Emit(connection.AssignUnsafe(connectionSource.CallWithReturn("GetConnection").Cast(SqlConnectionType)));

                // Create a new command object
                var command = il.Var(SqlCommandType);
                var commandText = IL.Str(storedProcName);
                il.Emit(command.AssignUnsafe(IL.NewObject(SqlCommandType, commandText, connection)));

                // Set up command object
                il.Emit(command.WField<CommandType>("CommandType").Assign(IL.EnumConst(CommandType.StoredProcedure)));

                // Get parameter collection from command so we can add parameters
                var sqlParameters = il.Var(SqlParameterCollectionType);
                il.Emit(sqlParameters.AssignUnsafe(command.RField("Parameters")));

                // Add return parameter
                if (includeReturnParameter)
                    il.Emit(sqlParameters.CallIgnoreReturn("Add", IL.Str("@RETURN_VALUE"), IL.EnumConst(SqlDbType.Int)));

                // Populate other arguments
                PopulateQueryArguments(sqlParameters, context);

                // Set up persistent data
                var persistData = il.Var(persistDataType);
                il.Emit(persistData.AssignUnsafe(IL.NewObject(persistDataType)));
                il.Emit(persistData.WField("MethodTracker").AssignUnsafe(methodTracker));
                il.Emit(persistData.WField("Command").AssignUnsafe(command));
                il.Emit(persistData.WField("Connection").AssignUnsafe(connection));
                il.Emit(persistData.WField("TaskCompletionSource").AssignUnsafe(IL.NewObject(taskCompletionSourceType)));

                // Open connection
                il.Emit(connection.CallIgnoreReturn("Open"));

                // C#: command.BeginExecuteReader(EndSelTerminalCommsParameters, persistData, CommandBehavior.CloseConnection | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
                RValue ftnEndQuery = new RValue(typeof(AsyncCallback), _ =>
                    {
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldftn, endQueryMethod);
                        il.Emit(OpCodes.Newobj, typeof(AsyncCallback).GetConstructor(new Type[] { typeof(object), typeof(IntPtr) }));
                    },
                    "Ldarg_0; Ldftn <finishQuery>, Newobj <AsyncCallback>; ");

                CommandBehavior flags = CommandBehavior.CloseConnection | CommandBehavior.SingleResult;
                // If the return type is not enumerable, then we can optimize for a single row return
                if (!(taskInnerType.IsGenericType && taskInnerType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                    flags = flags | CommandBehavior.SingleRow;

                il.Emit(command.CallIgnoreReturn("BeginExecuteReader", ftnEndQuery, persistData.As<object>(), IL.EnumConst(flags)));

                // C#: return obj.TaskCompletionSource.Task;
                il.Emit(result.AssignUnsafe(persistData.RField("TaskCompletionSource").RField("Task")));
            }
            // C#: catch (Exception)
            il.BeginCatchBlock(typeof(Exception)); // stack=1
            {
                // C#: FailedQuery(queryToken);
                il.Emit(methodTracker.CallIgnoreReturn("Failed"));
                // Clean up connection
                il.IfThen(connection.NotNull(), new Statement(connection.CallIgnoreReturn("Dispose")));
                // C#: throw;
                il.Emit(OpCodes.Rethrow);
            }
            il.EndExceptionBlock();
            il.Emit(result.Ret());
        }

        /// <summary>
        /// Populate the given parameter collection with the arguments from the interface method arguments
        /// </summary>
        /// <param name="sqlParameters">Value of type SqlParameterCollectionType</param>
        private void PopulateQueryArguments(IRValue sqlParameters, DebugContext context)
        {
            foreach (ParameterInfo parameter in parameters)
            {
                var paramAttr = parameter.SingleAttribute<DatabaseParamAttribute>();
                PopulateQueryArgument(sqlParameters, paramAttr, IL.Arg(parameter.ParameterType, parameter.Position + 1), parameter.Name, context + parameter.Name);
            }
        }

        /// <summary>
        /// Given an argument to the interface method, write the argument to the given parameter collection
        /// </summary>
        /// <param name="sqlParameters">Collection to write the argument to, of type SqlParameterCollectionType</param>
        /// <param name="fieldAttr">Attribute of the argument</param>
        /// <param name="argument">Value representing the argument</param>
        /// <param name="context">Context in which the argument exists</param>
        private void PopulateQueryArgument(IRValue sqlParameters, DatabaseParamAttribute paramAttr, IRValue argument, string name, DebugContext context)
        {
            Type argumentType = argument.ValueType;
            var attr = argumentType.SingleAttribute<DatabaseParamGroupAttribute>();

            // Not a simple type?
            if (argumentType.IsClass && !DataEx.TypeToSqlDbType(argumentType).HasValue)
            {
                bool useAllFields = attr.Transform(a => (bool?)a.UseAllFields) ?? true;

                // For each public field
                foreach (FieldInfo field in argumentType.GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    var subFieldAttr = field.SingleAttribute<DatabaseParamAttribute>();
                    // Should the field be included?
                    bool useField = (useAllFields || (subFieldAttr != null && !subFieldAttr.Ignore));
                    if (useField)
                        PopulateQueryArgument(sqlParameters, subFieldAttr, argument.RField(field.Name), field.Name, context + field.Name);
                }

                // For each public field
                foreach (var field in argumentType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    var subFieldAttr = field.SingleAttribute<DatabaseParamAttribute>();
                    // Should the field be included?
                    bool useField = (useAllFields || (subFieldAttr != null && !subFieldAttr.Ignore));
                    if (useField)
                        PopulateQueryArgument(sqlParameters, subFieldAttr, argument.RField(field.Name), field.Name, context + field.Name);
                }
            }
            else
            {
                // Choose the database type for the field
                SqlDbType sqlDbType = GetParamDbType(argumentType, paramAttr, context);
                // Choose the SQL name that the field should be mapped to
                string sqlParamName = (paramAttr != null && !String.IsNullOrEmpty(paramAttr.SqlName)) ? paramAttr.SqlName : "@" + name;
                // Add a new parameter to the stored procedure call
                var newSqlParam = AddParameter(sqlParameters, sqlParamName, sqlDbType);
                // Assign the new parameter
                il.Emit(newSqlParam.AssignUnsafe(argument.Box()));
            }
        }

        private static SqlDbType GetParamDbType(Type fieldType, DatabaseParamAttribute attr, DebugContext context)
        {
            if (attr != null && attr.UseDbType)
                return attr.DbType;

            SqlDbType? sqlDbType = DataEx.TypeToSqlDbType(fieldType);
            if (!sqlDbType.HasValue)
                throw new NotSupportedException(fieldType.ToString() + " is not a support type for field " + context);

            return sqlDbType.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters">Parameter collection of type SqlParameterCollectionType</param>
        /// <param name="sqlParamName"></param>
        /// <param name="sqlParamType"></param>
        /// <returns></returns>
        private IWValue AddParameter(IRValue parameters, string sqlParamName, SqlDbType sqlParamType)
        {
            return parameters.CallWithReturn("Add", IL.Str(sqlParamName), IL.EnumConst(sqlParamType)).WField("Value");
        }

        private string GetStoredProcName()
        {
            StoredProcedureAttribute storedProcAttr = interfaceMethod.GetCustomAttributes(typeof(StoredProcedureAttribute), true).Cast<StoredProcedureAttribute>().SingleOrDefault();
            
            if (storedProcAttr != null && !String.IsNullOrEmpty(storedProcAttr.Name))
                return storedProcAttr.Name;
            else if (interfaceAttribute != null && interfaceAttribute.StoredProcNameConverter != null)
            {
                var converter = converters.GetOrAdd(interfaceAttribute.StoredProcNameConverter, ConverterFactory);
                return converter.ConvertToStoredProc(interfaceMethod.Name);
            }
            else
                return interfaceMethod.Name;
        }

        private IStoredProcNameConverter ConverterFactory(Type converterType)
        {
            if (converterType.GetInterfaces().Where(t => t == typeof(IStoredProcNameConverter)).Count() == 0)
                throw new InvalidOperationException("Type " + converterType + " must implement IStoredProcNameConverter");
            return (IStoredProcNameConverter)Activator.CreateInstance(converterType);
        }

        /// <summary>
        /// Calls DataAccessBase.QueuingQuery, to get a QueryToken representing the query.
        /// </summary>
        /// <returns>A value representing the new QueryToken</returns>
        private IRValue GenerateMethodTracker()
        {
            IRValue databaseTracker = new RValue<IDatabaseTracker>(new CustomSequence(
                    i => { i.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, field_databaseTracker); },
                    "Ldarg_0; Ldfld databaseTracker",
                    new Type[] { }, typeof(IDatabaseTracker)));

            var argArray = il.Var<object[]>();
            var methodTracker = il.Var(methodTrackerType);
            // C#: QueryToken queryToken = QueingQuery("SelTerminalCommsParameters", terminalSerNo);
            il.Emit(argArray.Assign(IL.NewArray<object>(IL.Const(parameters.Length))));
            for (int i = 0; i < parameters.Length; i++)
                il.Emit(argArray.Elem(IL.Const(i)).Assign(IL.Arg(parameters[i].ParameterType, i + 1).Box()));
            il.Emit(methodTracker.AssignUnsafe(databaseTracker.CallWithReturn("StartingQuery", IL.Str(interfaceMethod.Name), argArray)));
            return methodTracker;
        }

        /// <summary>
        /// Build the callback function IL to be called when the query ends
        /// </summary>
        /// <param name="asyncResult">Result parameter</param>
        /// <param name="methodFieldAttr">The DatabaseReturn attribute of the return value</param>
        /// <param name="context"></param>
        private void BuildQueryEnd(IRValue<IAsyncResult> asyncResult, DatabaseReturnAttribute returnFieldAttr, DebugContext context)
        {
            // The persist object contains any persistent data we stored at the beginning of the operation
            // The persisted data is passed back to the callback via IAsyncResult.AsyncState
            var persistData = il.Var(persistDataType);
            il.Emit(persistData.AssignUnsafe(asyncResult.RField("AsyncState").Cast(persistDataType)));

            // Begin try block so if there are any problems reading the data we can catch it before it crashes the SqlCommand
            Label tryCatch = il.BeginExceptionBlock();
            {
                // We call SqlCommand.EndExecuteReader to get a SqlDataReader
                // C#: SqlDataReader reader = requestObject.Command.EndExecuteReader(ar);
                var reader = il.Var(SqlDataReaderType);
                var sqlCommand = persistData.RField("Command");
                il.Emit(reader.AssignUnsafe(sqlCommand.CallWithReturn("EndExecuteReader", asyncResult)));

                // Create result object by reading the database
                // The specific implementation is dependent on the concrete implementation of this class
                var taskResult = GenerateResultObj(taskInnerType, reader, returnFieldAttr, context);

                // Dispose the reader
                il.Emit(reader.CallIgnoreReturn("Close"));
                il.Emit(reader.CallIgnoreReturn("Dispose"));

                IRValue<object> resultArg;
                if (taskResult.ValueType.IsClass) // No boxing required required
                    resultArg = taskResult.As<object>();
                else
                    resultArg = taskResult.Box();

                // C#: SuccessfulQuery(requestObject.QueryToken);
                il.Emit(persistData.RField("MethodTracker").CallIgnoreReturn("Successful", resultArg));

                // C#: requestObject.TaskCompletionSource.SetResult(result);
                il.Emit(persistData.RField("TaskCompletionSource").CallIgnoreReturn("SetResult", taskResult));

                // End try
                il.Emit(OpCodes.Leave, tryCatch);
            }
            // C#: catch (Exception)
            il.BeginCatchBlock(typeof(Exception));
            {
                /*
                 * When we catch an exception, we need to forward it through to the task
                 * object so that the caller can be notified. 
                 */
                var loc_exception = il.DeclareLocal(typeof(Exception));
                var var_exception = new ILVariable<Exception>(loc_exception);
                il.Emit(OpCodes.Stloc, loc_exception);

                // C#: FailedQuery(requestObject.QueryToken);
                il.Emit(persistData.RField("MethodTracker").CallIgnoreReturn("Failed"));

                // C#: requestObject.TaskCompletionSource.TrySetException(e);
                il.Emit(persistData.RField("TaskCompletionSource").CallIgnoreReturn("TrySetException", var_exception));

                // End catch
                il.Emit(OpCodes.Leave, tryCatch);
            }
            il.BeginFinallyBlock();
            {
                // Close the connection
                il.Emit(persistData.RField("Connection").CallIgnoreReturn("Dispose"));
            }
            il.EndExceptionBlock();
            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Generate code to read the current row of a SqlDataReader into a data object
        /// </summary>
        /// <param name="il"></param>
        /// <param name="reader"></param>
        /// <param name="dataObject"></param>
        protected void ReadRowIntoObject(IRValue reader, IRWValue dataObject, DatabaseReturnAttribute returnFieldAttr, DebugContext context)
        {
            // If the object is a primitive type, then we just read the first column into the object
            if (dataObject.ValueType.IsPrimitive || 
                dataObject.ValueType == typeof(string) || 
                dataObject.ValueType == typeof(decimal) || 
                IsNullableType(dataObject.ValueType) || 
                dataObject.ValueType == typeof(DateTime))
            {
                DatabaseReturnAttribute attr = returnFieldAttr;
                ReadColumnIntoField(reader, dataObject, attr, null, context + "result");
            }
            else
            {
                // C#: result.TerminalSerNo = reader.GetString(reader.GetOrdinal("TerminalSerNo"));
                foreach (var resultField in dataObject.ValueType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    DatabaseReturnAttribute attr = resultField.SingleAttribute<DatabaseReturnAttribute>();
                    if (!attr.Transform(a => a.Ignore))
                        ReadColumnIntoField(reader, dataObject.WField(resultField.Name), attr, resultField.Name, context + resultField.Name);
                }

                foreach (var resultField in dataObject.ValueType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    DatabaseReturnAttribute attr = resultField.SingleAttribute<DatabaseReturnAttribute>();
                    if (!attr.Transform(a => a.Ignore))
                        ReadColumnIntoField(reader, dataObject.WField(resultField.Name), attr, resultField.Name, context + resultField.Name);
                }
            }
        }

        private bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Reads a field from the database into a local object field
        /// </summary>
        /// <param name="reader">Reader to read from</param>
        /// <param name="field">Field to write to</param>
        /// <param name="attr">Optional attribute of field. Used to determine the name of the column</param>
        /// <param name="fieldName">If the attr is null, or has no SqlName, then the fieldName is used</param>
        /// <param name="context"></param>
        private void ReadColumnIntoField(IRValue reader, IWValue field, DatabaseReturnAttribute attr, string fieldName, DebugContext context)
        {
            string indexStr;
            IRValue<int> paramIndex;

            if (attr != null)
            {
                switch (attr.FieldSource)
                {
                    case SqlFieldSource.ByFieldName:
                        if (fieldName == null)
                            throw new InvalidOperationException("Field \"" + context + "\" does not have a field name to be used in SQL. Consider using DatabaseReturnAttribute.SqlName or ColumnIndex");
                        paramIndex = reader.CallWithReturn<int>("GetOrdinal", IL.Str(fieldName));
                        indexStr = fieldName;
                        break;
                    case SqlFieldSource.ByColumnIndex:
                        paramIndex = IL.Const(attr.ColumnIndex);
                        indexStr = "[" + attr.ColumnIndex.ToString() + "]";
                        break;
                    case SqlFieldSource.BySqlName:
                        paramIndex = reader.CallWithReturn<int>("GetOrdinal", IL.Str(attr.SqlName));
                        indexStr = attr.SqlName;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                if (String.IsNullOrEmpty(fieldName))
                    throw new InvalidOperationException("Field " + context + " does not specify a DatabaseReturn attribute, but does not have a name to be used as a default. Consider using DatabaseReturnAttribute.SqlName or ColumnIndex.");
                paramIndex = reader.CallWithReturn<int>("GetOrdinal", IL.Str(fieldName));
                indexStr = fieldName;
            }

            Label tryCatch = il.BeginExceptionBlock();
            {
                Type targetType;
                // Nullable type?
                if (field.ValueType.IsGenericType && field.ValueType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    targetType = field.ValueType.GetGenericArguments()[0];
                    MethodInfo getterMethod = GetDbGetterForField(targetType, attr, context);
                    var var_nullable = il.DeclareLocal(field.ValueType);
                    // Save the parameter index so we dont have to call GetOrdinal twice
                    IRWValue<int> var_paramIndex = il.Var<int>();
                    il.Emit(var_paramIndex.Assign(paramIndex));
                    paramIndex = var_paramIndex;

                    // Create nullable type with default value of null
                    il.Emit(OpCodes.Ldloca_S, var_nullable);
                    il.Emit(OpCodes.Initobj, field.ValueType);

                    // Read from database, or skip if null
                    var isDbNull = il.DefineLabel();
                    reader.CallWithReturn("IsDBNull", paramIndex).Read.GenerateNonStatement(il);
                    il.Emit(OpCodes.Brtrue_S, isDbNull);
                    il.Emit(OpCodes.Ldloca_S, var_nullable); // Load address of nullable
                    reader.CallWithReturn(getterMethod.Name, paramIndex).Read.GenerateNonStatement(il); // Load the value
                    il.Emit(OpCodes.Call, field.ValueType.GetConstructor(new[] { targetType })); // Call the constructor of Nullable<T>

                    il.MarkLabel(isDbNull);

                    // Assign the value to the field
                    field.WritePreCalc.GenerateNonStatement(il);
                    il.Emit(OpCodes.Ldloc, var_nullable);
                    field.WritePostCalc.GenerateNonStatement(il);
                }
                // String? - a string is a nullable type that is not from Nullable<T>
                else if (field.ValueType == typeof(string))
                {
                    MethodInfo getterMethod = GetDbGetterForField(field.ValueType, attr, context);

                    field.WritePreCalc.GenerateNonStatement(il);

                    // Default value is null
                    il.Emit(OpCodes.Ldnull);
                    
                    // Optional create
                    var isDbNull = il.DefineLabel();
                    reader.CallWithReturn("IsDBNull", paramIndex).Read.GenerateNonStatement(il);
                    il.Emit(OpCodes.Brtrue_S, isDbNull);
                    il.Emit(OpCodes.Pop); // Pop null off the stack
                    // Push actual value onto the stack
                    reader.CallWithReturn(getterMethod.Name, paramIndex).Read.GenerateNonStatement(il); // Load the value
                    il.MarkLabel(isDbNull);

                    field.WritePostCalc.GenerateNonStatement(il);
                }
                else
                {
                    targetType = field.ValueType;
                    MethodInfo getterMethod = GetDbGetterForField(targetType, attr, context);
                    il.Emit(field.AssignUnsafe(reader.CallWithReturn(getterMethod.Name, paramIndex)));
                }
                il.Emit(OpCodes.Leave, tryCatch);
            }
            il.BeginCatchBlock(typeof(Exception));
            {
                var loc_exception = il.DeclareLocal(typeof(Exception));
                var var_exception = new ILVariable<Exception>(loc_exception);
                il.Emit(OpCodes.Stloc, loc_exception);
                IRValue<string> dataTypeName = reader.CallWithReturn<string>("GetDataTypeName", paramIndex);
                IRValue<string> dataValue = reader.CallWithReturn<object>("GetProviderSpecificValue", paramIndex)
                    .CallVirtWithReturn<string>("ToString");
                IRValue<string> exceptionText = IL.StrConcat(
                    IL.Str("Failed to copy value " + indexStr + " of value type "),
                    dataTypeName,
                    IL.Str(" with value "),
                    dataValue,
                    IL.Str(" into type " + field.ValueType + " in " + context));
                il.Emit(IL.ThrowNew<AggregateException>(exceptionText, var_exception));
            }
            il.EndExceptionBlock();
        }

        /// <summary>
        /// Given a field in a target data object, choose a SqlDataReader method that extracts the corresponding field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldType"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        private static MethodInfo GetDbGetterForField(Type fieldType, DatabaseReturnAttribute attr, DebugContext context)
        {
            string getterName;

            // Note that by allowing any type for which there is a SqlDataReader.GetX(int) method,
            // we automatically cater for "nullable" types such as SqlInt64

            // Read from attribute
            if (attr != null && !String.IsNullOrEmpty(attr.SqlDataReaderGetterName)) getterName = attr.SqlDataReaderGetterName;
            // Boolean
            else if (fieldType == typeof(bool)) getterName = "GetBoolean";
            // Anything for which there is a GetX() method
            else if (SqlDataReaderType.GetMethod("Get" + fieldType.Name) != null)
                getterName = "Get" + fieldType.Name;
            else
                throw new NotSupportedException("Method cannot return field type " + fieldType.Name + " \nNo valid DatabaseReturnAttribute.SqlDataReaderGetterName was specified, and no implicit getter \"SqlDataReader.Get" + fieldType.Name + "(int)\". Context: " + context);
            MethodInfo getterMethod = SqlDataReaderType.GetMethod(getterName, new Type[] { typeof(int) });
            if (getterMethod == null)
                throw new InvalidOperationException(String.Format("There is no valid method \"{0} SqlDataReader.{1}(int)\"",
                    fieldType.Name, getterName));

            if (getterMethod.ReturnType != fieldType)
                throw new InvalidOperationException(String.Format("Cannot use SqlDataReader.{0}, because the return type is {1} not {2}",
                    getterMethod.Name, getterMethod.ReturnType, fieldType));

            return getterMethod;
        }
    }
}
