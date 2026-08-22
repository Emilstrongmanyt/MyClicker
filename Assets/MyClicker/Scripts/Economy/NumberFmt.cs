using System;
using System.Globalization;
using UnityEngine;

namespace MyClicker.Economy
{
    public static class NumberFmt
    {
        static readonly string[] Suffixes =
        {
            "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc",
            "Ud", "Dd", "Td", "Qad", "Qid", "Sxd", "Spd", "Ocd", "Nod", "Vg"
        };

        public static string Compact(int value) => Compact((double)value);

        public static string Compact(long value) => Compact((double)value);

        public static string Compact(float value) => Compact((double)value);

        public static string Compact(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "∞";
            if (value < 0d)
                return "-" + Compact(-value);
            if (value < 1000d)
            {
                if (value >= 10d || Math.Abs(value - Math.Floor(value)) < 0.05d)
                    return Math.Floor(value).ToString(CultureInfo.InvariantCulture);
                return value.ToString("0.0", CultureInfo.InvariantCulture);
            }

            double scaled = value;
            int suffix = 0;
            while (scaled >= 1000d && suffix < Suffixes.Length - 1)
            {
                scaled /= 1000d;
                suffix++;
            }

            if (scaled >= 1000d)
                return value.ToString("0.00e0", CultureInfo.InvariantCulture);
            if (scaled >= 100d)
                return Mathf.FloorToInt((float)scaled) + Suffixes[suffix];
            if (scaled >= 10d)
                return scaled.ToString("0.0", CultureInfo.InvariantCulture) + Suffixes[suffix];
            return scaled.ToString("0.00", CultureInfo.InvariantCulture) + Suffixes[suffix];
        }

        public static string Gold(double value) => Compact(value) + "g";

        public static string Signed(double value)
        {
            if (value >= 0d)
                return "+" + Compact(value);
            return Compact(value);
        }
    }
}
