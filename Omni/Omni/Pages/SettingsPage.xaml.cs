using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Omni.Pages
{
    public partial class SettingsPage : UserControl
    {
        // Custom background until the user has picked their own colours
        private static readonly Color DefaultCustomBackground = Color.FromRgb(0x1E, 0x3A, 0x5F);

        // Placeholder: choosing a user does nothing yet. Once the database work is
        // merged, load the real users here and switch the current user on selection.
        private static readonly string[] PlaceholderUsers = { "User 1", "User 2" };

        // Suppresses Theme_Checked while the page selects a tile itself
        private bool _syncing;
        // Saving to disk is debounced so dragging a slider doesn't write every tick
        private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };

        private string CustomThemeValue => ThemeManager.Custom(BackgroundPicker.Color, TextPicker.Color);

        public SettingsPage()
        {
            InitializeComponent();

            UserPicker.ItemsSource = PlaceholderUsers;
            UserPicker.SelectedIndex = 0;

            _saveTimer.Tick += (_, _) => FlushSave();
            // Flush a pending save when leaving the page
            Unloaded += (_, _) => { if (_saveTimer.IsEnabled) FlushSave(); };

            if (ThemeManager.LastCustom is { } custom)
            {
                BackgroundPicker.Color = custom.Background;
                TextPicker.Color = custom.Text;
            }
            else
            {
                BackgroundPicker.Color = DefaultCustomBackground;
                TextPicker.Color = ThemeManager.ContrastText(DefaultCustomBackground);
            }
            UpdateCustomPreview();

            string theme = ThemeManager.CurrentTheme;
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

            // Applying below saves right away, so a pending save has nothing left to do
            _saveTimer.Stop();

            CustomPanel.Visibility = sender == CustomTheme ? Visibility.Visible : Visibility.Collapsed;

            if (sender == DayTheme) ThemeManager.Apply(ThemeManager.Light);
            else if (sender == NightTheme) ThemeManager.Apply(ThemeManager.Dark);
            else ThemeManager.Apply(CustomThemeValue);
        }

        private void CustomColor_Changed(object? sender, EventArgs e)
        {
            UpdateCustomPreview();
            ThemeManager.Apply(CustomThemeValue, save: false);
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        // Only writes what is already applied, so it can never change the active theme
        private void FlushSave()
        {
            _saveTimer.Stop();
            ThemeManager.SavePending();
        }

        private void UpdateCustomPreview()
        {
            Color background = BackgroundPicker.Color;
            Color text = TextPicker.Color;

            CustomTilePreview.Background = new SolidColorBrush(background);
            CustomTileText.Foreground = new SolidColorBrush(ThemeManager.ReadableText(background, text));
            ContrastWarning.Visibility = ThemeManager.IsReadable(background, text)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }
}
