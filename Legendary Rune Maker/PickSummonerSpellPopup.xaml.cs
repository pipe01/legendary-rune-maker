using Legendary_Rune_Maker.Data;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Legendary_Rune_Maker
{
    /// <summary>
    /// Interaction logic for PickSummonerSpellPopup.xaml
    /// </summary>
    public partial class PickSummonerSpellPopup : Window
    {
        private struct IHateWpf
        {
            public SummonerSpell Spell { get; set; }
            public bool Selected { get; set; }

            public IHateWpf(SummonerSpell spell, bool selected)
            {
                this.Spell = spell;
                this.Selected = selected;
            }
        }

        public ObservableCollection<SummonerSpell> Spells { get; set; } = new ObservableCollection<SummonerSpell>();

        public int[] SpellWhitelist { get; set; } = new[] { 21, 1, 14, 3, 4, 6, 7, 13, 11, 12, 32 };
        public SummonerSpell SelectedSpell { get; private set; }
        
        public PickSummonerSpellPopup()
        {
            InitializeComponent();
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            foreach (var item in await Riot.GetSummonerSpells())
            {
                if (SpellWhitelist.Any(o => o == item.ID))
                    List.Items.Add(new IHateWpf(item, item == SelectedSpell));
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            try
            {
                this.Close();
            }
            catch
            {
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var mousePos = this.PointToScreen(Mouse.GetPosition(this));

            this.Left = mousePos.X - this.Width / 2;
            this.Top = mousePos.Y - this.Height / 2;
        }

        private void SummonerSpellControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.SelectedSpell = ((IHateWpf)List.SelectedItem).Spell;
            this.Close();
        }

        public static async Task<SummonerSpell> SelectSpell(SummonerSpell selectedSpell = null, int[] allowedSpells = null)
        {
            var win = new PickSummonerSpellPopup();

            if (allowedSpells != null)
                win.SpellWhitelist = allowedSpells;

            if (selectedSpell != null)
                win.SelectedSpell = selectedSpell;

            var ev = new TaskCompletionSource<bool>();

            win.Closed += (_, __) => ev.SetResult(true);
            win.Show();

            await ev.Task;
            return win.SelectedSpell;
        }
    }
}
