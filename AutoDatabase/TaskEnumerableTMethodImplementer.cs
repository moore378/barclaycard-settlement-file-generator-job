using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using EasyIL;

namespace AutoDatabase
{
    /// <summary>
    /// Implements database methods that return Task of IEnumerable of T
    /// </summary>
    class TaskEnumerableTMethodImplementer : MethodImplementer
    {
        protected override IRValue GenerateResultObj(Type taskInnerType, IRValue reader, DatabaseReturnAttribute returnFieldAttr, DebugContext context)
        {
            Type recordType = taskInnerType.GetGenericArguments()[0];
            Type listType = typeof(List<>).MakeGenericType(recordType);

            var list = il.Var(listType);
            var record = il.Var(recordType);

            // Create list
            il.Emit(list.AssignUnsafe(IL.NewObject(listType)));

            // Begin loop
            var loopBegin = il.DefineLabel();
            var loopEnd = il.DefineLabel();
            il.MarkLabel(loopBegin);
            {
                //il.Emit(IL.CallStaticNoReturn(typeof(TempDebug.TempDebug), "TestMethod", reader, IL.Str(context.ToString())));
                //il.Emit(IL.CallStaticNoReturn(typeof(Debug), "WriteLine", IL.Str((context + "read").ToString())));
                //var temp = IL.CallStaticNoReturn(typeof(Debug), "WriteLine", IL.Str((context + "read_done").ToString())).Concat(IL.Goto(loopEnd));
                // C#: if (!reader.Read()) break;
                il.Emit(il.IfThen(reader.CallWithReturn<bool>("Read").Not(),
                    IL.Goto(loopEnd)));

                // Create the record for the row
                if (recordType.IsClass && recordType != typeof(string))
                    il.Emit(record.AssignUnsafe(IL.NewObject(recordType)));

                // Read this row into the record
                ReadRowIntoObject(reader, record, returnFieldAttr, context);

                // Add the record to the list
                il.Emit(list.CallIgnoreReturn("Add", record));

                // Loop back for the next record
                il.Emit(IL.Goto(loopBegin));
            }
            il.MarkLabel(loopEnd);

            return list.Cast(taskInnerType);
        }

    }
}
