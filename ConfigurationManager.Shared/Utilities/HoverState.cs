namespace ConfigurationManager.Utilities
{
    internal sealed class HoverState
    {
        internal string Text { get; private set; }
        private float _since;
        internal void Update(string text, bool blocked, float now)
        {
            text = blocked || string.IsNullOrEmpty(text) ? null : text;
            if (Text == text) return;
            Text = text; _since = now;
        }
        internal bool Visible(float now) => Text != null && now - _since >= 0.45f;
    }
}
