using LCU.NET;
using LCU.NET.API_Models;
using Legendary_Rune_Maker.Controls;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Rune_providers;
using Legendary_Rune_Maker.Game;
using Notifications.Wpf;
using System;
using System.Collections.Generic;
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
        
        public bool Attached
        {
            get { return (bool)GetValue(AttachedProperty); }
            set { SetValue(AttachedProperty, value); }
        }
        public static readonly DependencyProperty AttachedProperty = DependencyProperty.Register("Attached", typeof(bool), typeof(MainWindow), new PropertyMetadata(true, AttachedChanged));

        private Rune[] SelectedRunes => Tree.SelectedPrimary.Concat(Tree.SelectedSecondary).Where(o => o != null).ToArray();

        private RunePage Page => new RunePage(SelectedRunes.Select(o => o.ID).ToArray(), Tree.PrimaryTree.ID, Tree.SecondaryTree.ID, SelectedChampion, SelectedPosition);

        private readonly IRuneProvider[] RuneProviders = new IRuneProvider[]
        {
            new RunesLolProvider(),
            new ClientProvider()
        };

        public static INotificationManager NotificationManager;

        private bool ValidPage;
        private int SelectedChampion;
        private Position SelectedPosition;

        public MainWindow()
        {
            if (!InDesigner)
                Application.Current.MainWindow = this;

            NotificationManager = new NotificationManager(Dispatcher);

            InitializeComponent();
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            await InitDetectors();
            await InitControls();

            //if (LeagueClient.Connected)
            //    new DebugProxyWindow().Show();

            this.Show();
        }

        private async Task InitDetectors()
        {
            GameState.State.EnteredState += State_EnteredState;
            ChampSelectDetector.SessionUpdated += ChampSelectDetector_SessionUpdated;
            LeagueClient.ConnectedChanged += LeagueClient_ConnectedChanged;

            if (!LeagueClient.TryInit())
            {
                LeagueClient.BeginTryInit();
            }
            
            await LoginDetector.Init();
            await ChampSelectDetector.Init();
        }

        private async Task InitControls()
        {
            await SetChampionIndex(0);

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
            Dispatcher.Invoke(async () =>
            {
                switch (state)
                {
                    case GameStates.Disconnected:
                        Status.Foreground = new SolidColorBrush(Colors.Red);
                        Status.Text = "disconnected";

                        await Task.Run(async () =>
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

                        if (UploadOnLock && ValidPage && GameState.CanUpload)
                        {
                            string champion = (await Riot.GetChampions()).Single(o => o.ID == SelectedChampion).Name;

                            NotificationManager.Show(new NotificationContent
                            {
                                Title = "Locked in",
                                Message = champion + ", " + SelectedPosition.ToString().ToLower(),
                                Type = NotificationType.Success
                            });

                            await Task.Run(Page.UploadToClient);
                        }

                        break;
                }
            });
        }

        private void ChampSelectDetector_SessionUpdated(LolChampSelectChampSelectSession obj)
        {
            var player = ChampSelectDetector.CurrentSelection;

            if (player == null || player.championId == 0 || !Attached)
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
            ValidPage = SelectedRunes.Length == 6 && SelectedChampion != 0;

            Upload.IsEnabled = ValidPage && GameState.CanUpload;

            if (ValidPage)
                SaveRunePageToBook();
        }

        private void PositionDD_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetPosition((Position)PositionDD.SelectedIndex);
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
            await SetChampion((await Riot.GetChampions())[index]);
        }

        private async Task SetChampion(int id)
        {
            var champs = await Riot.GetChampions();
            var champ = champs.SingleOrDefault(o => o.ID == id);

            await SetChampion(champ);
        }

        private async Task SetChampion(Champion champ)
        {
            if (champ == null)
            {
                SelectedChampion = 0;
                ChampionImage.Source = null;
                return;
            }

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

            var page = RuneBook.Instance.Get(SelectedChampion, SelectedPosition, false);

            if (page != null)
            {
                Tree.SetPage(page);
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

        private async void Load_Click(object sender, EventArgs e)
        {
            if (SelectedChampion == 0)
                return;

            Load.IsEnabled = false;

            var menu = new ContextMenu();
            var availProviders = new List<(IRuneProvider Provider, Position Position)>();

            foreach (var provider in RuneProviders)
            {
                var available = await provider.GetPossibleRoles(SelectedChampion);

                if (SelectedPosition == Position.Fill)
                {
                    foreach (var avail in available)
                    {
                        availProviders.Add((provider, avail));
                    }
                }
                else if (available.Contains(SelectedPosition))
                {
                    availProviders.Add((provider, SelectedPosition));
                }
            }

            if (availProviders.Count > 0)
                menu.Items.Add(new MenuItem { Header = "Load from:", IsEnabled = false });
            else
                menu.Items.Add(new MenuItem { Header = "None available", IsEnabled = false });

            foreach (var item in availProviders)
            {
                string header = item.Provider.Name + (item.Position != Position.Fill ? $" - {item.Position}" : "");

                var menuItem = new MenuItem { Header = header };
                menuItem.Click += async (a, b) => Tree.SetPage(await item.Provider.GetRunePage(SelectedChampion, item.Position));

                menu.Items.Add(menuItem);
            }

            Load.IsEnabled = true;
            Load.ContextMenu = menu;
            menu.IsOpen = true;
        }

        private async void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                await Task.Delay(500); //Wait for Windows' window minimize animation to finish, cuz it looks K00L
                this.Hide();
            }
        }

        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                new DebugProxyWindow().Show();
            }
        }
        
        private async void StatusT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await LoginDetector.ForceUpdate();
            await ChampSelectDetector.ForceUpdate();
        }

        private async void ChampionImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var champ = PickChampionDialog.PickChampion();

            if (champ != null)
                await SetChampion(champ);
        }
        
        private static async void AttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
                await ChampSelectDetector.ForceUpdate();
        }

        private void LoLButton_Click(object sender, EventArgs e)
        {

        }

        private void Automation_Click(object sender, EventArgs e)
        {
            new AutomationWindow().Show();
        }
    }
}
