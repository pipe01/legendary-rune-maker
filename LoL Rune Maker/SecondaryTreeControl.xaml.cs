using LoL_Rune_Maker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LoL_Rune_Maker
{
    /// <summary>
    /// Interaction logic for SecondaryTreeControl.xaml
    /// </summary>
    public partial class SecondaryTreeControl : UserControl
    {
        public SecondaryTreeControl()
        {
            InitializeComponent();
        }

        public Rune[] Selection => SelectedRunes.Select(o => o.Rune).ToArray();

        private IList<SRune> SelectedRunes = new List<SRune>();
        private int CurrentTree;

        public async Task SetValidTrees(int[] trees)
        {
            await Picker.SetIDs(trees);

            if (!trees.Contains(CurrentTree))
            {
                Picker.SelectedTree = trees[0];
                SetTree((await Riot.GetRuneTreesByID())[trees[0]]);
            }
        }

        public void SetTree(RuneTree tree)
        {
            CurrentTree = tree.ID;
            TreeGrid.Children.Clear();
            TreeGrid.RowDefinitions.Clear();

            int row = 0;
            foreach (var slot in tree.Slots.Skip(1))
            {
                TreeGrid.RowDefinitions.Add(new RowDefinition());

                Grid slotGrid = new Grid();
                slotGrid.Height = 40;
                TreeGrid.Children.Add(slotGrid);
                Grid.SetRow(slotGrid, row++);

                int col = 0;
                foreach (var rune in slot.Runes)
                {
                    slotGrid.ColumnDefinitions.Add(new ColumnDefinition());

                    var runeControl = new GrayscaleImageControl(Riot.ImageEndpoint + rune.IconURL);
                    runeControl.MouseDown += RuneControl_MouseDown;
                    runeControl.Tag = rune;

                    slotGrid.Children.Add(runeControl);
                    Grid.SetColumn(runeControl, col++);
                }
            }
        }

        private void RuneControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var el = (GrayscaleImageControl)sender;
            int col = Grid.GetColumn(el);
            int row = Grid.GetRow((UIElement)el.Parent);

            if (SelectedRunes.Any(o => o.Control == el))
                return;

            var sameRow = SelectedRunes.SingleOrDefault(o => o.Row == row);
            if (!sameRow.Equals(default(SRune)))
            {
                SelectedRunes.Remove(sameRow);
                sameRow.Control.Selected = false;
            }

            if (SelectedRunes.Count >= 2)
            {
                var first = SelectedRunes[0];
                SelectedRunes.RemoveAt(0);

                first.Control.Selected = false;
            }

            SelectedRunes.Add(new SRune
            {
                Column = col,
                Row = row,
                Rune = (Rune)el.Tag,
                Control = el
            });
        }

        private async void Picker_SelectionChanged(object sender, EventArgs e)
        {
            SelectedRunes.Clear();
            SetTree((await Riot.GetRuneTreesByID())[Picker.SelectedTree]);
        }

        private struct SRune
        {
            public int Column, Row;
            public Rune Rune;
            public GrayscaleImageControl Control;
        }
    }
}
