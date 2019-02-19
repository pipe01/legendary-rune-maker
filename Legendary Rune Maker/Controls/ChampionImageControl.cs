using Legendary_Rune_Maker.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Legendary_Rune_Maker.Controls
{
    public class ChampionImageControl : Control
    {
        private static readonly ImageSource NoChamp = Application.Current.FindResource("NoChamp") as ImageSource;

        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(ImageSource), typeof(ChampionImageControl), new PropertyMetadata(NoChamp));

        public Champion Champion
        {
            get { return (Champion)GetValue(ChampionProperty); }
            set { SetValue(ChampionProperty, value); }
        }
        public static readonly DependencyProperty ChampionProperty =
            DependencyProperty.Register("Champion", typeof(Champion), typeof(ChampionImageControl), new PropertyMetadata(ChampionChanged));

        private static async void ChampionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = d as ChampionImageControl;
            var champ = e.NewValue as Champion;

            if (champ == null)
                c.Source = NoChamp;
            else
                c.Source = await ImageCache.Instance.Get(champ.ImageURL);

            c.ToolTip = champ?.Name;
        }

        static ChampionImageControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ChampionImageControl), new FrameworkPropertyMetadata(typeof(ChampionImageControl)));
        }
    }
}
