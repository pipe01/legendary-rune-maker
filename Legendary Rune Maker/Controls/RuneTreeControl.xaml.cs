using Legendary_Rune_Maker.Data;
using System;
using System.Collections;
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
        public RuneTreeControl()
        {
            InitializeComponent();
        }

        private RuneTree _PrimaryTree;
        public RuneTree PrimaryTree
        {
            get => _PrimaryTree;
            set
            {
                bool changed = _PrimaryTree != value;
                _PrimaryTree = value;

                if (changed)
                    SetTree(value, false);
            }
        }

        private RuneTree _SecondaryTree;
        public RuneTree SecondaryTree
        {
            get => _SecondaryTree;
            set
            {
                bool changed = _SecondaryTree != value;
                _SecondaryTree = value;

                if (changed)
                    SetTree(value, true);
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

        public event EventHandler SelectionChanged;

        private List<List<GrayscaleImageControl>> PrimaryControls = new List<List<GrayscaleImageControl>>();
        private List<List<GrayscaleImageControl>> SecondaryControls = new List<List<GrayscaleImageControl>>();
        private bool SettingSelection;

        private void SetTree(RuneTree tree, bool secondary, bool setPicker = true)
        {
            if (setPicker)
            {
                (secondary ? SecondaryPicker : PrimaryPicker).SelectedTree = tree.ID;
            }

            if (!secondary)
            {
                int[] trees = Riot.GetRuneTreesByID().Keys.Where(o => o != tree.ID).ToArray();
                SecondaryPicker.SetIDs(trees).Wait();

                if (SecondaryPicker.SelectedTree == tree.ID)
                {
                    SetTree(Riot.GetRuneTreesByID()[trees[0]], true);
                }

                _PrimaryTree = tree;
                _SelectedPrimary = new Rune[4];
            }
            else
            {
                _SecondaryTree = tree;
                _SelectedSecondary = new Rune[2];
            }

            var grid = secondary ? Secondary : Primary;
            var slots = secondary ? tree.Slots.Skip(1).ToArray() : tree.Slots;
            var controls = secondary ? SecondaryControls : PrimaryControls;

            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            controls.Clear();
            
            int row = 0;
            foreach (var slot in slots)
            {
                grid.RowDefinitions.Add(new RowDefinition());
                controls.Add(new List<GrayscaleImageControl>());

                var slotGrid = new Grid();
                grid.Children.Add(slotGrid);
                Grid.SetRow(slotGrid, row++);

                int col = 0;
                foreach (var rune in slot.Runes)
                {
                    slotGrid.ColumnDefinitions.Add(new ColumnDefinition());

                    var runeControl = new GrayscaleImageControl(rune, row == 1 && !secondary);
                    runeControl.SelectedChanged += RuneControl_SelectedChanged;
                    runeControl.Tag = secondary;

                    if (!(row == 1 && !secondary))
                        runeControl.Width = runeControl.Height = 40;

                    controls[row - 1].Add(runeControl);
                    slotGrid.Children.Add(runeControl);
                    Grid.SetColumn(runeControl, col++);
                }
            }
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
            RuneTree[] trees = await Riot.GetRuneTrees();

            PrimaryTree = trees[0];
            SecondaryTree = trees[1];

            await PrimaryPicker.SetIDs((await Riot.GetRuneTreesByIDAsync()).Keys.ToArray());
        }

        private void SecondaryPicker_SelectionChanged(object sender, EventArgs e)
        {
            SetTree(Riot.GetRuneTreesByID()[SecondaryPicker.SelectedTree], true, false);

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PrimaryPicker_SelectionChanged(object sender, EventArgs e)
        {
            SetTree(Riot.GetRuneTreesByID()[PrimaryPicker.SelectedTree], false, false);

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
