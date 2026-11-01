using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace Omni
{
   
    public static class ThemeManager
    {
        public const string Dark = "Dark";
        public const string Light = "Light";

        private static readonly Color DarkBg = Color.FromRgb(0x0B, 0x0B, 0x0B);
        private static readonly Color LightBg = Color.FromRgb(0xFF, 0xFF, 0xFF);

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Omni", "theme.txt");

        public static string CurrentTheme { get; private set; } = Dark;
        public static Color CurrentBackground { get; private set; } = DarkBg;
        public static Color CurrentText { get; private set; } = Colors.White;

        public static bool IsCustom(string theme) => theme != Dark && theme != Light;

        public static string Custom(Color background, Color text) => $"{ToHex(background)},{ToHex(text)}";

        // Readable default text colour for a background
        public static Color ContrastText(Color background) =>
            IsLight(background) ? Colors.Black : Colors.White;

        public static void LoadSaved()
        {
            string theme = Dark;
            try
            {
                if (File.Exists(SettingsPath))
                    theme = File.ReadAllText(SettingsPath).Trim();
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            Apply(theme, save: false);
        }

        public static void Apply(string theme, bool save = true)
        {
            Color bg, text;
            if (theme == Light) (bg, text) = (LightBg, Colors.Black);
            else if (theme == Dark) (bg, text) = (DarkBg, Colors.White);
            else if (TryParseCustom(theme, out bg, out text)) theme = Custom(bg, text);
            else (theme, bg, text) = (Dark, DarkBg, Colors.White);

            ApplyColors(bg, text);
            CurrentTheme = theme;
            CurrentBackground = bg;
            CurrentText = text;

            if (save) Save(theme);
        }

        private static bool TryParseCustom(string theme, out Color bg, out Color text)
        {
            text = default;
            var parts = theme.Split(',');
            if (parts.Length > 2 || !TryParseColor(parts[0], out bg)) return false;
            if (parts.Length == 1)
            {
                text = ContrastText(bg);
                return true;
            }
            return TryParseColor(parts[1], out text);
        }

        public static bool TryParseColor(string text, out Color color)
        {
            color = default;
            text = text.Trim();
            if (!text.StartsWith('#')) text = "#" + text;
            if (text.Length != 7) return false;
            try
            {
                color = (Color)ColorConverter.ConvertFromString(text);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        public static bool IsLight(Color c) =>
            (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0 > 0.55;

        // Derives every surface colour from the background, and the muted
        // text colour from the text colour, so two colours theme the whole app
        private static void ApplyColors(Color bg, Color text)
        {
            // Surfaces step darker on light backgrounds, lighter on dark ones
            double dir = IsLight(bg) ? -1 : 1;

            Set("BgBrush", bg);
            Set("SidebarBrush", Shift(bg, dir * 0.02));
            Set("HoverBrush", Shift(bg, dir * 0.07));
            Set("SelectedBrush", Shift(bg, dir * 0.11));
            Set("BorderBrush", Shift(bg, dir * 0.09));
            Set("TextBrush", text);
            Set("MutedTextBrush", Blend(text, bg, 0.6));
        }

        private static Color Blend(Color a, Color b, double amountOfA)
        {
            byte f(byte x, byte y) => (byte)Math.Round(x * amountOfA + y * (1 - amountOfA));
            return Color.FromRgb(f(a.R, b.R), f(a.G, b.G), f(a.B, b.B));
        }

        private static void Set(string key, Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            Application.Current.Resources[key] = brush;
        }

        private static Color Shift(Color c, double amount)
        {
            byte f(byte v) => (byte)Math.Clamp(v + amount * 255, 0, 255);
            return Color.FromRgb(f(c.R), f(c.G), f(c.B));
        }

        private static void Save(string theme)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, theme);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
