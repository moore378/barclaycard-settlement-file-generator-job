using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using Logging;

namespace AutoDatabase.DebugProxy
{
    public class DebugSqlDataReader : IDataReader
    {
        private SqlDataReader sqlDataReader;
        private ITreeLog log;
        private ITreeLog row;

        public DebugSqlDataReader(SqlDataReader sqlDataReader, ITreeLog log)
        {
            this.log = log;
            this.sqlDataReader = sqlDataReader;
        }

        public bool Read()
        {
            bool result = sqlDataReader.Read();
            row = log.CreateChild("Read() :" + result);
            return result;
        }

        public int GetOrdinal(string name)
        {
            int result = sqlDataReader.GetOrdinal(name);
            row.Log("GetOrdinal(" + name + "): " + result);
            return result;
        }

        public int GetInt32(int i)
        {
            var result = sqlDataReader.GetInt32(i);
            row.Log("GetInt32(" + i + "): " + result);
            return result;
        }

        public string GetString(int i)
        {
            var result = sqlDataReader.GetString(i);
            row.Log("GetString(" + i + "): " + result);
            return result;
        }

        public bool HasRows 
        { 
            get 
            {
                bool result = sqlDataReader.HasRows;
                log.Log("HasRows: " + result);
                return result; 
            } 
        }

        public string GetDataTypeName(int i)
        {
            var result = sqlDataReader.GetDataTypeName(i);
            row.Log("GetDataTypeName(" + i + "): " + result);
            return result;
        }

        public object GetProviderSpecificValue(int i)
        {
            var result = sqlDataReader.GetProviderSpecificValue(i);
            row.Log("GetProviderSpecificValue(" + i + "): " + result);
            return result;
        }

        public bool IsDBNull(int i)
        {
            var result = sqlDataReader.IsDBNull(i);
            row.Log("IsDBNull(" + i + "): " + result);
            return result;
        }
  

        public void Dispose()
        {
        }

        public void Close()
        {
            sqlDataReader.Close();
            log.Log("Close()");
        }

        public int Depth
        {
            get
            {
                var result = sqlDataReader.Depth;
                log.Log("Depth: " + result);
                return result;
            } 
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public bool IsClosed
        {
            get
            {
                var result = sqlDataReader.IsClosed;
                log.Log("IsClosed: " + result);
                return result;
            }
        }

        public bool NextResult()
        {
            var result = sqlDataReader.NextResult();
            log.Log("NextResult(): " + result);
            return result;
        }

        public int RecordsAffected
        {
            get
            {
                var result = sqlDataReader.RecordsAffected;
                log.Log("RecordsAffected: " + result);
                return result;
            }
        }

        public int FieldCount
        {
            get
            {
                var result = sqlDataReader.FieldCount;
                row.Log("FieldCount: " + result);
                return result;
            }
        }

        public bool GetBoolean(int i)
        {
            var result = sqlDataReader.GetBoolean(i);
            row.Log("GetBoolean(" + i + "): " + result);
            return result;
        }

        public SqlBinary GetSqlBinary(int i)
        {
            var result = sqlDataReader.GetSqlBinary(i);
            row.Log("GetSqlBinary(" + i + "): " + result);
            return result;
        }

        public byte GetByte(int i)
        {
            var result = sqlDataReader.GetByte(i);
            row.Log("GetByte(" + i + "): " + result);
            return result;
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            var result = sqlDataReader.GetChar(i);
            row.Log("GetChar(" + i + "): " + result);
            return result;
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            var result = sqlDataReader.GetData(i);
            row.Log("GetData(" + i + "): " + result);
            return result;
        }

        public DateTime GetDateTime(int i)
        {
            var result = sqlDataReader.GetDateTime(i);
            row.Log("GetDateTime(" + i + "): " + result);
            return result;
        }

        public decimal GetDecimal(int i)
        {
            var result = sqlDataReader.GetDecimal(i);
            row.Log("GetDecimal(" + i + "): " + result);
            return result;
        }

        public double GetDouble(int i)
        {
            var result = sqlDataReader.GetDouble(i);
            row.Log("GetDouble(" + i + "): " + result);
            return result;
        }

        public Type GetFieldType(int i)
        {
            var result = sqlDataReader.GetFieldType(i);
            row.Log("GetFieldType(" + i + "): " + result);
            return result;
        }

        public float GetFloat(int i)
        {
            var result = sqlDataReader.GetFloat(i);
            row.Log("GetFloat(" + i + "): " + result);
            return result;
        }

        public Guid GetGuid(int i)
        {
            var result = sqlDataReader.GetGuid(i);
            row.Log("GetGuid(" + i + "): " + result);
            return result;
        }

        public short GetInt16(int i)
        {
            var result = sqlDataReader.GetInt16(i);
            row.Log("GetInt16(" + i + "): " + result);
            return result;
        }

        public long GetInt64(int i)
        {
            var result = sqlDataReader.GetInt64(i);
            row.Log("GetInt64(" + i + "): " + result);
            return result;
        }

        public string GetName(int i)
        {
            var result = sqlDataReader.GetName(i);
            row.Log("GetName(" + i + "): " + result);
            return result;
        }

        public object GetValue(int i)
        {
            var result = sqlDataReader.GetValue(i);
            row.Log("GetValue(" + i + "): " + result);
            return result;
        }

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public object this[string name]
        {
            get
            {
                var result = sqlDataReader[name];
                row.Log("[" + name + "]: " + result);
                return result;
            }
        }

        public object this[int i]
        {
            get
            {
                var result = sqlDataReader[i];
                row.Log("[" + i + "]: " + result);
                return result;
            }
        }
    }
}
