using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class BitConverterEx
{
    public static void WriteBytes(ushort value, byte[] target, int offset)
    {
        if (offset + 2 > target.Length)
            throw new ArgumentOutOfRangeException("offset");
        target[offset] = (byte)value;
        target[offset + 1] = (byte)(value >> 8);
    }

    public static void WriteBytes(int value, byte[] target, int offset)
    {
        if (offset + 4 > target.Length)
            throw new ArgumentOutOfRangeException("offset");
        uint temp = unchecked((uint)value);
        target[offset] = (byte)temp;
        target[offset + 1] = (byte)(temp >> 8);
        target[offset + 2] = (byte)(temp >> 16);
        target[offset + 3] = (byte)(temp >> 24);
    }
}