using UnityEngine;
using UnityEngine.UI;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// Shared UI helpers for GameManager and HUDManager.
    /// </summary>
    public static class UIUtility
    {
        private static readonly string[] BuiltinFontNames = {
            "LegacyRuntime.ttf",
            "Arial.ttf",
            "Arial",
        };

        /// <summary>Returns the first loadable built-in font, or null to use Unity default.</summary>
        public static Font GetBuiltinFont()
        {
            foreach (string name in BuiltinFontNames)
            {
                try
                {
                    var f = Resources.GetBuiltinResource<Font>(name);
                    if (f != null) return f;
                }
                catch { /* try next */ }
            }
            return null;
        }

        /// <summary>Configure a Text component with safe defaults.</summary>
        public static void StyleText(Text t, int size, FontStyle style = FontStyle.Bold,
                                     TextAnchor anchor = TextAnchor.MiddleCenter, Color? color = null)
        {
            if (t == null) return;
            t.font      = GetBuiltinFont();
            t.fontSize  = size;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color     = color ?? Color.white;
        }
    }
}
