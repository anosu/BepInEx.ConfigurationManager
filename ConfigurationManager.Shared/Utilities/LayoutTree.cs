using System;
using System.Collections.Generic;

namespace ConfigurationManager.Utilities
{
    internal enum LayoutKind { Leaf, Row, Column, Scroll }
    internal enum LayoutRule { Width, Height, MinWidth, MaxWidth, MinHeight, MaxHeight, ExpandWidth, ExpandHeight }

    internal sealed class LayoutOption
    {
        internal readonly LayoutRule Rule;
        internal readonly float Value;
        internal LayoutOption(LayoutRule rule, float value) { Rule = rule; Value = value; }
    }

    internal struct LayoutRect
    {
        internal float X, Y, Width, Height;
        internal LayoutRect(float x, float y, float width, float height)
        { X = x; Y = y; Width = Math.Max(0, width); Height = Math.Max(0, height); }
        internal bool Contains(float x, float y) => x >= X && y >= Y && x < X + Width && y < Y + Height;
    }

    internal sealed class LayoutNode
    {
        internal readonly LayoutKind Kind;
        internal readonly string Key;
        internal readonly List<LayoutNode> Children = new List<LayoutNode>();
        internal LayoutRect Rect;
        internal float PreferredWidth, PreferredHeight;
        internal float Width = -1, Height = -1, MinWidth, MinHeight, MaxWidth = float.MaxValue, MaxHeight = float.MaxValue;
        internal float Left, Right, Top, Bottom, Gap = 4;
        internal bool ExpandWidth, ExpandHeight;
        internal float ScrollY, ContentHeight;
        internal Func<float, float> Measure;

        internal LayoutNode(LayoutKind kind, string key) { Kind = kind; Key = key; }
        internal void Apply(LayoutOption[] options)
        {
            if (options == null) return;
            foreach (var option in options)
            {
                switch (option.Rule)
                {
                    case LayoutRule.Width: Width = option.Value; ExpandWidth = false; break;
                    case LayoutRule.Height: Height = option.Value; ExpandHeight = false; break;
                    case LayoutRule.MinWidth: MinWidth = option.Value; break;
                    case LayoutRule.MaxWidth: MaxWidth = option.Value; break;
                    case LayoutRule.MinHeight: MinHeight = option.Value; break;
                    case LayoutRule.MaxHeight: MaxHeight = option.Value; break;
                    case LayoutRule.ExpandWidth: ExpandWidth = option.Value != 0; break;
                    case LayoutRule.ExpandHeight: ExpandHeight = option.Value != 0; break;
                }
            }
        }
    }

    // Geometry has no Unity/native objects. Every event builds the same complete tree.
    internal static class LayoutTree
    {
        internal static void Arrange(LayoutNode node, LayoutRect rect)
        {
            node.Rect = rect;
            if (node.Kind == LayoutKind.Leaf) return;
            var inner = new LayoutRect(rect.X + node.Left, rect.Y + node.Top,
                rect.Width - node.Left - node.Right, rect.Height - node.Top - node.Bottom);
            if (node.Kind == LayoutKind.Row)
            {
                var widths = RowWidths(node, inner.Width);
                var x = inner.X;
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var height = Height(node.Children[i], widths[i]);
                    Arrange(node.Children[i], new LayoutRect(x, inner.Y + Math.Max(0, (inner.Height - height) / 2), widths[i], height));
                    x += widths[i] + node.Gap;
                }
                return;
            }
            if (node.Kind == LayoutKind.Scroll)
            {
                // Vertical scrolling reserves a rail on every frame. It never creates horizontal overflow.
                inner.Width = Math.Max(0, inner.Width - 16);
                node.ContentHeight = ColumnHeight(node, inner.Width);
                node.ScrollY = Math.Max(0, Math.Min(node.ScrollY, Math.Max(0, node.ContentHeight - inner.Height)));
                ArrangeColumn(node, new LayoutRect(inner.X, inner.Y - node.ScrollY, inner.Width, node.ContentHeight), false);
                return;
            }
            ArrangeColumn(node, inner, true);
        }

