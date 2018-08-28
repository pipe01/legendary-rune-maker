using Legendary_Rune_Maker.Controls;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Properties;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for AutomationWindow.xaml
    /// </summary>
    public partial class AutomationWindow : Window
    {
        public AutomationWindow()
        {
            InitializeComponent();
        }

        private ChampionPickerControl[] Picks, Bans;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Config.Default.Save();
        }

        private void PickBan_Changed(object sender, EventArgs e)
        {
            var picker = (ChampionPickerControl)sender;
            bool ban = picker.Name.StartsWith("Ban");
            int n = int.Parse(picker.Name.Substring(picker.Name.Length - 1)) - 1;

            var dic = ban ? Config.Default.BanChampions : Config.Default.PickChampions;
            var key = dic.Keys.ElementAt(n);

            dic[key] = picker.Champion?.ID ?? 0;
        }
        
        private async void Window_Initialized(object sender, EventArgs e)
        {
            Picks = new[] { Pick1, Pick2, Pick3, Pick4, Pick5, Pick6 };
            Bans = new[] { Ban1, Ban2, Ban3, Ban4, Ban5, Ban6 };

            var champs = await Riot.GetChampions();

            int i = 0;
            foreach (var item in Config.Default.PickChampions)
            {
                Picks[i++].Champion = champs.SingleOrDefault(o => o.ID == item.Value);
            }

            i = 0;
            foreach (var item in Config.Default.BanChampions)
            {
                Bans[i++].Champion = champs.SingleOrDefault(o => o.ID == item.Value);
            }
        }
    }
}
