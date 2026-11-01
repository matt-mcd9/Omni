using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Omni.Data;

namespace Omni.Pages
{
    public partial class SettingsPage : UserControl
    {
        // Custom background until the user has picked their own colours
        private static readonly Color DefaultCustomBackground = Color.FromRgb(0x1E, 0x3A, 0x5F);

        // Suppresses Theme_Checked while the page selects a tile itself
        private bool _syncing;
        // Saving to disk is debounced so dragging a slider doesn't write every tick
        private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
        
        // Handles database work; one instance is reused for the life of the page.
        private readonly OmniService _service = new();

        private string CustomThemeValue => ThemeManager.Custom(BackgroundPicker.Color, TextPicker.Color);

        public SettingsPage()
        {
            InitializeComponent();

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
            
            // Save the chosen tile (Day / Night / Custom) to the database.
            SaveThemeToDatabase();
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

            // Save custom colours too. This runs on the 400ms debounce timer, so
            // dragging a colour slider writes once when you stop, not on every tick.
            SaveThemeToDatabase();
        }
        
        // Writes the theme that's currently applied to the database for the active user.
        // Uses ThemeManager.CurrentTheme (not the raw value) because Apply() cleans the
        // value up first, e.g. an invalid custom colour falls back to "Dark".

        private void SaveThemeToDatabase()
        {
            // Safety check: CurrentUser is set in App.OnStartup, but don't crash if it isn't.
            if (App.CurrentUser is null) return;
                
            _service.UpdateTheme(App.CurrentUser.UserId, ThemeManager.CurrentTheme);
                
            // Keep the in-memory copy in sync, since it doesn't reload from the DB by itself.
            App.CurrentUser.Theme = ThemeManager.CurrentTheme;
                
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
