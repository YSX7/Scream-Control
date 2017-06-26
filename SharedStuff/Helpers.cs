using System;

namespace ScreamControl
{
    public enum ConnectionInfoStates { Initializing, Ready, Connected, Disconnected, Failed };

    public static class MathEx
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
