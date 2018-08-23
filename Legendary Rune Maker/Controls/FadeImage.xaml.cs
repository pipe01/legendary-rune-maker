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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            set { SetValue(StretchProperty, value); ImageA.Stretch = value; ImageB.Stretch = value; }
        }
        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register("Stretch", typeof(Stretch), typeof(FadeImage));
        
        private Image ImgA, ImgB;

        public FadeImage()
        {
            InitializeComponent();

            ImgA = ImageA;
            ImgB = ImageB;
        }
        
        private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var fadeImage = d as FadeImage;

            if (e.OldValue != e.NewValue)
            {
                fadeImage.ImgB.Source = e.NewValue as ImageSource;
                fadeImage.Switch();
            }
        }
        
        private void Switch()
        {
            var sbIn = (Storyboard)Resources["FadeIn"];
            sbIn.Begin(ImgB);

            var sbOut = (Storyboard)Resources["FadeOut"];
            sbOut.Begin(ImgA);

            var a = ImgA;
            ImgA = ImgB;
            ImgB = a;
        }
    }
}
