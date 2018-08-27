using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Legendary_Rune_Maker
{
    /// <summary>
    /// Interaction logic for LoLButton.xaml
    /// </summary>
    [ContentProperty("AdditionalContent")]
    public partial class LoLButton : UserControl
    {
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(LoLButton));
        
        public object AdditionalContent
        {
            get { return GetValue(AdditionalContentProperty); }
            set { SetValue(AdditionalContentProperty, value); }
        }
        public static readonly DependencyProperty AdditionalContentProperty = DependencyProperty.Register("AdditionalContent", typeof(object), typeof(LoLButton));
        
        public ImageSource Side
        {
            get { return (ImageSource)GetValue(SideProperty); }
            set { SetValue(SideProperty, value); }
        }
        public static readonly DependencyProperty SideProperty = DependencyProperty.Register("Side", typeof(ImageSource), typeof(LoLButton));
        
        public ImageSource Right
        {
            get { return (ImageSource)GetValue(RightProperty); }
            set { SetValue(RightProperty, value); }
        }
        public static readonly DependencyProperty RightProperty = DependencyProperty.Register("Right", typeof(ImageSource), typeof(LoLButton));
        
        public ImageSource Fill
        {
            get { return (ImageSource)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }
        public static readonly DependencyProperty FillProperty = DependencyProperty.Register("Fill", typeof(ImageSource), typeof(LoLButton));

        public event EventHandler Click;

        public LoLButton()
        {
            InitializeComponent();
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
            => SetImages("Highlighted");

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
            => SetImages("Normal");

        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
            => SetImages(IsEnabled ? "Normal" : "Disabled");

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) SetImages("Pressed");
        }

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                SetImages("Highlighted");
                Click?.Invoke(sender, EventArgs.Empty);
            }
        }

        private void SetImages(string phase)
        {
            Side = Application.Current.FindResource(phase + "Side") as ImageSource;
            Fill = Application.Current.FindResource(phase + "Fill") as ImageSource;
            Right = Application.Current.FindResource(phase + "Right") as ImageSource;

            if (phase == "Pressed" || phase == "Disabled")
                TextLabel.Foreground = Application.Current.FindResource("Pressed") as Brush;
            else
                TextLabel.Foreground = Application.Current.FindResource("Normal") as Brush;
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            if (IsEnabled)
                SetImages("Normal");
            else
                SetImages("Disabled");
        }
    }

    public class UppercaseConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as string)?.ToUpper();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => new UppercaseConverter();
    }
}
