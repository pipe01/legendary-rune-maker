using LoL_Rune_Maker.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LoL_Rune_Maker
{
    /// <summary>
    /// Interaction logic for RuneSlotControl.xaml
    /// </summary>
    public partial class RuneSlotControl : UserControl
    {
        public double SecondarySlotsHeight
        {
            get => (double)GetValue(SecondarySlotsHeightProperty);
            set => SetValue(SecondarySlotsHeightProperty, value);
        }
        public static readonly DependencyProperty SecondarySlotsHeightProperty =
            DependencyProperty.Register("SecondarySlotsHeight", typeof(double), typeof(RuneTreeControl), new PropertyMetadata(40.0));

        public event EventHandler<Rune> SelectedRune;

        private RuneSlot Slot;
        private IList<GrayscaleImageControl> Runes = new List<GrayscaleImageControl>();

        public RuneSlotControl(RuneSlot slot, bool first)
        {
            this.Slot = slot;

            InitializeComponent();

            if (first)
                this.Height = 65;
            else
                this.Height = SecondarySlotsHeight;

            int col = 0;
            foreach (var rune in slot.Runes)
            {
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition());

                var image = new GrayscaleImageControl(Riot.ImageEndpoint + rune.IconURL);
                image.MouseDown += Image_MouseDown;
                image.Tag = rune;

                if (first)
                    image.Height = 65;

                MainGrid.Children.Add(image);
                Runes.Add(image);
                Grid.SetColumn(image, col++);
            }
        }

        private void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            foreach (var item in Runes)
            {
                if (item != (GrayscaleImageControl)sender)
                    item.Selected = false;
            }

            SelectedRune?.Invoke(this, ((Control)sender).Tag as Rune);
        }
    }
}
