using System;

namespace ConfigurationManager.Utilities
{
    internal static class ScrollMath
    {
        internal static float Update(float position, float wheelDelta, float contentSize, float viewportSize, bool measuring)
        {
            // Layout groups have no computed bounds while Unity is collecting controls.
            if (measuring) return position;
            return Math.Max(0, Math.Min(Math.Max(0, contentSize - viewportSize), position + wheelDelta));
        }
    }
}
