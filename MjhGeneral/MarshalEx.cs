using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

public static class MarshalEx
{
    public static T ReadClassFromBytes<T>(byte[] source, int startIndex)
    {
        return (T)ReadClassFromBytes(source, startIndex, typeof(T));
    }

    // Unsafe version
    /*public unsafe static object ReadClassFromBytes(byte[] source, int startIndex, Type objType)
    {
        int structSize = Marshal.SizeOf(objType);
        if (structSize > startIndex + source.Length)
            throw new InvalidOperationException("Not enough data to deserialize object");
        fixed (byte* p = &source[0])
        {
            return Marshal.PtrToStructure(new IntPtr(p), objType);
        }
    }*/

    // Safe version
    public static object ReadClassFromBytes(byte[] source, int startIndex, Type objType)
    {
        int structSize = Marshal.SizeOf(objType);
        if (structSize > startIndex + source.Length)
            throw new InvalidOperationException("Not enough data to deserialize object");
        IntPtr mem = Marshal.AllocHGlobal(structSize);
        try
        {
            Marshal.Copy(source, startIndex, mem, structSize);
            return Marshal.PtrToStructure(mem, objType);
        }
        finally
        {
            Marshal.FreeHGlobal(mem);
        }
    }

    // Unsafe version
    /*public unsafe static void StructToBytes(object structure, byte[] target, int targetOffset)
    {
        int dataSize = Marshal.SizeOf(structure);

        if (targetOffset + dataSize > target.Length)
            throw new ArgumentOutOfRangeException("targetOffset");

        fixed (byte* p = &target[targetOffset])
            Marshal.StructureToPtr(structure, new IntPtr(p), false);
    }*/

    // Safe version
    public static void StructToBytes(object structure, byte[] target, int targetOffset)
    {
        int dataSize = Marshal.SizeOf(structure);

        if (targetOffset + dataSize > target.Length)
            throw new ArgumentOutOfRangeException("targetOffset");

        IntPtr mem = Marshal.AllocHGlobal(dataSize);
        try
        {
            Marshal.StructureToPtr(structure, mem, false);
            Marshal.Copy(mem, target, targetOffset, dataSize);
        }
        finally
        {
            Marshal.FreeHGlobal(mem);
        }
    }

    public static byte[] StructToBytes(object structure)
    {
        int dataSize = Marshal.SizeOf(structure);
        byte[] buffer = new byte[dataSize];
        StructToBytes(structure, buffer, 0);
        return buffer;
    }
}