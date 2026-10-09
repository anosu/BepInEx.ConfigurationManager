namespace ConfigurationManager.Utilities
{
    // Update and IMGUI can observe the same press. Holding/repeating must never toggle twice.
    internal sealed class HotkeyGate
    {
        private bool _held;
        private int _frame = -1;
        internal bool Press(int frame)
        {
            if (_held || _frame == frame) return false;
            _held = true; _frame = frame; return true;
        }
        internal void Release() { _held = false; }
    }
}
