using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace AutoDatabase.DebugProxy
{
    public class DebugSqlCommand
    {
        public static object LogLock = new object();

        private SqlCommand command;
        private Logging.ITreeLog log;

        public DebugSqlCommand(string commandText, DebugSqlConnection connection)
        {
            this.command = new SqlCommand(commandText, connection.Inner);
            this.log = connection.CreateChildLog("New Command: " + commandText);
            this.Parameters = new DebugSqlParameterCollection(command.Parameters, log.CreateChild("Parameters"));
        }

        public IAsyncResult BeginExecuteReader(AsyncCallback callback, Object stateObject, CommandBehavior behavior)
        {
            log.Log("BeginExecuteReader(behavior: " + behavior + ")");
            return command.BeginExecuteReader(callback, stateObject, behavior);
        }

        public DebugSqlDataReader EndExecuteReader(IAsyncResult result)
        {
            return new DebugSqlDataReader(command.EndExecuteReader(result), log.CreateChild("EndExecuteReader"));
        }

        public SqlConnection Connection
        { 
            get { return command.Connection; } 
            set 
            { 
                command.Connection = value;
                log.Log("Connection = " + value);
            } 
        }

        public string CommandText 
        { 
            get { return command.CommandText; } 
            set 
            { 
                command.CommandText = value;
                log.Log("CommandText = " + value);
            } 
        }

        public CommandType CommandType 
        { 
            get { return command.CommandType; } 
            set 
            { 
                command.CommandType = value;
                log.Log("CommandType = " + value);
            } 
        }
        public DebugSqlParameterCollection Parameters { get; private set; }
    }
}
