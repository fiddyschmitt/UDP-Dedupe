﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace udp_dedupe.Utilities
{
    public static class StringUtilities
    {
        public static string ToHexString(this byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }
    }
}
