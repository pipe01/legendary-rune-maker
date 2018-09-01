using Legendary_Rune_Maker.Controls;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Game;
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
    /// Interaction logic for PickChampionDialog.xaml
    /// </summary>
    public partial class PickChampionDialog : Window
    {
        public PickChampionDialog()
        {
            InitializeComponent();

            Available.IsEnabled = GameState.CanUpload;
            this.DataContext = this;
        }

        public bool ShowNoChampion { get; set; } = true;

        public Champion SelectedChampion { get; private set; }

        public ObservableCollection<Champion> ChampionList { get; set; } = new ObservableCollection<Champion>();

        private async void Window_Initialized(object sender, EventArgs e)
        {
            LoadAllChampions();
        }

        private void Champion_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SelectedChampion = ((ChampionImageControl)sender).Champion;

            this.DialogResult = true;
            this.Close();
        }

        public static (bool Success, Champion Selected) PickChampion(bool ban = false)
        {
            var win = new PickChampionDialog();

            if (ban)
                win.BackImage.ImageSource = (ImageSource)Application.Current.FindResource("BgRed");

            if (win.ShowDialog() != true)
                return (false, null);

            return (true, win.SelectedChampion);
        }

        private async void Available_Checked(object sender, RoutedEventArgs e)
        {
            var availableIds = (await LCU.NET.Plugins.LoL.Champions.GetOwnedChampionsMinimal()).Select(o => o.id);
            
            foreach (var item in ChampionList.Where(o => o != null && !availableIds.Contains(o.ID)).ToArray())
            {
                ChampionList.Remove(item);
            }
        }

        private async void Available_Unchecked(object sender, RoutedEventArgs e)
        {
            await LoadAllChampions();
        }

        private async Task LoadAllChampions()
        {
            ChampionList.Clear();

            if (ShowNoChampion)
                ChampionList.Add(null);

            foreach (var item in await Riot.GetChampions())
            {
                ChampionList.Add(item);
            }
        }
    }
}
