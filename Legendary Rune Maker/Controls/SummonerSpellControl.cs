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
    public class SummonerSpellControl : Control
    {
        public ImageSource SpellImage
        {
            get { return (ImageSource)GetValue(SpellImageProperty); }
            set { SetValue(SpellImageProperty, value); }
        }
        public static readonly DependencyProperty SpellImageProperty = DependencyProperty.Register("SpellImage", typeof(ImageSource), typeof(SummonerSpellControl));
        
        public SummonerSpell Spell
        {
            get { return (SummonerSpell)GetValue(SpellProperty); }
            set { SetValue(SpellProperty, value); }
        }
        public static readonly DependencyProperty SpellProperty = DependencyProperty.Register("Spell", typeof(SummonerSpell), typeof(SummonerSpellControl), new PropertyMetadata(SpellChanged));
        
        public bool Selected
        {
            get { return (bool)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }
        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register("Selected", typeof(bool), typeof(SummonerSpellControl), new PropertyMetadata(false, SelectedChanged));

        private static void SelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = d as SummonerSpellControl;

            if ((bool)e.NewValue)
            {
                c.BorderBrush = (LinearGradientBrush)c.Template.Resources["Selected"];
                c.BorderThickness = new Thickness(2);
            }
            else
            {
                c.BorderBrush = (LinearGradientBrush)c.Template.Resources["Normal"];
                c.BorderThickness = new Thickness(1);
            }
        }

        private static async void SpellChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spell = e.NewValue as SummonerSpell;
            var c = d as SummonerSpellControl;

            if (spell == null)
                c.SpellImage = null;
            else
                c.SpellImage = await ImageCache.Instance.Get(spell.ImageURL);
        }

        static SummonerSpellControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SummonerSpellControl), new FrameworkPropertyMetadata(typeof(SummonerSpellControl)));
        }
    }
}
