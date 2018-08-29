using LCU.NET.Plugins;
using Legendary_Rune_Maker.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Legendary_Rune_Maker
{
    /// <summary>
    /// Interaction logic for PickSummonerSpellPopup.xaml
    /// </summary>
    public partial class PickSummonerSpellPopup : Window
    {
        public ObservableCollection<SummonerSpell> Spells { get; set; } = new ObservableCollection<SummonerSpell>();

        public int[] SpellWhitelist = new[] { 21, 1, 14, 3, 4, 6, 7, 13, 11, 12, 32 };

        public PickSummonerSpellPopup()
        {
            InitializeComponent();
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            foreach (var item in await Riot.GetSummonerSpells())
            {
                if (SpellWhitelist.Any(o => o == item.ID))
                    List.Items.Add(item);
            }
        }
    }
}
