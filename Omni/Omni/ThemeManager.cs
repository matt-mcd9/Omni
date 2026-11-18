using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Omni.Data;

namespace Omni
{
    // Each user's theme is stored in UserProfiles.Theme as "<active theme>|<last custom colours>",
    // e.g. "Light|#1E3A5F,#FFFFFF". The part after "|" is left out until custom colours are picked.
    public static class ThemeManager
    {
        public const string Dark = "Dark";
        public const string Light = "Light";

        private static readonly Color DarkBg = Color.FromRgb(0x0B, 0x0B, 0x0B);
        private static readonly Color LightBg = Color.FromRgb(0xFF, 0xFF, 0xFF);

        // Where the theme was kept before it moved to the database; imported once, then renamed
        private static readonly string OldSettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Omni", "theme.txt");

        private const char CustomSeparator = '|';

        // Below this WCAG contrast ratio, text is shown in black or white instead
        // so a custom theme can never make the app unreadable
        private const double MinContrast = 3.0;

        // Database value as last read or written, to skip writes when nothing changed
        private static string? _savedValue;

        public static string CurrentTheme { get; private set; } = Dark;

        // The most recent custom colours, remembered while a preset is active
        public static (Color Background, Color Text)? LastCustom { get; private set; }

        public static string Custom(Color background, Color text) => $"{ToHex(background)},{ToHex(text)}";

        // Black or white, whichever is easier to read on this background
        public static Color ContrastText(Color background) =>
            ContrastRatio(background, Colors.Black) >= ContrastRatio(background, Colors.White)
                ? Colors.Black
                : Colors.White;

        public static bool IsReadable(Color background, Color text) =>
            ContrastRatio(background, text) >= MinContrast;

        // The text colour actually shown for a background and a chosen text colour
        public static Color ReadableText(Color background, Color text) =>
            IsReadable(background, text) ? text : ContrastText(background);

        // Applies the theme stored for this user. A user without custom colours
        // doesn't keep the previous user's, so they never leak between users.
        public static void Load(int userId)
        {
            string theme = Dark;
            LastCustom = null;

            string? stored = new OmniService().GetTheme(userId);
            if (!string.IsNullOrWhiteSpace(stored))
            {
                var parts = stored.Split(CustomSeparator);
                theme = parts[0].Trim();
                if (parts.Length > 1 && TryParseCustom(parts[1], out var bg, out var text))
                    LastCustom = (bg, text);
            }

            Apply(theme, save: false);
            _savedValue = StoredValue();
        }

        // Saves the current user's theme, then goes back to the default with no user loaded,
        // as on start-up. Nothing more is saved until Load is called for the next user.
        public static void Reset()
        {
            SavePending();
            _savedValue = null;
            LastCustom = null;
            Apply(Dark, save: false);
        }

        // Moves the theme from the old theme.txt into this user's profile, once.
        // The file is renamed rather than deleted, so nothing is lost if the import goes wrong.
        public static void ImportOldSettingsFile(int userId)
        {
            try
            {
                if (!File.Exists(OldSettingsPath)) return;

                // Line 1: the active theme. Line 2: the last custom colours
                var lines = File.ReadAllLines(OldSettingsPath);
                if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]))
                {
                    string value = lines[0].Trim();
                    if (lines.Length > 1 && TryParseCustom(lines[1], out _, out _))
                        value += CustomSeparator + lines[1].Trim();
                    new OmniService().SaveTheme(userId, value);
                }

                File.Move(OldSettingsPath, OldSettingsPath + ".imported", overwrite: true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        public static void Apply(string theme, bool save = true)
        {
            Color bg, text;
            if (theme == Light) (bg, text) = (LightBg, Colors.Black);
            else if (theme == Dark) (bg, text) = (DarkBg, Colors.White);
            else if (TryParseCustom(theme, out bg, out text))
            {
                theme = Custom(bg, text);
                LastCustom = (bg, text);
            }
            else (theme, bg, text) = (Dark, DarkBg, Colors.White);

            ApplyColors(bg, ReadableText(bg, text));
            CurrentTheme = theme;

            if (save) SavePending();
        }

        // Saves the current theme to the current user's profile if it differs from what is saved there
        public static void SavePending()
        {
            // Nothing has been loaded yet (no user picked), so there is no user to save for
            if (_savedValue is null) return;

            string value = StoredValue();
            if (value == _savedValue) return;

            new OmniService().SaveTheme(OmniService.CurrentUserId, value);
            _savedValue = value;
        }

        private static string StoredValue() =>
            LastCustom is { } custom
                ? $"{CurrentTheme}{CustomSeparator}{Custom(custom.Background, custom.Text)}"
                : CurrentTheme;

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

        private static double ContrastRatio(Color a, Color b)
        {
            double la = RelativeLuminance(a), lb = RelativeLuminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        private static double RelativeLuminance(Color c)
        {
            double f(byte v)
            {
                double s = v / 255.0;
                return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * f(c.R) + 0.7152 * f(c.G) + 0.0722 * f(c.B);
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
    }
}
