using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Legendary_Rune_Maker.Controls
{
    /// <summary>
    /// Interaction logic for FadeImage.xaml
    /// </summary>
    public partial class FadeImage : UserControl
    {
        public ImageSource Source
        {
            get => (ImageSource)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(ImageSource), typeof(FadeImage), new PropertyMetadata(SourceChanged));

        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }
        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register("Stretch", typeof(Stretch), typeof(FadeImage), new PropertyMetadata(StretchChanged));

        private static void StretchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = d as FadeImage;

            c.ImageA.Stretch = c.ImageB.Stretch = (Stretch)e.NewValue;
        }

        public double SpeedRatio
        {
            get { return (double)GetValue(SpeedRatioProperty); }
            set { SetValue(SpeedRatioProperty, value); }
        }
        public static readonly DependencyProperty SpeedRatioProperty = DependencyProperty.Register("SpeedRatio", typeof(double), typeof(FadeImage), new PropertyMetadata(1.0));

        private Image ImgA, ImgB;
        private Storyboard FadeIn, FadeOut;

        public FadeImage()
        {
            InitializeComponent();

            ImgA = ImageA;
            ImgB = ImageB;

            FadeIn = (Storyboard)Resources["FadeIn"];
            FadeOut = (Storyboard)Resources["FadeOut"];
        }
        
        private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var fadeImage = d as FadeImage;

            if (e.OldValue != e.NewValue)
            {
                fadeImage.Switch(e.NewValue as ImageSource);
            }
        }
        
        private void Switch(ImageSource newImage)
        {
            ImgB.Source = newImage;

            FadeOut.SpeedRatio = SpeedRatio;
            FadeIn.SpeedRatio = SpeedRatio;

            FadeOut.Begin(ImgA);
            FadeIn.Begin(ImgB);
            
            var a = ImgA;
            ImgA = ImgB;
            ImgB = a;
        }
    }
}
