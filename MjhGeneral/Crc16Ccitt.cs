﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Adapted from http://www.sanity-free.org/133/crc_16_ccitt_in_csharp.html
public class Crc16Ccitt
{
    const ushort poly = 4129;
    static ushort[] table = new ushort[256];

    public static ushort Compute(byte[] bytes, int startOffset, int count, ushort initialValue)
    {
        ushort crc = initialValue;
        int end = startOffset + count;
        for (int i = startOffset; i < end; i++)
            crc = (ushort)((crc << 8) ^ table[((crc >> 8) ^ (0xff & bytes[i]))]);
        return crc;
    }

    static Crc16Ccitt()
    {
        ushort temp, a;
        for (int i = 0; i < table.Length; ++i)
        {
            temp = 0;
            a = (ushort)(i << 8);
            for (int j = 0; j < 8; ++j)
            {
                if (((temp ^ a) & 0x8000) != 0)
                {
                    temp = (ushort)((temp << 1) ^ poly);
                }
                else
                {
                    temp <<= 1;
                }
                a <<= 1;
            }
            table[i] = temp;
        }
    }
}
