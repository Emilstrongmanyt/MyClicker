using System.Globalization;
using UnityEngine;

namespace MyClicker.Economy
{
    public static class NumberFmt
    {
        static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi" };

        public static string Compact(long value)
        {
            if (value < 1000)
                return value.ToString(CultureInfo.InvariantCulture);

            double scaled = value;
            int suffix = 0;
            while (scaled >= 1000d && suffix < Suffixes.Length - 1)
            {
                scaled /= 1000d;
                suffix++;
            }

            if (scaled >= 100d)
                return Mathf.FloorToInt((float)scaled) + Suffixes[suffix];
            if (scaled >= 10d)
                return scaled.ToString("0.0", CultureInfo.InvariantCulture) + Suffixes[suffix];
            return scaled.ToString("0.00", CultureInfo.InvariantCulture) + Suffixes[suffix];
        }

        public static string Gold(long value) => Compact(value) + "g";

        public static string Signed(long value)
        {
            if (value >= 0)
                return "+" + Compact(value);
            return Compact(value);
        }
    }
}
