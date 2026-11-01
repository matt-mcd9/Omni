using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Omni.Pages
{
    public partial class SettingsPage : UserControl
    {
        // Custom colours used when switching to Custom from a preset
        private static readonly Color DefaultCustomBackground = Color.FromRgb(0x1E, 0x3A, 0x5F);

        // Suppresses Theme_Checked while the page selects a tile itself
        private bool _syncing;
        // Saving to disk is debounced so dragging a slider doesn't write every tick
        private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };

        private string CustomThemeValue => ThemeManager.Custom(BackgroundPicker.Color, TextPicker.Color);

        public SettingsPage()
        {
            InitializeComponent();

            _saveTimer.Tick += (_, _) => FlushSave();
            // Flush a pending save when leaving the page
            Unloaded += (_, _) => { if (_saveTimer.IsEnabled) FlushSave(); };

            string theme = ThemeManager.CurrentTheme;
            if (ThemeManager.IsCustom(theme))
            {
                BackgroundPicker.Color = ThemeManager.CurrentBackground;
                TextPicker.Color = ThemeManager.CurrentText;
            }
            else
            {
                BackgroundPicker.Color = DefaultCustomBackground;
                TextPicker.Color = ThemeManager.ContrastText(DefaultCustomBackground);
            }
            UpdateCustomTile();

            _syncing = true;
            if (theme == ThemeManager.Light) DayTheme.IsChecked = true;
            else if (theme == ThemeManager.Dark) NightTheme.IsChecked = true;
            else CustomTheme.IsChecked = true;
            CustomPanel.Visibility = CustomTheme.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            _syncing = false;
        }

        private void Theme_Checked(object sender, RoutedEventArgs e)
        {
            if (_syncing) return;

            CustomPanel.Visibility = sender == CustomTheme ? Visibility.Visible : Visibility.Collapsed;

            if (sender == DayTheme) ThemeManager.Apply(ThemeManager.Light);
            else if (sender == NightTheme) ThemeManager.Apply(ThemeManager.Dark);
            else ThemeManager.Apply(CustomThemeValue);
        }

        private void CustomColor_Changed(object? sender, EventArgs e)
        {
            UpdateCustomTile();
            ThemeManager.Apply(CustomThemeValue, save: false);
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private void FlushSave()
        {
            _saveTimer.Stop();
            ThemeManager.Apply(CustomThemeValue);
        }

        private void UpdateCustomTile()
        {
            CustomTilePreview.Background = new SolidColorBrush(BackgroundPicker.Color);
            CustomTileText.Foreground = new SolidColorBrush(TextPicker.Color);
        }
    }
}
