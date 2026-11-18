using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Omni.Data;
using Omni.Models;

namespace Omni
{
    // First window on start-up: pick who is using Omni, or click + to add a user.
    // With no users in the database only the + is shown; the name is asked for after clicking it.
    public partial class UserSelectWindow : Window
    {
        // Avatar colours, picked by user ID so each user keeps the same one
        private static readonly Color[] AvatarColors =
        {
            Color.FromRgb(0x3B, 0x82, 0xF6), Color.FromRgb(0x8B, 0x5C, 0xF6), Color.FromRgb(0xEC, 0x48, 0x99),
            Color.FromRgb(0xF5, 0x9E, 0x0B), Color.FromRgb(0x10, 0xB9, 0x81), Color.FromRgb(0xEF, 0x44, 0x44),
            Color.FromRgb(0x14, 0xB8, 0xA6), Color.FromRgb(0x63, 0x66, 0xF1),
        };

        private readonly OmniService _service = new();
        private bool _hasUsers;

        // Raised once a user has been picked or created
        public event Action<UserProfile>? UserChosen;

        public UserSelectWindow()
        {
            InitializeComponent();
            ShowUsers();
        }

        // ----- Pick a user -----

        private void ShowUsers()
        {
            var users = _service.GetAllUsers().OrderBy(u => u.UserId).ToList();
            _hasUsers = users.Count > 0;

            TitleText.Text = "Who's using Omni?";
            HintText.Text = _hasUsers ? "Choose a user, or add a new one." : "Add a user to get started.";

            UserTiles.Children.Clear();
            foreach (var user in users)
                UserTiles.Children.Add(UserTile(user));
            UserTiles.Children.Add(AddTile());

            CreateView.Visibility = Visibility.Collapsed;
            PickView.Visibility = Visibility.Visible;
        }

        // A coloured square with the first letter of the name
        private Button UserTile(UserProfile user)
        {
            var avatar = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(AvatarColors[Math.Abs(user.UserId) % AvatarColors.Length]),
                Effect = new DropShadowEffect { BlurRadius = 12, ShadowDepth = 3, Opacity = 0.45 },
                Child = new TextBlock
                {
                    Text = Initial(user.Username),
                    Foreground = Brushes.White,
                    FontSize = 30,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
            return Tile(avatar, user.Username, $"Continue as {user.Username}", (_, _) => UserChosen?.Invoke(user));
        }

        // A large plus on its own, like the add-account button in game launchers
        private Button AddTile()
        {
            var plus = new Border
            {
                Background = Brushes.Transparent,
                Child = new TextBlock
                {
                    Text = "",
                    FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                    FontSize = 40,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
            ((TextBlock)plus.Child).SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            return Tile(plus, "Add user", "Add a new user", (_, _) => ShowCreate());
        }

        private Button Tile(UIElement content, string name, string toolTip, RoutedEventHandler click)
        {
            var tile = new Button
            {
                Style = (Style)FindResource("UserTileStyle"),
                Content = content,
                Tag = name,
                ToolTip = toolTip,
            };
            tile.Click += click;
            return tile;
        }

        // The first letter, as a whole character even for emoji or other non-Latin names
        private static string Initial(string name)
        {
            string trimmed = name.Trim();
            return trimmed.Length == 0 ? "?" : StringInfo.GetNextTextElement(trimmed).ToUpper(CultureInfo.CurrentCulture);
        }

        // ----- Add a user -----

        private void ShowCreate()
        {
            TitleText.Text = "Add a user";
            HintText.Text = "Enter a name for the new user.";

            NameBox.Text = "";
            NameError.Visibility = Visibility.Collapsed;
            PickView.Visibility = Visibility.Collapsed;
            CreateView.Visibility = Visibility.Visible;

            // Focus once the box is visible and laid out
            Dispatcher.BeginInvoke(() => NameBox.Focus(), DispatcherPriority.Input);
        }

        private void CreateUser()
        {
            string name = NameBox.Text.Trim();
            if (name.Length == 0)
            {
                ShowNameError("Enter a name.");
                return;
            }

            var user = _service.CreateUser(name);
            if (user is null)
            {
                ShowNameError($"There's already a user called \"{name}\".");
                return;
            }

            UserChosen?.Invoke(user);
        }

        private void ShowNameError(string message)
        {
            NameError.Text = message;
            NameError.Visibility = Visibility.Visible;
            NameBox.Focus();
        }

        private void Create_Click(object sender, RoutedEventArgs e) => CreateUser();

        private void Back_Click(object sender, RoutedEventArgs e) => ShowUsers();

        private void NameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) CreateUser();
            else if (e.Key == Key.Escape) ShowUsers();
        }

        private void NameBox_TextChanged(object sender, TextChangedEventArgs e) =>
            NameError.Visibility = Visibility.Collapsed;

        // ----- Window -----

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
