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
        public int[] SelectedRunes
        {
            get => _SelectedRunes.Where(o => o.Rune != null).Select(o => o.Rune.ID).ToArray();
            set
            {
                _SelectedRunes.Clear();

                var runes = Riot.GetRuneTreesByID().Values.SelectMany(o => o.Slots).SelectMany(o => o.Runes).ToDictionary(o => o.ID);

                int i = 0;
                foreach (var item in value)
                {
                    var c = Runes[i++];
                    int col = Grid.GetColumn(c);
                    int row = Grid.GetRow(c);

                    _SelectedRunes.Add(new SRune
                    {
                        Column = col,
                        Row = row,
                        Control = c,
                        Rune = runes.TryGetValue(item, out var v) ? v : null
                    });
                }

                foreach (var item in Runes)
                {
                    item.Selected = value.Contains(((Rune)item.Tag).ID);
                }
            }
        }

        public RuneTree SelectedTree { get; private set; }

        private List<GrayscaleImageControl> Runes = new List<GrayscaleImageControl>();

        public event EventHandler SelectedRunesChanged;
        
        public async Task SetValidTrees(int[] trees)
        {
            await Picker.SetIDs(trees);

            if (!trees.Contains(SelectedTree.ID))
            {
                Picker.SelectedTree = trees[0];
                SetTree((await Riot.GetRuneTreesByIDAsync())[trees[0]]);
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

                    Runes.Add(runeControl);
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
            SetTree((await Riot.GetRuneTreesByIDAsync())[Picker.SelectedTree]);
        }

        private struct SRune
        {
            public int Column, Row;
            public Rune Rune;
            public GrayscaleImageControl Control;
        }
    }
}
