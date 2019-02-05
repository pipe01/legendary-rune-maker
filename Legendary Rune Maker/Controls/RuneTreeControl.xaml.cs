using Legendary_Rune_Maker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Legendary_Rune_Maker.Controls
{
    /// <summary>
    /// Interaction logic for RuneTreeControl.xaml
    /// </summary>
    public partial class RuneTreeControl : UserControl
    {
        private enum Tree
        {
            Primary,
            Secondary
        }

        public RuneTreeControl()
        {
            InitializeComponent();
        }

        private TreeStructure _PrimaryTree;
        public TreeStructure PrimaryTree
        {
            get => _PrimaryTree;
            set
            {
                bool changed = _PrimaryTree != value;
                _PrimaryTree = value;

                if (changed)
                    SetTree(value, Tree.Primary);
            }
        }

        private TreeStructure _SecondaryTree;
        public TreeStructure SecondaryTree
        {
            get => _SecondaryTree;
            set
            {
                bool changed = _SecondaryTree != value;
                _SecondaryTree = value;

                if (changed)
                    SetTree(value, Tree.Secondary);
            }
        }

        private Rune[] _SelectedPrimary;
        public Rune[] SelectedPrimary
        {
            get => _SelectedPrimary;
            set
            {
                _SelectedPrimary = value;
                SetSelected(value, false);
            }
        }

        private Rune[] _SelectedSecondary;
        public Rune[] SelectedSecondary
        {
            get => _SelectedSecondary;
            set
            {
                _SelectedSecondary = value;
                SetSelected(value, true);
            }
        }

        private Rune[] _SelectedStats;
        public Rune[] SelectedStats
        {
            get => _SelectedStats;
            set
            {
                if (value.Length == 0)
                    value = new Rune[3];

                _SelectedStats = value;
                
                SetSelectedStats(value);
            }
        }

        public event EventHandler SelectionChanged;

        private List<List<GrayscaleImageControl>> PrimaryControls = new List<List<GrayscaleImageControl>>();
        private List<List<GrayscaleImageControl>> SecondaryControls = new List<List<GrayscaleImageControl>>();
        private bool SettingSelection;

        private void SetTree(TreeStructure tree, Tree slot, bool setPicker = true)
        {
            if (setPicker)
            {
                (slot == Tree.Secondary ? SecondaryPicker : PrimaryPicker).SelectedTree = tree.ID;
            }

            if (slot == Tree.Primary)
            {
                int[] trees = Riot.TreeStructures.Keys.Where(o => o != tree.ID).ToArray();
                SecondaryPicker.SetIDs(trees).Wait();

                if (SecondaryPicker.SelectedTree == tree.ID)
                {
                    SetTree(Riot.TreeStructures[trees[0]], Tree.Secondary);
                }

                _PrimaryTree = tree;
                _SelectedPrimary = new Rune[4];
            }
            else if (slot == Tree.Secondary)
            {
                _SecondaryTree = tree;
                _SelectedSecondary = new Rune[2];
            }
            
            var grid = slot == Tree.Secondary ? Secondary : Primary;
            var slots = slot == Tree.Secondary ? tree.PerkSlots.Skip(1).ToArray() : tree.PerkSlots;
            var controls = slot == Tree.Secondary ? SecondaryControls : PrimaryControls;

            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            controls.Clear();

            int row = 0;
            foreach (var gslot in slots)
            {
                grid.RowDefinitions.Add(new RowDefinition());
                controls.Add(new List<GrayscaleImageControl>());

                var slotGrid = new Grid();
                grid.Children.Add(slotGrid);
                Grid.SetRow(slotGrid, row++);

                int col = 0;
                foreach (var rune in gslot)
                {
                    slotGrid.ColumnDefinitions.Add(new ColumnDefinition());

                    var runeControl = new GrayscaleImageControl(rune);
                    runeControl.SelectedChanged += RuneControl_SelectedChanged;
                    runeControl.Tag = slot == Tree.Secondary;

                    if (row != 1 || slot == Tree.Secondary)
                    {
                        runeControl.Width = runeControl.Height = 40;
                        runeControl.ShowSelector = true;
                    }

                    controls[row - 1].Add(runeControl);
                    slotGrid.Children.Add(runeControl);
                    Grid.SetColumn(runeControl, col++);
                }
            }
        }

        public void SetSelectedStats(Rune[] runes)
        {
            var cbs = new[] { Stat1, Stat2, Stat3 };

            SettingSelection = true;

            for (int i = 0; i < cbs.Length; i++)
            {
                if (i >= runes.Length || runes[i] == default)
                    cbs[i].SelectedIndex = -1;
                else
                    cbs[i].SelectedItem = runes[i];
            }

            SettingSelection = false;
        }

        private void SetSelected(Rune[] runes, bool secondary)
        {
            SettingSelection = true;

            var controls = secondary ? SecondaryControls : PrimaryControls;

            int i = 0;
            foreach (var row in controls)
            {
                foreach (var control in row)
                {
                    var grid = (Grid)control.Parent;

                    control.Selected = secondary ? runes.Any(o => o?.ID == control.Rune.ID) : control.Rune.ID == runes[i]?.ID;
                }

                i++;
            }

            SettingSelection = false;
        }

        public void Clear()
        {
            SelectedPrimary = new Rune[4];
            SelectedSecondary = new Rune[2];
            SelectedStats = new Rune[3];

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RuneControl_SelectedChanged(object sender, bool e)
        {
            if (!e || SettingSelection)
                return;

            var control = (GrayscaleImageControl)sender;
            bool secondary = (bool)control.Tag;
            int row = Grid.GetRow((UIElement)control.Parent);

            var runes = secondary ? SelectedSecondary : SelectedPrimary;

            if (secondary)
            {
                bool flag = false;

                foreach (var item in SecondaryControls[row])
                {
                    if (runes.Any(o => o?.ID == item.Rune.ID))
                    {
                        int index = runes.TakeWhile(o => o?.ID != item.Rune.ID).Count();
                        runes[index] = control.Rune;

                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    runes[1] = SelectedSecondary[0];
                    runes[0] = control.Rune;
                }
            }
            else
            {
                runes[row] = control.Rune;
            }

            if (secondary)
                SelectedSecondary = runes;
            else
                SelectedPrimary = runes;

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void UserControl_Initialized(object sender, EventArgs e)
        {
            if (MainWindow.InDesigner)
                return;

            var trees = await Riot.GetTreeStructuresAsync();

            PrimaryTree = trees.Values.First();
            SecondaryTree = trees.Values.ElementAt(1);

            await PrimaryPicker.SetIDs(trees.Keys.ToArray());

            var stats = await Riot.GetStatRuneStructureAsync();

            for (int i = 0; i < 3; i++)
            {
                ComboBox cb = new[] { Stat1, Stat2, Stat3 }[i];

                for (int j = 0; j < 3; j++)
                {
                    cb.Items.Add(stats[i][j]);
                }
            }
        }

        private void SecondaryPicker_SelectionChanged(object sender, EventArgs e)
        {
            SetTree(Riot.TreeStructures[SecondaryPicker.SelectedTree], Tree.Secondary, false);

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PrimaryPicker_SelectionChanged(object sender, EventArgs e)
        {
            SetTree(Riot.TreeStructures[PrimaryPicker.SelectedTree], Tree.Primary, false);

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetPage(RunePage page)
        {
            var trees = Riot.TreeStructures;

            PrimaryTree = trees[page.PrimaryTree];
            SecondaryTree = trees[page.SecondaryTree];
            SelectedPrimary = PrimaryTree.PerkSlots.SelectMany(o => o).Where(o => page.RuneIDs.Contains(o.ID)).ToArray();
            SelectedSecondary = SecondaryTree.PerkSlots.SelectMany(o => o).Where(o => page.RuneIDs.Contains(o.ID)).ToArray();
            
            var statRunes = Riot.StatRunes.Select(o => o.Value);
            SelectedStats = page.RuneIDs.Select(o => statRunes.SingleOrDefault(i => i.ID == o)).Where(o => o != default).ToArray();

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Stat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingSelection)
                return;

            var cb = sender as ComboBox;
            var index = Array.IndexOf(new[] { Stat1, Stat2, Stat3 }, sender);

            if (SelectedStats.Length != 3)
                Array.Resize(ref _SelectedStats, 3);

            SelectedStats[index] = (Rune)cb.SelectedItem;

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
