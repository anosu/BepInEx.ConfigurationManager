using UnityEngine;

namespace ConfigurationManager.Utilities
{
    internal static class ModernSkin
    {
        internal const int WindowWidth = 820;
        internal const int ControlHeight = 36;
        internal const int ResetButtonWidth = 84;
        internal static readonly Color Accent = new Color(0.36f, 0.70f, 1f);

        internal static RectOffset Offset(int left, int right, int top, int bottom)
        {
            return new RectOffset { left = left, right = right, top = top, bottom = bottom };
        }

        internal static GUISkin Create(GUISkin original)
        {
#if IL2CPP
            var skin = UnityEngine.Object.Instantiate(original).Cast<GUISkin>();
#else
            var skin = UnityEngine.Object.Instantiate(original) as GUISkin;
#endif
            var background = Texture(new Color(0.07f, 0.09f, 0.13f));
            var card = Texture(new Color(0.12f, 0.15f, 0.20f));
            var control = Texture(new Color(0.18f, 0.22f, 0.28f));
            var hover = Texture(new Color(0.24f, 0.29f, 0.36f));
            var selected = Texture(new Color(0.14f, 0.27f, 0.43f));
            var accent = Texture(Accent);
            var text = new Color(0.97f, 0.98f, 1f);

            skin.label = original.label.CreateCopy();
            Configure(skin.label, null, null, null, text);
            skin.label.fontSize = 16;
            skin.label.alignment = TextAnchor.MiddleLeft;
            skin.label.wordWrap = true;
            skin.label.padding = ModernSkin.Offset(4, 4, 5, 5);

            skin.window = original.window.CreateCopy();
            Configure(skin.window, background, background, background, text);
            skin.window.fontSize = 20;
            skin.window.alignment = TextAnchor.UpperLeft;
            skin.window.padding = ModernSkin.Offset(18, 18, 48, 16);

            skin.box = original.box.CreateCopy();
            Configure(skin.box, card, card, card, text);
            skin.box.padding = ModernSkin.Offset(12, 12, 10, 10);
            skin.box.margin = ModernSkin.Offset(0, 0, 4, 6);

            skin.button = original.button.CreateCopy();
            Configure(skin.button, control, hover, selected, text);
            skin.button.padding = ModernSkin.Offset(12, 12, 7, 7);
            skin.button.margin = ModernSkin.Offset(3, 3, 3, 3);
            skin.button.fontSize = 16;
            skin.button.fixedHeight = ControlHeight;
            skin.button.alignment = TextAnchor.MiddleCenter;

            skin.toggle = skin.button.CreateCopy();
            skin.toggle.onNormal.background = selected;
            skin.toggle.onNormal.textColor = text;
            skin.toggle.onHover.background = selected;
            skin.toggle.onHover.textColor = Color.white;
            skin.toggle.onActive.background = selected;
            skin.toggle.onActive.textColor = Color.white;
            skin.toggle.onFocused.background = selected;
            skin.toggle.onFocused.textColor = text;

            skin.textField = original.textField.CreateCopy();
            Configure(skin.textField, background, control, control, text);
            skin.textField.fontSize = 17;
            skin.textField.padding = ModernSkin.Offset(10, 10, 6, 6);
            skin.textField.margin = ModernSkin.Offset(3, 3, 3, 3);
            skin.textField.fixedHeight = ControlHeight;
            skin.textField.alignment = TextAnchor.MiddleLeft;
            skin.textField.stretchWidth = true;

            skin.scrollView = original.scrollView.CreateCopy();
            skin.scrollView.normal.background = null;
            skin.scrollView.padding = ModernSkin.Offset(0, 0, 0, 0);
            skin.horizontalSlider = original.horizontalSlider.CreateCopy();
            skin.horizontalSliderThumb = original.horizontalSliderThumb.CreateCopy();
            skin.verticalSlider = original.verticalSlider.CreateCopy();
            skin.verticalSliderThumb = original.verticalSliderThumb.CreateCopy();
            Configure(skin.horizontalSlider, control, control, control, text);
            skin.horizontalSlider.fixedHeight = 8;
            skin.horizontalSlider.margin = ModernSkin.Offset(4, 4, 12, 10);
            Configure(skin.horizontalSliderThumb, accent, accent, accent, text);
            Configure(skin.verticalSlider, control, control, control, text);
            Configure(skin.verticalSliderThumb, accent, accent, accent, text);
            return skin;
        }

        private static void Configure(GUIStyle style, Texture2D normal, Texture2D hover, Texture2D active, Color text)
        {
            style.font = null;
            style.fontStyle = FontStyle.Normal;
            style.contentOffset = Vector2.zero;
            style.clipping = TextClipping.Clip;
            style.fixedWidth = style.fixedHeight = 0;
            style.stretchWidth = style.stretchHeight = false;
            style.wordWrap = false;
            style.border = ModernSkin.Offset(0, 0, 0, 0);
            style.overflow = ModernSkin.Offset(0, 0, 0, 0);
            style.normal.background = normal;
            style.normal.textColor = text;
            style.hover.background = hover;
            style.hover.textColor = Color.white;
            style.active.background = active;
            style.active.textColor = Color.white;
            style.focused.background = hover;
            style.focused.textColor = text;
            style.onNormal.background = normal;
            style.onNormal.textColor = text;
            style.onHover.background = hover;
            style.onHover.textColor = Color.white;
            style.onActive.background = active;
            style.onActive.textColor = Color.white;
            style.onFocused.background = hover;
            style.onFocused.textColor = text;
        }

        private static Texture2D Texture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
