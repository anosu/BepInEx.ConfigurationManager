using System;

namespace ConfigurationManager.Utilities
{
    // All steps run on the caller's Unity thread, once per hidden repaint frame.
    internal sealed class WindowWarmup
    {
        private readonly Action[] _steps;
        private int _next;
        private int _lastFrame = -1;

        internal WindowWarmup(params Action[] steps) { _steps = steps; }
        internal void Advance(int frame, bool windowVisible)
        {
            if (windowVisible || _next == _steps.Length || _lastFrame == frame) return;
            _lastFrame = frame;
            // A failed preparation must not retry on every GUI callback; normal opening keeps its fallback.
            var step = _steps[_next++];
            step();
        }
    }
}
