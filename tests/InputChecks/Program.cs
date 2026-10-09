using BepInEx.Configuration;
using UnityEngine;

var input = CompatibleUnityInput.Current;
#if MODERN
var held = KeyCode.F1;
var pressed = KeyCode.A;
var mouseX = 789;
var resetCount = 10;
var keys = new[] { KeyCode.F1, KeyCode.A };
#else
var held = KeyCode.A;
var pressed = KeyCode.F1;
var mouseX = 123;
var resetCount = 1;
var keys = Enum.GetValues<KeyCode>();
#endif
if (!input.GetKey(held) || input.GetKey(pressed) ||
    !input.GetKeyDown(pressed) || input.GetKeyDown(held) ||
    !input.GetKeyUp(held) || input.GetKeyUp(pressed))
    throw new Exception("Key states did not come from the selected backend.");
if (input.mousePosition.x != mouseX || !input.SupportedKeyCodes.SequenceEqual(keys))
    throw new Exception("Mouse position or supported keys came from the wrong backend.");
input.ResetInputAxes();
if (Input.ResetCount != resetCount)
    throw new Exception("ResetInputAxes did not reach the selected backend.");
Console.WriteLine("PASS: key held/down/up, mouse, supported keys and input reset reach the correct backend.");
