using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Omni.Controls
{
    public partial class ColorPicker : UserControl
    {
        private Color _color;
        // Stops slider / hex box updates from feeding back into each other
        private bool _syncing;

        // Raised only for user edits, not when Color is set from code
        public event EventHandler? ColorChanged;

        public ColorPicker()
        {
            InitializeComponent();
            UpdateControls();
        }

        public Color Color
        {
            get => _color;
            set { _color = value; UpdateControls(); }
        }

        private void Channel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_syncing) return;
            SetFromUser(Color.FromRgb((byte)RSlider.Value, (byte)GSlider.Value, (byte)BSlider.Value));
        }

        private void HexBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) CommitHex();
        }

        private void HexBox_LostFocus(object sender, RoutedEventArgs e) => CommitHex();

        private void CommitHex()
        {
            if (!ThemeManager.TryParseColor(HexBox.Text, out var color))
                HexBox.Text = ThemeManager.ToHex(_color); // revert invalid input
            else if (color != _color)
                SetFromUser(color);
        }

        private void SetFromUser(Color color)
        {
            _color = color;
            UpdateControls();
            ColorChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateControls()
        {
            _syncing = true;
            RSlider.Value = _color.R;
            GSlider.Value = _color.G;
            BSlider.Value = _color.B;
            HexBox.Text = ThemeManager.ToHex(_color);
            _syncing = false;
        }
    }
}
