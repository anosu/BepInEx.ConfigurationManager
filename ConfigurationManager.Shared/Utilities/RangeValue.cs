using System;
using System.Globalization;

namespace ConfigurationManager.Utilities
{
    internal static class RangeValue
    {
        internal static object Parse(string text, Type type, object min, object max)
        {
            return Clamp(Convert.ChangeType(text, type, CultureInfo.InvariantCulture), min, max);
        }

        internal static object FromSlider(float value, Type type, object min, object max)
        {
            // Float endpoints can round outside an integer type's domain. Keep the exact bounds.
            if (value <= Convert.ToSingle(min, CultureInfo.InvariantCulture)) return min;
            if (value >= Convert.ToSingle(max, CultureInfo.InvariantCulture)) return max;
            return Clamp(Convert.ChangeType(value, type, CultureInfo.InvariantCulture), min, max);
        }

        private static object Clamp(object value, object min, object max)
        {
            if (value is float f && (float.IsNaN(f) || float.IsInfinity(f)) ||
                value is double d && (double.IsNaN(d) || double.IsInfinity(d)))
                throw new FormatException("A range value must be finite.");
            var comparable = (IComparable)value;
            if (comparable.CompareTo(min) < 0) return min;
            if (comparable.CompareTo(max) > 0) return max;
            return value;
        }
    }
}
