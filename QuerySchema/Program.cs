using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using QuerySchema.Properties;

namespace QuerySchema
{
    class Program
    {
        /// <summary>
        /// Given a stored procedure, it queries the parameters and return fields
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            try
            {
                if (args.Length != 1)
                    throw new ArgumentException("Invalid number of arguments to application");
                using (SqlConnection connection = new SqlConnection(Settings.Default.ConnectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandText = args[0];
                    command.CommandType = CommandType.StoredProcedure;
                    SqlCommandBuilder.DeriveParameters(command);
                    string s = command.CommandText + "(";
                    s += command.Parameters.Cast<SqlParameter>().Where(p => p.Direction == ParameterDirection.Input).Select(p => p.SqlDbType + " " + p.ParameterName).JoinStr(", ");
                    s += ")";
                    Console.WriteLine(s);

                    Console.WriteLine("Parameters:");
                    foreach (SqlParameter parameter in command.Parameters.Cast<SqlParameter>())
                    {
                        s = "\t";
                        if (parameter.Direction != ParameterDirection.Input)
                            s += "// ";
                        Type paramType = DataEx.SqlDbTypeToType(parameter.SqlDbType);
                        string localName = parameter.ParameterName[0] == '@' ? parameter.ParameterName.Substring(1) : parameter.ParameterName;
                        if (paramType == null)
                            s += "// public " + parameter.SqlDbType + localName + "; // (Unsupported DB type)";
                        else
                        {
                            // If it cant be restored to the original DB type through the obvious conversion then we need to annotate it
                            if (!DataEx.TypeToSqlDbType(paramType).HasValue || DataEx.TypeToSqlDbType(paramType).Value != parameter.SqlDbType)
                                s += "[DatabaseParam(SqlDbType." + parameter.SqlDbType + ")] ";
                            s += "public " + paramType.Name + " " + localName + ";";
                        }

                        s += " // " + parameter.SqlDbType;

                        if (parameter.Direction == ParameterDirection.Output)
                            s += " // (Output parameter - not supported)";
                        else if (parameter.Direction == ParameterDirection.InputOutput)
                            s += " // (Ref parameter - not supported)";
                        else if (parameter.Direction == ParameterDirection.ReturnValue)
                            s += " // (Return value)";

                        Console.WriteLine(s);
                    }

                    SqlDataReader reader = command.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.CloseConnection);
                    DataTable schema = reader.GetSchemaTable();
                    Console.WriteLine("Returns:");
                    if (schema == null)
                        Console.WriteLine("\t// No return fields");
                    else 
                        foreach (DataRow row in schema.Rows)
                        {
                            Console.WriteLine(String.Format("\tpublic {0} {1}; // {2}",
                                ((Type)row[schema.Columns["DataType"]]).Name,
                                row[schema.Columns["ColumnName"]],
                                row[schema.Columns["DataTypeName"]]
                                ));
                        }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        
    }
}
