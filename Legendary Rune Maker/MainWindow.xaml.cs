using LCU.NET;
using LCU.NET.API_Models;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Game;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Legendary_Rune_Maker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool InDesigner => DesignerProperties.GetIsInDesignMode(new DependencyObject());

        public bool UploadOnLock
        {
            get => (bool)GetValue(UploadOnLockProperty);
            set => SetValue(UploadOnLockProperty, value);
        }
        public static readonly DependencyProperty UploadOnLockProperty = DependencyProperty.Register("UploadOnLock", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

        private Rune[] SelectedRunes => Tree.SelectedPrimary.Concat(Tree.SelectedSecondary).Where(o => o != null).ToArray();

        private RunePage Page => new RunePage(SelectedRunes.Select(o => o.ID).ToArray(), Tree.PrimaryTree.ID, Tree.SecondaryTree.ID, SelectedChampion, SelectedPosition);

        private bool ValidPage;
        private bool MovingWindow;
        private Point MoveStart;
        private int SelectedChampion;
        private Position SelectedPosition;

        public MainWindow()
        {
            if (!InDesigner)
                Application.Current.MainWindow = this;

            InitializeComponent();
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            await InitDetectors();
            await InitControls();

            this.Show();
        }

        private async Task InitDetectors()
        {
            GameState.State.EnteredState += State_EnteredState;

            LeagueClient.ConnectedChanged += LeagueClient_ConnectedChanged;

            if (!LeagueClient.TryInit())
            {
                LeagueClient.BeginTryInit();
            }

            ChampSelectDetector.SessionUpdated += ChampSelectDetector_SessionUpdated;

            LoginDetector.Init();

            if (LeagueClient.Connected)
            {
                try
                {
                    await LoginDetector.ForceUpdate();
                }
                catch (APIErrorException) { }

                await ChampSelectDetector.Init();
            }
        }

        private async Task InitControls()
        {
            foreach (var item in await Riot.GetChampions())
            {
                ChampionDD.Items.Add(item.Name);
            }

            foreach (var item in new[] { "Any", "Top", "Jungle", "Mid", "Bottom", "Support" })
            {
                PositionDD.Items.Add(item);
            }
            SetPosition(Position.Fill);
        }

        private void LeagueClient_ConnectedChanged(bool connected)
        {
            Debug.WriteLine("Connected: " + connected);

            if (connected)
            {
                GameState.State.Fire(GameTriggers.OpenGame);
            }
            else
            {
                GameState.State.Fire(GameTriggers.CloseGame);
            }
        }

        private void State_EnteredState(GameStates state)
        {
            Dispatcher.Invoke(() =>
            {
                switch (state)
                {
                    case GameStates.Disconnected:
                        Status.Foreground = new SolidColorBrush(Colors.Red);
                        Status.Text = "disconnected";

                        Task.Run(async () =>
                        {
                            await Task.Delay(1000);
                            LeagueClient.BeginTryInit();
                        });
                        break;
                    case GameStates.NotLoggedIn:
                        Status.Foreground = new SolidColorBrush(Colors.Orange);
                        Status.Text = "not logged in";
                        break;
                    case GameStates.LoggedIn:
                        Status.Foreground = new SolidColorBrush(Colors.Green);
                        Status.Text = "logged in";
                        break;
                    case GameStates.InChampSelect:
                        Status.Foreground = new SolidColorBrush(Colors.SlateBlue);
                        Status.Text = "in champ select";
                        break;
                    case GameStates.LockedIn:
                        Status.Foreground = new SolidColorBrush(Colors.YellowGreen);
                        Status.Text = "locked in";

                        if (UploadOnLock && ValidPage)
                            Task.Run(Page.UploadToClient);

                        break;
                }
            });
        }

        private void ChampSelectDetector_SessionUpdated(LolChampSelectChampSelectSession obj)
        {
            var player = ChampSelectDetector.CurrentSelection;

            if (player == null || player.championId == 0)
                return;

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
            Upload.IsEnabled = ValidPage = SelectedRunes.Length == 6 && SelectedChampion != 0 && GameState.CanUpload;

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

        private void UpdateRunePageFromRuneBook(bool canCopy = true)
        {
            if (canCopy && ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift))
            {
                SaveRunePageToBook();
                return;
            }

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

        private void Close_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Minimize_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.Source == this)
            {
                DragMove();
            }
        }
    }
}