        private static void ArrangeColumn(LayoutNode node, LayoutRect rect, bool fill)
        {
            var heights = new float[node.Children.Count];
            var total = node.Gap * Math.Max(0, node.Children.Count - 1);
            var expand = 0;
            for (var i = 0; i < heights.Length; i++)
            {
                var child = node.Children[i];
                heights[i] = Height(child, Math.Min(rect.Width, child.MaxWidth));
                if (fill && child.ExpandHeight) { heights[i] = child.MinHeight; expand++; }
                total += heights[i];
            }
            var extra = fill && expand > 0 ? Math.Max(0, rect.Height - total) / expand : 0;
            var y = rect.Y;
            for (var i = 0; i < heights.Length; i++)
            {
                var child = node.Children[i];
                var height = heights[i] + (fill && child.ExpandHeight ? extra : 0);
                var width = Math.Min(rect.Width, child.Width >= 0 ? child.Width : child.MaxWidth);
                Arrange(child, new LayoutRect(rect.X, y, width, height));
                y += height + node.Gap;
            }
        }

        internal static float Height(LayoutNode node, float width)
        {
            if (node.Height >= 0) return Clamp(node.Height, node.MinHeight, node.MaxHeight);
            var innerWidth = Math.Max(0, width - node.Left - node.Right);
            float height;
            switch (node.Kind)
            {
                case LayoutKind.Row:
                    height = 0;
                    var widths = RowWidths(node, innerWidth);
                    for (var i = 0; i < widths.Length; i++)
                        height = Math.Max(height, Height(node.Children[i], widths[i]));
                    break;
                case LayoutKind.Column: height = ColumnHeight(node, innerWidth); break;
                case LayoutKind.Scroll: height = node.MinHeight; break;
                default: height = node.Measure != null ? node.Measure(width) : node.PreferredHeight; break;
            }
            return Clamp(height + node.Top + node.Bottom, node.MinHeight, node.MaxHeight);
        }

        private static float ColumnHeight(LayoutNode node, float width)
        {
            var height = node.Gap * Math.Max(0, node.Children.Count - 1);
            foreach (var child in node.Children)
                height += Height(child, Math.Min(width, child.Width >= 0 ? child.Width : child.MaxWidth));
            return height;
        }

        private static float[] RowWidths(LayoutNode node, float available)
        {
            var widths = new float[node.Children.Count];
            var usable = Math.Max(0, available - node.Gap * Math.Max(0, widths.Length - 1));
            var total = 0f;
            var expand = 0;
            for (var i = 0; i < widths.Length; i++)
            {
                var child = node.Children[i];
                widths[i] = child.Width >= 0 ? child.Width : child.ExpandWidth ? child.MinWidth : PreferredWidth(child);
                widths[i] = Clamp(widths[i], child.MinWidth, child.MaxWidth);
                total += widths[i];
                if (child.ExpandWidth && child.Width < 0) expand++;
            }
            var extra = expand > 0 ? Math.Max(0, usable - total) / expand : 0;
            total = 0;
            for (var i = 0; i < widths.Length; i++)
            {
                var child = node.Children[i];
                if (child.ExpandWidth && child.Width < 0) widths[i] = Math.Min(child.MaxWidth, widths[i] + extra);
                total += widths[i];
            }
            // Fixed widths still fit small windows and long translated headers.
            if (total > usable && total > 0)
                for (var i = 0; i < widths.Length; i++) widths[i] *= usable / total;
            return widths;
        }

        private static float PreferredWidth(LayoutNode node)
        {
            if (node.Width >= 0) return node.Width;
            if (node.Kind == LayoutKind.Leaf) return node.PreferredWidth;
            var width = 0f;
            foreach (var child in node.Children)
                width = node.Kind == LayoutKind.Row ? width + PreferredWidth(child) : Math.Max(width, PreferredWidth(child));
            return width + node.Left + node.Right + (node.Kind == LayoutKind.Row ? node.Gap * Math.Max(0, node.Children.Count - 1) : 0);
        }
        private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
}
