using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

public class DataEx
{
    public static Type SqlDbTypeToType(SqlDbType sqlDbType)
    {
        switch (sqlDbType)
        {
            case SqlDbType.BigInt: return typeof(System.Int64);
            case SqlDbType.Binary: return typeof(byte[]);
            case SqlDbType.Bit: return typeof(System.Boolean);
            case SqlDbType.Char: return typeof(System.String);
            case SqlDbType.Date:
            case SqlDbType.DateTime:
            case SqlDbType.DateTime2: return typeof(DateTime);
            case SqlDbType.DateTimeOffset: return typeof(DateTimeOffset);
            case SqlDbType.Decimal: return typeof(System.Decimal);
            case SqlDbType.Float: return typeof(System.Double);
            case SqlDbType.Image: return typeof(byte[]);
            case SqlDbType.Int: return typeof(System.Int32);
            case SqlDbType.Money: return typeof(System.Decimal);
            case SqlDbType.NChar:
            case SqlDbType.NText:
            case SqlDbType.NVarChar: return typeof(System.String);
            case SqlDbType.Real: return typeof(System.Single);
            case SqlDbType.SmallDateTime: return typeof(System.DateTime);
            case SqlDbType.SmallInt: return typeof(System.Int16);
            case SqlDbType.SmallMoney: return typeof(System.Decimal);
            case SqlDbType.Structured: return null;
            case SqlDbType.Text: return typeof(System.String);
            case SqlDbType.Time: return typeof(System.DateTime);
            case SqlDbType.Timestamp: return typeof(byte[]);
            case SqlDbType.TinyInt: return typeof(byte);
            case SqlDbType.Udt: return null;
            case SqlDbType.UniqueIdentifier: return typeof(System.Guid);
            case SqlDbType.VarBinary: return typeof(byte[]);
            case SqlDbType.VarChar: return typeof(System.String);
            case SqlDbType.Variant: return typeof(object);
            case SqlDbType.Xml: return null;
            default: throw new NotSupportedException("Unknown SqlDbType \"" + sqlDbType + "\"");
        }
    }

    public static SqlDbType? TypeToSqlDbType(Type type)
    {
        if (type == typeof(string)) return SqlDbType.VarChar;
        else if (type == typeof(decimal)) return SqlDbType.Decimal;
        else if (type == typeof(bool)) return SqlDbType.Bit;
        else if (type == typeof(byte[])) return SqlDbType.Binary;
        else if (type == typeof(long)) return SqlDbType.BigInt;
        else if (type == typeof(DateTime)) return SqlDbType.DateTime;
        else if (type == typeof(DateTimeOffset)) return SqlDbType.DateTimeOffset;
        else if (type == typeof(double)) return SqlDbType.Float;
        else if (type == typeof(float)) return SqlDbType.Real;
        else if (type == typeof(short)) return SqlDbType.SmallInt;
        else if (type == typeof(Guid)) return SqlDbType.UniqueIdentifier;
        else if (type == typeof(int)) return SqlDbType.Int;
        else return null;
    }
}
