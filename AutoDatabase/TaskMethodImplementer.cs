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
    /// Implements database methods of that return Task (non generic)
    /// </summary>
    class TaskMethodImplementer : MethodImplementer
    {
        protected override IRValue GenerateResultObj(Type taskInnerType, IRValue reader, DatabaseReturnAttribute returnFieldAttr, DebugContext context)
        {
            // No fields, so we just return a new object
            var taskResult = il.Var(taskInnerType);
            il.Emit(taskResult.AssignUnsafe(IL.NewObject(typeof(object))));
            return taskResult;
        }
    }
}
