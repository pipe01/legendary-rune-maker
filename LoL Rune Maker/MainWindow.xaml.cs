using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.Plugins.LoL;
using LoL_Rune_Maker.Data;
using LoL_Rune_Maker.Game;
using Newtonsoft.Json;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Application.Current.MainWindow = this;

            InitializeComponent();
        }

        private Rune[] SelectedRunes => Tree.SelectedPrimary.Concat(Tree.SelectedSecondary).Where(o => o != null).ToArray();

        private RunePage Page => new RunePage(SelectedRunes.Select(o => o.ID).ToArray(), Tree.PrimaryTree.ID, Tree.SecondaryTree.ID, SelectedChampion, SelectedPosition);

        private int SelectedChampion;
        private Position SelectedPosition;

        private async void Window_Initialized(object sender, EventArgs e)
        {
            ChampSelectDetector.Init();

            foreach (var item in await Riot.GetChampions())
            {
                ChampionDD.Items.Add(item.Name);
            }

            foreach (var item in new[] { "Top", "Jungle", "Mid", "Bottom", "Support" })
            {
                PositionDD.Items.Add(item);
            }

            this.Show();
        }
        
        private async void Upload_Click(object sender, EventArgs e)
        {
            await Page.UploadToClient();
        }

        private void Tree_SelectedRunesChanged(object sender, EventArgs e)
        {
            RefreshAndSave();
        }

        private void RefreshAndSave()
        {
            Upload.IsEnabled = SelectedRunes.Length == 6 && SelectedChampion != 0 && SelectedPosition != 0;

            if (Upload.IsEnabled)
                SaveRunePageToBook();
        }

        private void PositionDD_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedPosition = (Position)PositionDD.SelectedIndex;
            PositionImage.Source = Application.Current.FindResource(PositionDD.SelectedItem as string) as ImageSource;

            UpdateRunePageFromRuneBook();
        }

        private async void ChampionDD_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var champs = await Riot.GetChampions();
            var champ = champs[ChampionDD.SelectedIndex];

            SelectedChampion = champ.ID;
            ChampionImage.Source = await ImageCache.Instance.Get(champ.ImageURL);

            UpdateRunePageFromRuneBook();
        }

        private void UpdateRunePageFromRuneBook()
        {
            var page = RuneBook.Instance.Get(SelectedChampion, SelectedPosition);

            if (page != null)
            {
                Tree.PrimaryTree = Riot.GetRuneTreesByID()[page.PrimaryTree];
                Tree.SecondaryTree = Riot.GetRuneTreesByID()[page.SecondaryTree];

                var allRunes = Riot.GetAllRunes();
                var runes = page.RuneIDs.Select(o => allRunes[o]);

                Tree.SelectedPrimary = runes.Take(4).ToArray();
                Tree.SelectedSecondary = runes.Skip(4).Take(2).ToArray();

                RefreshAndSave();
            }
            else
            {
                Tree.Clear();
            }
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            Tree.Clear();

            RuneBook.Instance.Remove(SelectedChampion, SelectedPosition);
        }

        private void SaveRunePageToBook()
        {
            RuneBook.Instance.Remove(SelectedChampion, SelectedPosition);
                        
            if (SelectedChampion != 0 && SelectedPosition != 0)
                RuneBook.Instance.Add(this.Page);
        }
    }
}
