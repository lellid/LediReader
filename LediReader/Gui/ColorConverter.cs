// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LediReader.Gui
{
    static class ColorConverter
    {
        public static (byte r, byte g, byte b, byte a) ToRGBA(uint value)
        {
            uint v = (uint)value;

            byte a = (byte)(v & 0xFF);
            v >>= 8;
            byte b = (byte)(v & 0xFF);
            v >>= 8;
            byte g = (byte)(v & 0xFF);
            v >>= 8;
            byte r = (byte)(v & 0xFF);
            return (r, g, b, a);
        }

        public static byte ToGrayFromRGBA(uint value)
        {
            var (r, g, b, a) = ToRGBA(value);
            return (byte)(0.30 * r + 0.59 * g + 0.11 * b);
        }

        public static uint ToRGBAIntFromGrayLevel(byte grayLevel)
        {
            return ToRGBAInt((grayLevel, grayLevel, grayLevel, 255));
        }

        public static uint ToRGBAInt((byte r, byte g, byte b, byte a) tuple)
        {
            uint v = 0;

            v |= tuple.r;
            v <<= 8;
            v |= tuple.g;
            v <<= 8;
            v |= tuple.b;
            v <<= 8;
            v |= tuple.a;
            return v;
        }
    }
}
