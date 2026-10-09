using ConfigurationManager.Utilities;

var offset = ScrollMath.Update(0, 72, 1200, 400, false);
if (offset != 72) throw new Exception("Wheel input failed to move expanded content.");
offset = ScrollMath.Update(offset, 0, 0, 0, true);
if (offset != 72) throw new Exception("Layout measurement reset the scroll offset to zero.");
offset = ScrollMath.Update(offset, 0, 1200, 400, false);
if (offset != 72) throw new Exception("Repaint lost the wheel position.");
if (ScrollMath.Update(72, 0, 0, 0, true) != 72)
    throw new Exception("Consumed-event measurement reset the scroll position.");
if (ScrollMath.Update(72, 0, 100, 400, false) != 0)
    throw new Exception("Collapsed content must clamp a now-invalid offset.");
if (ScrollMath.Update(0, -24, 1200, 400, false) != 0 ||
    ScrollMath.Update(790, 48, 1200, 400, false) != 800)
    throw new Exception("Scroll bounds failed.");
Console.WriteLine("PASS: wheel position survives layout/repaint and clamps after collapsing or reaching bounds.");
