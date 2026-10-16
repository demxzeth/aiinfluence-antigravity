using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace AIInfluenceAntigravity.Infra
{
    /// <summary>
    /// In-game chat notices. Uses Taleworlds' InformationManager to show
    /// messages in the game UI feed. Falls back to the log in any
    /// situation where the UI bridge is unavailable.
    ///
    /// The prefix [Antigravity] always has a constant violet color,
    /// and the rest of the message body is always pure white (#FFFFFFFF).
    /// </summary>
    public static class ChatNotices
    {
        public const string Prefix = "[Antigravity] ";
        public const float VioletR = 0.68f;
        public const float VioletG = 0.45f;
        public const float VioletB = 0.98f;

        private static readonly ConcurrentQueue<string> PendingNotices =
            new ConcurrentQueue<string>();

        private static Type _infoManagerType;
        private static Type _infoMessageType;
        private static Type _colorType;
        private static MethodInfo _displayMessageMethod;
        private static bool _typesResolved;
        private static bool _styleRegistered;

        public static void Show(string text)
        {
            string cleanMessage = Prefix + text;
            string formattedMessage = "<span style=\"Antigravity.Default\">" + Prefix.TrimEnd() + "</span> " + text;

            TryDisplay(formattedMessage);
            LogSink.Always(cleanMessage);
        }

        public static void Show(string text, float? r, float? g = null, float? b = null)
        {
            Show(text);
        }

        public static void Error(string text)
        {
            string cleanMessage = Prefix + text;
            string formattedMessage = "<span style=\"Antigravity.Default\">" + Prefix.TrimEnd() + "</span> " + text;

            TryDisplay(formattedMessage);
            LogSink.Warn(cleanMessage);
        }

        public static void Error(string text, float? r, float? g = null, float? b = null)
        {
            Error(text);
        }

        public static void ShowTimer(long elapsedMs)
        {
            Show("Timer: " + FormatTimer(elapsedMs));
        }

        public static string FormatTimer(long elapsedMs)
        {
            if (elapsedMs < 1000)
            {
                return elapsedMs + " ms";
            }

            double seconds = elapsedMs / 1000.0;
            return seconds.ToString("0.#", CultureInfo.InvariantCulture) + "s";
        }

        public static void FlushPending()
        {
            while (PendingNotices.TryDequeue(out string message))
            {
                TryDisplayDirect(message);
            }
        }

        private static void TryDisplay(string formattedMessage)
        {
            PendingNotices.Enqueue(formattedMessage);
        }

        private static Type FindType(string fullName)
        {
            Type t = Type.GetType(fullName, false);
            if (t != null) return t;

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = asm.GetType(fullName, false);
                    if (t != null) return t;
                }
                catch
                {
                }
            }
            return null;
        }

        private static void EnsureTypes()
        {
            if (!_typesResolved)
            {
                try
                {
                    _infoManagerType = FindType("TaleWorlds.Library.InformationManager");
                    _infoMessageType = FindType("TaleWorlds.Library.InformationMessage");
                    _colorType = FindType("TaleWorlds.Library.Color");
                    if (_infoManagerType != null)
                    {
                        _displayMessageMethod = _infoManagerType.GetMethod(
                            "DisplayMessage", BindingFlags.Public | BindingFlags.Static);
                    }
                }
                catch
                {
                }
                finally
                {
                    _typesResolved = true;
                }
            }

            EnsureAntigravityStyle();
        }

        private static void EnsureAntigravityStyle()
        {
            if (_styleRegistered)
            {
                return;
            }

            try
            {
                Type uiResType = FindType("TaleWorlds.Engine.GauntletUI.UIResourceManager");
                if (uiResType == null) return;

                PropertyInfo factoryProp = uiResType.GetProperty("BrushFactory", BindingFlags.Public | BindingFlags.Static);
                if (factoryProp == null) return;
                object factory = factoryProp.GetValue(null);
                if (factory == null) return;

                MethodInfo getBrushMethod = factory.GetType().GetMethod("GetBrush", new[] { typeof(string) });
                if (getBrushMethod == null) return;

                object chatBrush = getBrushMethod.Invoke(factory, new object[] { "ChatLog.Text" });
                if (chatBrush == null) return;

                Type brushType = chatBrush.GetType();
                PropertyInfo layersProp = brushType.GetProperty("Layers");
                object layers = layersProp?.GetValue(chatBrush);

                Type styleType = FindType("TaleWorlds.GauntletUI.Style");
                if (styleType == null || layers == null) return;

                ConstructorInfo[] ctors = styleType.GetConstructors();
                if (ctors.Length == 0) return;
                ConstructorInfo styleCtor = ctors[0];

                MethodInfo getStyleMethod = brushType.GetMethod("GetStyle", new[] { typeof(string) });
                MethodInfo removeStyleMethod = brushType.GetMethod("RemoveStyle", new[] { typeof(string) });
                MethodInfo addStyleMethod = brushType.GetMethod("AddStyle", new[] { styleType });
                if (addStyleMethod == null) return;

                const string styleName = "Antigravity.Default";
                if (getStyleMethod != null && removeStyleMethod != null)
                {
                    object existing = getStyleMethod.Invoke(chatBrush, new object[] { styleName });
                    if (existing != null)
                    {
                        removeStyleMethod.Invoke(chatBrush, new object[] { styleName });
                    }
                }

                object style = styleCtor.Invoke(new[] { layers });
                object defaultStyle = brushType.GetProperty("DefaultStyle")?.GetValue(chatBrush);
                PropertyInfo defaultStyleProp = styleType.GetProperty("DefaultStyle");
                if (defaultStyle != null && defaultStyleProp != null)
                {
                    defaultStyleProp.SetValue(style, defaultStyle);
                }

                PropertyInfo nameProp = styleType.GetProperty("Name");
                nameProp?.SetValue(style, styleName);

                if (_colorType != null)
                {
                    object violetColor = Activator.CreateInstance(_colorType, new object[] { VioletR, VioletG, VioletB, 1f });
                    FieldInfo fieldColor = styleType.GetField("_fontColor", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo fieldChanged = styleType.GetField("_isFontColorChanged", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldColor != null && fieldChanged != null)
                    {
                        fieldColor.SetValue(style, violetColor);
                        fieldChanged.SetValue(style, true);
                    }
                    PropertyInfo fontColorProp = styleType.GetProperty("FontColor");
                    if (fontColorProp != null)
                    {
                        try { fontColorProp.SetValue(style, violetColor); } catch { }
                    }
                }

                addStyleMethod.Invoke(chatBrush, new[] { style });
                _styleRegistered = true;
            }
            catch
            {
                // UIResourceManager may not be initialized yet during early load.
            }
        }

        private static void TryDisplayDirect(string message)
        {
            try
            {
                EnsureTypes();
                if (_infoManagerType == null || _infoMessageType == null)
                {
                    return;
                }

                object msg;
                if (_colorType != null)
                {
                    // Body text is pure white (#FFFFFFFF), prefix is violet via embedded rich text style.
                    object whiteColor = Activator.CreateInstance(
                        _colorType, new object[] { 1f, 1f, 1f, 1f });
                    msg = Activator.CreateInstance(
                        _infoMessageType, new object[] { message, whiteColor });
                }
                else
                {
                    msg = Activator.CreateInstance(_infoMessageType, new object[] { message });
                }

                if (_displayMessageMethod != null)
                {
                    _displayMessageMethod.Invoke(null, new[] { msg });
                }
                else
                {
                    _infoManagerType.InvokeMember("DisplayMessage",
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                        null, null, new[] { msg });
                }
            }
            catch
            {
                // Game UI not ready (early load / tests).
            }
        }
    }
}
