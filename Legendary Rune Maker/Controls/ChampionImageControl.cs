using Legendary_Rune_Maker.Data;
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
            await c.SetChampion(e.NewValue as Champion);
        }

        static ChampionImageControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ChampionImageControl), new FrameworkPropertyMetadata(typeof(ChampionImageControl)));
        }

        public async Task SetChampion(Champion champ)
        {
            if (champ == null)
                Source = NoChamp;
            else
                Source = await ImageCache.Instance.Get(champ.ImageURL);
        }
    }
}
