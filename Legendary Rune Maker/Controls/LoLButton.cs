using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Legendary_Rune_Maker.Controls
{
    public class LoLButton : ContentControl
    {
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(LoLButton));

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
        
        static LoLButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LoLButton), new FrameworkPropertyMetadata(typeof(LoLButton)));
        }

        public event EventHandler Click;

        public LoLButton()
        {
            this.MouseEnter += LoLButton_MouseEnter;
            this.MouseLeave += LoLButton_MouseLeave;
            this.MouseLeftButtonDown += LoLButton_MouseLeftButtonDown;
            this.MouseLeftButtonUp += LoLButton_MouseLeftButtonUp;
            this.IsEnabledChanged += LoLButton_IsEnabledChanged;

            if (this.IsEnabled)
                SetImages("Normal");
            else
                SetImages("Disabled");
        }

        private void LoLButton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
            => SetImages(this.IsEnabled ? "Normal" : "Disabled");

        private void LoLButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SetImages("Highlighted");
            Click?.Invoke(this, EventArgs.Empty);
        }

        private void LoLButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => SetImages("Pressed");

        private void LoLButton_MouseLeave(object sender, MouseEventArgs e)
            => SetImages("Normal");
        
        private void LoLButton_MouseEnter(object sender, MouseEventArgs e)
            => SetImages("Highlighted");

        private void SetImages(string phase)
        {
            Side = Application.Current.FindResource(phase + "Side") as ImageSource;
            Fill = Application.Current.FindResource(phase + "Fill") as ImageSource;
            Right = Application.Current.FindResource(phase + "Right") as ImageSource;

            if (phase == "Pressed" || phase == "Disabled")
                this.Foreground = Application.Current.FindResource("Pressed") as Brush;
            else
                this.Foreground = Application.Current.FindResource("Normal") as Brush;
        }
    }
}
