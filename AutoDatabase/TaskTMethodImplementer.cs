using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using EasyIL;

namespace AutoDatabase
{
    /// <summary>
    /// Implements database methods that return Task of T where T is a class
    /// </summary>
    class TaskTMethodImplementer : MethodImplementer
    {
        protected override IRValue GenerateResultObj(Type taskInnerType, IRValue reader, DatabaseReturnAttribute returnFieldAttr, DebugContext context)
        {
            var taskResult = il.Var(taskInnerType);

            if (taskInnerType.IsClass && !taskInnerType.IsPrimitive && taskInnerType != typeof(string))
                il.Emit(taskResult.AssignUnsafe(IL.NewObject(taskInnerType)));

            // C#: if (!reader.HasRows) throw new InvalidOperationException("No rows in returned table");
            il.Emit(il.IfThen(reader.RField<bool>("HasRows").Not(),
                IL.ThrowNew<InvalidOperationException>(IL.Str("No rows in returned table"))));

            // C#: reader.Read();
            il.Emit(reader.CallIgnoreReturn("Read"));

            // Read fields
            ReadRowIntoObject(reader, taskResult, returnFieldAttr, context);

            return taskResult;
        }

    }
}
