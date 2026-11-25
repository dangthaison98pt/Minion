using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace OrostrideToolkit.Core
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ColorConverter
    {
        [FieldOffset(0)] public Color32 Color;
        [FieldOffset(0)] public int IntValue;
    }

    public static class Color32Extensions
    {
        public static int ToInt(this Color32 c)
        {
            return new ColorConverter { Color = c }.IntValue;
        }

        public static Color32 ToColor32(this int value)
        {
            return new ColorConverter { IntValue = value }.Color;
        }

        public static bool EqualsFast(this Color32 a, Color32 b)
        {
            return a.ToInt() == b.ToInt();
        }

        public static HashSet<int> ToIntHashSet(this Color32[] colors)
        {
            if (colors == null) return null;
            var set = new HashSet<int>(colors.Length);
            for (int i = 0; i < colors.Length; i++)
            {
                set.Add(colors[i].ToInt());
            }
            return set;
        }

        public static HashSet<int> ToIntHashSet(this List<Color32> colors)
        {
            if (colors == null) return null;
            int count = colors.Count;
            var set = new HashSet<int>(count);
            for (int i = 0; i < count; i++)
            {
                set.Add(colors[i].ToInt());
            }
            return set;
        }
    }
}