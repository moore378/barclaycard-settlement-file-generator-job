using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyIL
{
    public interface IValue
    {
        Type ValueType { get; }
    }

    /// <summary>
    /// R-value (read but not write)
    /// </summary>
    public interface IRValue : IValue
    {
        ILSeq Read { get; }
    }

    /// <summary>
    /// An L-value (writable)
    /// </summary>
    public interface IWValue : IValue
    {
       ILSeq WritePreCalc { get; }
       ILSeq WritePostCalc { get; }
    }

    /// <summary>
    /// A read-writable value
    /// </summary>
    public interface IRWValue : IRValue, IWValue
    {
        new Type ValueType { get; }
    }

    /// <summary>
    /// A writable value of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IWValue<in T> : IWValue
    {
        
    }

    /// <summary>
    /// A readable value of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRValue<out T> : IRValue
    {

    }

    /// <summary>
    /// A read-writable value of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRWValue<T> : IRWValue, IRValue<T>, IWValue<T>
    {

    }
}
