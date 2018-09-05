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

        public double ImageMargin
        {
            get { return (double)GetValue(ImageMarginProperty); }
            set { SetValue(ImageMarginProperty, value); }
        }
        public static readonly DependencyProperty ImageMarginProperty = DependencyProperty.Register("ImageMargin", typeof(double), typeof(SummonerSpellControl));
        
        public bool Picker
        {
            get { return (bool)GetValue(PickerProperty); }
            set { SetValue(PickerProperty, value); }
        }
        public static readonly DependencyProperty PickerProperty = DependencyProperty.Register("Picker", typeof(bool), typeof(SummonerSpellControl));
        
        public event EventHandler SpellSelected;

        private static void SelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = d as SummonerSpellControl;

            if (c.Template != null)
                c.SetSelected();
        }

        private static async void SpellChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spell = e.NewValue as SummonerSpell;
            var c = d as SummonerSpellControl;

            if (spell == null)
            {
                c.SpellImage = null;
                c.BorderThickness = new Thickness(0);
            }
            else
            {
                c.SpellImage = await ImageCache.Instance.Get(spell.ImageURL);
                c.BorderThickness = c.Selected ? new Thickness(2) : new Thickness(1);
            }
        }
        
        static SummonerSpellControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SummonerSpellControl), new FrameworkPropertyMetadata(typeof(SummonerSpellControl)));
        }

        private void SetSelected()
        {
            if (Selected)
            {
                this.BorderBrush = (Brush)this.Template.Resources["Selected"];
                this.BorderThickness = Spell == null ? new Thickness(0) : new Thickness(2);
                this.ImageMargin = 4;
            }
            else
            {
                this.BorderBrush = (Brush)this.Template.Resources["Normal"];
                this.BorderThickness = Spell == null ? new Thickness(0) : new Thickness(1);
                this.ImageMargin = 2;
            }
        }

        public SummonerSpellControl()
        {
            this.Initialized += SummonerSpellControl_Initialized;
            this.MouseLeftButtonDown += (_, e) => e.Handled = true;
            this.MouseLeftButtonUp += SummonerSpellControl_MouseLeftButtonUp;
            this.MouseRightButtonUp += SummonerSpellControl_MouseRightButtonUp;
        }

        private void SummonerSpellControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Picker)
            {
                this.Spell = null;
                SpellSelected?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void SummonerSpellControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Picker)
            {
                this.Spell = await PickSummonerSpellPopup.SelectSpell(this.Spell);
                SpellSelected?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SummonerSpellControl_Initialized(object sender, EventArgs e)
        {
            SetSelected();
        }
    }
}
