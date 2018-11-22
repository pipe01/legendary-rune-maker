using LCU.NET;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Pages;
using Ninject;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

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

        private NavigationService NavigationService;

        static ChampionPickerControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ChampionPickerControl), new FrameworkPropertyMetadata(typeof(ChampionPickerControl)));
        }

        public ChampionPickerControl(NavigationService navService)
        {
            this.NavigationService = navService;

            this.MouseLeftButtonDown += (_, e) => e.Handled = true;
            this.MouseLeftButtonUp += ChampionPickerControl_MouseLeftButtonUp;
            this.MouseRightButtonUp += ChampionPickerControl_MouseRightButtonUp;
        }

        private void ChampionPickerControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Champion = null;
            ChampionChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void ChampionPickerControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var champ = await PickChampionPage.PickChampion(NavigationService, App.Container.Get<ILoL>().Champions, Ban);

            if (champ.Success)
            {
                this.Champion = champ.Selected;
                ChampionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
