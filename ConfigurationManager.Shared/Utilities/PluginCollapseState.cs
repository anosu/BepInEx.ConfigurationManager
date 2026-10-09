using System;
using System.Collections.Generic;

namespace ConfigurationManager.Utilities
{
    // Plugin display names may collide or change; identity and temporary visibility do not.
    internal sealed class PluginCollapseState
    {
        private readonly Dictionary<string, bool> _overrides = new Dictionary<string, bool>(StringComparer.Ordinal);
        private bool _defaultCollapsed;

        internal PluginCollapseState(bool defaultCollapsed) { _defaultCollapsed = defaultCollapsed; }
        internal bool Get(string guid) => _overrides.TryGetValue(guid, out var collapsed) ? collapsed : _defaultCollapsed;
        internal void Set(string guid, bool collapsed)
        {
            if (collapsed == _defaultCollapsed) _overrides.Remove(guid);
            else _overrides[guid] = collapsed;
        }
        internal void Reset(bool defaultCollapsed)
        {
            _defaultCollapsed = defaultCollapsed;
            _overrides.Clear();
        }
    }
}
