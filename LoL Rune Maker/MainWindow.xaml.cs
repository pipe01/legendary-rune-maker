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

        private int[] SelectedRunes => Tree.SelectedRunes.Concat(Second.SelectedRunes).ToArray();

        private RunePage Page => new RunePage(SelectedRunes, Tree.SelectedTree.ID, Second.SelectedTree.ID);

        private int SelectedChampion, SelectedPosition;

        private async void Window_Initialized(object sender, EventArgs e)
        {
            ChampSelectDetector.Init();

            RuneTree[] trees = await Riot.GetRuneTrees();

            await Tree.Initialize();
            Second.SetTree(trees[1]);
            await SetSecondaryTrees();

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
        
        private async Task SetSecondaryTrees()
        {
            await Second.SetValidTrees((await Riot.GetRuneTreesByIDAsync()).Keys.Where(o => o != Tree.SelectedTree.ID).ToArray());
        }

        private async void Tree_SelectedTreeChanged(object sender, int e)
        {
            await SetSecondaryTrees();
        }
        
        private async void Upload_Click(object sender, EventArgs e)
        {
            await Page.UploadToClient();
        }

        private void Tree_SelectedRunesChanged(object sender, EventArgs e)
        {
            Upload.IsEnabled = SelectedRunes.Length == 6;

            if (Upload.IsEnabled)
                SaveRunePageToBook();
        }

        private void PositionDD_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedPosition = PositionDD.SelectedIndex;
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
            var position = (Position)SelectedPosition;

            var page = RuneBook.Instance.FirstOrDefault(o => o.ChampionID == SelectedChampion && o.Position == position);

            if (page != null)
            {
                Tree.SetTree(Riot.GetRuneTreesByID()[page.PrimaryTree]);
                Second.SetTree(Riot.GetRuneTreesByID()[page.SecondaryTree]);

                Tree.SelectedRunes = page.RuneIDs.Take(4).ToArray();
            }
        }

        private async void Clear_Click(object sender, EventArgs e)
        {
            var page = RuneBook.Instance.FirstOrDefault(o => o.ChampionID == SelectedChampion && o.Position == (Position)SelectedPosition);

            Tree.SelectedRunes = new int[4];
            Second.SelectedRunes = new int[0];
            RuneBook.Instance.Remove(page);

            Tree.SetTree(Riot.GetRuneTreesByID().Values.First());
            //Second.SetTree(Riot.GetRuneTreesByID().Values.ElementAt(2));
            await SetSecondaryTrees();
        }

        private void SaveRunePageToBook()
        {
            var position = (Position)SelectedPosition;

            var page = RuneBook.Instance.FirstOrDefault(o => o.ChampionID == SelectedChampion && o.Position == position);

            if (page != null)
            {
                RuneBook.Instance.Remove(page);
            }

            page = this.Page;
            page.ChampionID = SelectedChampion;
            page.Position = position;

            RuneBook.Instance.Add(page);
        }
    }
}
