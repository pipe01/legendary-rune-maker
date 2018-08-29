using Legendary_Rune_Maker.Data;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Legendary_Rune_Maker.Controls
{
    public class ChampionPickerControl : Control
    {
        public Champion Champion
        {
            get => (Champion)GetValue(ChampionProperty);
            set => SetValue(ChampionProperty, value);
        }
        public static readonly DependencyProperty ChampionProperty = DependencyProperty.Register("Champion", typeof(Champion), typeof(ChampionPickerControl));
        
        public bool Ban { get; set; }
        
        public event EventHandler ChampionChanged;

        static ChampionPickerControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ChampionPickerControl), new FrameworkPropertyMetadata(typeof(ChampionPickerControl)));
        }

        public ChampionPickerControl()
        {
            this.MouseLeftButtonUp += ChampionPickerControl_MouseLeftButtonUp;
            this.MouseRightButtonUp += ChampionPickerControl_MouseRightButtonUp;
        }

        private void ChampionPickerControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Champion = null;
            ChampionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ChampionPickerControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var champ = PickChampionDialog.PickChampion(Ban);

            if (champ.Success)
            {
                this.Champion = champ.Selected;
                ChampionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
