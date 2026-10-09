using System;
using System.Collections.Generic;

namespace ConfigurationManager.Utilities
{
    internal sealed class TextEditState
    {
        internal string Text { get; private set; }
        internal int Caret { get; private set; }
        internal float ScrollX = 0;
        internal int SelectionStart => Start;
        internal int SelectionLength => Length;
        private int _anchor;
        private int Start => Math.Min(Caret, _anchor);
        private int Length => Math.Abs(Caret - _anchor);
        internal string SelectedText => Text.Substring(Start, Length);
        private readonly Stack<Snapshot> _undo = new Stack<Snapshot>();
        private readonly Stack<Snapshot> _redo = new Stack<Snapshot>();
        private struct Snapshot
        {
            internal string Text;
            internal int Caret, Anchor;
        }
        private Snapshot Capture() => new Snapshot { Text = Text, Caret = Caret, Anchor = _anchor };
        private void Restore(Snapshot snapshot) { Text = snapshot.Text; Caret = snapshot.Caret; _anchor = snapshot.Anchor; }

        internal TextEditState(string text) { Sync(text); }
        internal void Sync(string text)
        {
            text = text ?? string.Empty;
            if (Text == text) return;
            Text = text;
            Caret = _anchor = Text.Length;
            _undo.Clear(); _redo.Clear();
        }
        internal void SelectAll() { _anchor = 0; Caret = Text.Length; }
        internal void Move(int position, bool extend)
        {
            Caret = Math.Max(0, Math.Min(Text.Length, position));
            if (Caret > 0 && Caret < Text.Length && char.IsSurrogatePair(Text, Caret - 1))
                Caret--;
            if (!extend) _anchor = Caret;
        }
        internal void Insert(string value)
        {
            value = (value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("\t", " ");
            if (Length == 0 && value.Length == 0) return;
            _undo.Push(Capture()); _redo.Clear();
            var start = Start;
            Text = Text.Remove(start, Length).Insert(start, value);
            Caret = _anchor = start + value.Length;
        }
        internal void Backspace()
        {
            if (Length > 0) { Insert(string.Empty); return; }
            if (Caret == 0) return;
            var count = Caret > 1 && char.IsSurrogatePair(Text, Caret - 2) ? 2 : 1;
            _anchor = Caret - count;
            Insert(string.Empty);
        }
        internal void Delete()
        {
            if (Length > 0) { Insert(string.Empty); return; }
            if (Caret == Text.Length) return;
            _anchor = Caret + (Caret + 1 < Text.Length && char.IsSurrogatePair(Text, Caret) ? 2 : 1);
            Insert(string.Empty);
        }
        internal void Left(bool extend)
        {
            if (!extend && Length > 0) { Move(Start, false); return; }
            Move(Caret - (Caret > 1 && char.IsSurrogatePair(Text, Caret - 2) ? 2 : 1), extend);
        }
        internal void Right(bool extend)
        {
            if (!extend && Length > 0) { Move(Start + Length, false); return; }
            Move(Caret + (Caret + 1 < Text.Length && char.IsSurrogatePair(Text, Caret) ? 2 : 1), extend);
        }
        internal void WordLeft(bool extend)
        {
            var target = Caret;
            while (target > 0 && char.IsWhiteSpace(Text[target - 1])) target--;
            while (target > 0 && !char.IsWhiteSpace(Text[target - 1])) target--;
            Move(target, extend);
        }
        internal void WordRight(bool extend)
        {
            var target = Caret;
            while (target < Text.Length && !char.IsWhiteSpace(Text[target])) target++;
            while (target < Text.Length && char.IsWhiteSpace(Text[target])) target++;
            Move(target, extend);
        }
        internal void Undo()
        {
            if (_undo.Count == 0) return;
            _redo.Push(Capture()); Restore(_undo.Pop());
        }
        internal void Redo()
        {
            if (_redo.Count == 0) return;
            _undo.Push(Capture()); Restore(_redo.Pop());
        }
    }
}
