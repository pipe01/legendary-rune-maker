using LoL_Rune_Maker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

namespace LoL_Rune_Maker
{
    /// <summary>
    /// Interaction logic for RuneTree.xaml
    /// </summary>
    public partial class RuneTreeControl : UserControl
    {
        private readonly WebClient Client = new WebClient();

        private Rune[] _SelectedRunes = new Rune[4];
        public Rune[] SelectedRunes => _SelectedRunes.ToArray();

        public RuneTree SelectedTree { get; private set; }

        public event EventHandler<int> SelectedTreeChanged;
        public event EventHandler SelectedRunesChanged;

        public RuneTreeControl()
        {
            InitializeComponent();

            Picker.SelectionChanged += Picker_SelectionChanged;
        }
        
        private async void Picker_SelectionChanged(object sender, EventArgs e)
        {
            SetTree((await Riot.GetRuneTrees()).Single(o => o.ID == Picker.SelectedTree));
            SelectedTreeChanged?.Invoke(this, Picker.SelectedTree);
        }

        public async Task Initialize()
        {
            await Picker.SetIDs(new[] { 8000, 8100, 8200, 8300, 8400 });
            SetTree((await Riot.GetRuneTrees())[0]);
        }

        public void SetTree(RuneTree tree)
        {
            this.SelectedTree = tree;
            this._SelectedRunes = new Rune[4];
            TreeGrid.Children.Clear();
            TreeGrid.RowDefinitions.Clear();

            int row = 0;
            foreach (var item in SelectedTree.Slots)
            {
                TreeGrid.RowDefinitions.Add(new RowDefinition());

                var slotControl = new RuneSlotControl(item, item == SelectedTree.Slots[0]);
                slotControl.Tag = row;
                slotControl.SelectedRune += SlotControl_SelectedRune;

                TreeGrid.Children.Add(slotControl);
                Grid.SetRow(slotControl, row++);
            }
        }

        private void SlotControl_SelectedRune(object sender, Rune e)
        {
            this._SelectedRunes[(int)((Control)sender).Tag] = e;
            SelectedRunesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
