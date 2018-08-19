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

        private IList<SRune> _SelectedRunes = new List<SRune>();
        public Rune[] SelectedRunes => _SelectedRunes.Select(o => o.Rune).ToArray();

        public RuneTree SelectedTree { get; private set; }

        public event EventHandler SelectedRunesChanged;
        
        public async Task SetValidTrees(int[] trees)
        {
            await Picker.SetIDs(trees);

            if (!trees.Contains(SelectedTree.ID))
            {
                Picker.SelectedTree = trees[0];
                SetTree((await Riot.GetRuneTreesByID())[trees[0]]);
            }
        }

        public void SetTree(RuneTree tree)
        {
            SelectedTree = tree;
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

                    var runeControl = new GrayscaleImageControl(rune);
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

            if (_SelectedRunes.Any(o => o.Control == el))
                return;

            var sameRow = _SelectedRunes.SingleOrDefault(o => o.Row == row);
            if (!sameRow.Equals(default(SRune)))
            {
                _SelectedRunes.Remove(sameRow);
                sameRow.Control.Selected = false;
            }

            if (_SelectedRunes.Count >= 2)
            {
                var first = _SelectedRunes[0];
                _SelectedRunes.RemoveAt(0);

                first.Control.Selected = false;
            }

            _SelectedRunes.Add(new SRune
            {
                Column = col,
                Row = row,
                Rune = (Rune)el.Tag,
                Control = el
            });

            SelectedRunesChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void Picker_SelectionChanged(object sender, EventArgs e)
        {
            _SelectedRunes.Clear();
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
