using System;
using ConfigurationManager.Utilities;
using UnityEngine;
#if IL2CPP
using GUILayout = ConfigurationManager.Utilities.ManagedLayout;
#endif

namespace ConfigurationManager
{
    // Presentation caches belong to the window; plugin collection and input state stay in the main file.
    public partial class ConfigurationManager
    {
        private Font _chineseFont;
        private Font _englishFont;
        private bool _englishFontChecked;
        private bool _chineseFontChecked;
        private GUISkin _modernSkin;
        private GUIStyle _tipStyle;

        private void ApplyWindowAppearance(GUISkin originalSkin)
        {
            PrepareWindowSkin(originalSkin);
            GUI.skin = _modernSkin;
            var font = PrepareWindowFont();
            if (font != null) GUI.skin.font = font;
        }

        private void PrepareWindowSkin(GUISkin originalSkin)
        {
            if (_modernSkin == null) _modernSkin = ModernSkin.Create(originalSkin);
        }

        private Font PrepareWindowFont()
        {
            if (Localization.Language == Localization.SimplifiedChinese)
            {
                if (!_chineseFontChecked)
                {
                    _chineseFontChecked = true;
                    try
                    {
                        _chineseFont = ImguiCompatibility.CreateSystemFont(new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "WenQuanYi Micro Hei", "Arial Unicode MS" }, 17);
                    }
                    catch (Exception ex) { Logger.LogWarning("Unable to load a Chinese font: " + ex.Message); }
                }
                return _chineseFont;
            }
            else
            {
                if (!_englishFontChecked)
                {
                    _englishFontChecked = true;
                    try { _englishFont = ImguiCompatibility.CreateSystemFont(new[] { "Segoe UI", "Arial", "Liberation Sans" }, 16); }
                    catch (Exception ex) { Logger.LogWarning("Unable to load an interface font: " + ex.Message); }
                }
                return _englishFont;
            }
        }

        private void DrawTips()
        {
            var tip = !_tipsPluginHeaderWasClicked ? Localization.Text("Tip: Click a plugin to expand; hover over names for details.") :
                !_tipsWindowWasMoved ? Localization.Text("Tip: Drag the title to move; press the shortcut to close.") : null;
            if (tip == null) return;

            if (_tipStyle == null)
            {
                _tipStyle = GUI.skin.label.CreateCopy();
                _tipStyle.alignment = TextAnchor.UpperLeft;
                _tipStyle.wordWrap = true;
                _tipStyle.stretchWidth = true;
                _tipStyle.fixedHeight = 0;
            }
            // Reserve two lines for native font measurement differences; narrow windows can grow further.
            var minHeight = _tipStyle.lineHeight * 2 + _tipStyle.padding.top + _tipStyle.padding.bottom + 2;
            GUILayout.Label(tip, _tipStyle, ImguiCompatibility.ExpandWidth(true), ImguiCompatibility.MinHeight(minHeight));
        }
    }
}
