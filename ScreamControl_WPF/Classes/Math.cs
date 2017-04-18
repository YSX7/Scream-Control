using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreamControl_WPF
{
    public static class Math
    {
        public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(max) > 0)
                return max;
            else
                if (value.CompareTo(min) < 0)
                    return min;
                else return value;
        }
    }
}
