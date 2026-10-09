using ConfigurationManager.Utilities;

var state = new TextEditState("123");
state.SelectAll();
state.Insert("456");
Check("456", 3);
state.Left(false);
state.Insert(".");
Check("45.6", 3);
state.Backspace();
Check("456", 2);
state.Delete();
Check("45", 2);
state.Move(0, false);
state.Right(true);
if (state.SelectedText != "4") throw new Exception("Shift selection failed.");
state.Insert("中文");
Check("中文5", 2);
state.Sync("A😀B");
state.Left(false);
state.Backspace();
Check("AB", 1);
state.Sync("A😀B");
state.Move(1, false);
state.Delete();
Check("AB", 1);
state.Sync("A😀B");
state.Move(0, false);
state.Right(false);
state.Right(false);
Check("A😀B", 3);
state.SelectAll();
state.Delete();
state.Backspace();
Check("", 0);
state.Sync(null!);
Check("", 0);
state.Sync("first 中文 last");
state.WordLeft(false);
Check("first 中文 last", 9);
state.WordLeft(true);
if (state.SelectedText != "中文 ") throw new Exception("Word selection failed.");
state.Left(false);
Check("first 中文 last", 6);
state.WordRight(true);
state.Right(false);
Check("first 中文 last", 9);
state.SelectAll();
state.Insert("a\r\nb\tc");
Check("ab c", 4);
state.Undo();
Check("first 中文 last", 13);
state.Redo();
Check("ab c", 4);
Console.WriteLine("PASS: replacement, caret editing, deletion, selection, Chinese text and Unicode surrogate pairs.");

void Check(string text, int caret)
{
    if (state.Text != text || state.Caret != caret)
        throw new Exception($"Expected '{text}' at {caret}; got '{state.Text}' at {state.Caret}.");
}
