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

        private bool ValidPage;

        private int SelectedChampion;
        private Position SelectedPosition;

        private async void Window_Initialized(object sender, EventArgs e)
        {
            ChampSelectDetector.SessionUpdated += ChampSelectDetector_SessionUpdated;
            ChampSelectDetector.Init();

            try
            {
                await ChampSelectDetector.ForceUpdate();
            }
            catch (APIErrorException) { }

            foreach (var item in await Riot.GetChampions())
            {
                ChampionDD.Items.Add(item.Name);
            }

            foreach (var item in new[] { "Any", "Top", "Jungle", "Mid", "Bottom", "Support" })
            {
                PositionDD.Items.Add(item);
            }
            SetPosition(Position.Fill);

            this.Show();
        }

        private void ChampSelectDetector_SessionUpdated(LolChampSelectChampSelectSession obj)
        {
            var player = ChampSelectDetector.CurrentSelection;

            if (player == null || player.championId == 0)
                return;

            bool lockedIn = obj.actions.Select(o => o[0]).LastOrDefault(o => o.actorCellId == player.cellId && o.type == "pick")?.completed ?? false;

            Position p;

            switch (player.assignedPosition)
            {
                case "TOP":
                    p = Position.Top;
                    break;
                case "JUNGLE":
                    p = Position.Jungle;
                    break;
                case "MIDDLE":
                    p = Position.Mid;
                    break;
                case "UTILITY":
                    p = Position.Support;
                    break;
                case "BOTTOM":
                    p = Position.Bottom;
                    break;
                default:
                    p = Position.Fill;
                    break;
            }

            Dispatcher.Invoke(async () =>
            {
                SetPosition(p);
                await SetChampion(player.championId);

                if (lockedIn && ValidPage)
                    await Page.UploadToClient();
            });
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
            Upload.IsEnabled = ValidPage = SelectedRunes.Length == 6 && SelectedChampion != 0;

            if (ValidPage)
                SaveRunePageToBook();
        }

        private void PositionDD_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetPosition((Position)PositionDD.SelectedIndex);
        }

        private async void ChampionDD_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await SetChampionIndex(ChampionDD.SelectedIndex);
        }

        private void SetPosition(Position position)
        {
            SelectedPosition = position;

            if (position == Position.UNSELECTED)
            {
                PositionDD.SelectedIndex = -1;
                PositionImage.Source = null;
            }
            else
            {
                PositionDD.SelectedIndex = (int)position;
                PositionImage.Source = Application.Current.FindResource(position.ToString()) as ImageSource;

                UpdateRunePageFromRuneBook();
            }
        }

        private async Task SetChampionIndex(int index)
        {
            await SetChampion((await Riot.GetChampions())[index].ID);
        }

        private async Task SetChampion(int id)
        {
            var champs = await Riot.GetChampions();
            var champ = champs.SingleOrDefault(o => o.ID == id);

            if (champ == null)
            {
                SelectedChampion = 0;
                ChampionImage.Source = null;
                return;
            }

            ChampionDD.SelectedIndex = Array.IndexOf(champs, champ);
            SelectedChampion = champ.ID;
            ChampionImage.Source = await ImageCache.Instance.Get(champ.ImageURL);

            UpdateRunePageFromRuneBook();
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            Tree.Clear();

            RuneBook.Instance.Remove(SelectedChampion, SelectedPosition);
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

        private void SaveRunePageToBook()
        {
            RuneBook.Instance.Remove(SelectedChampion, SelectedPosition);
            
            if (SelectedChampion != 0)
                RuneBook.Instance.Add(this.Page);
        }
    }
}
