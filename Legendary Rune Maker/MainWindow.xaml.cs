using LCU.NET;
using LCU.NET.API_Models;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Rune_providers;
using Legendary_Rune_Maker.Game;
using Legendary_Rune_Maker.Locale;
using Legendary_Rune_Maker.Utils;
using Notifications.Wpf;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
            get => (bool)GetValue(AttachedProperty);
            set => SetValue(AttachedProperty, value);
        }
        public static readonly DependencyProperty AttachedProperty = DependencyProperty.Register("Attached", typeof(bool), typeof(MainWindow), new PropertyMetadata(true, AttachedChanged));

        private Rune[] SelectedRunes => Tree.SelectedPrimary.Concat(Tree.SelectedSecondary).Where(o => o != null).ToArray();

        private RunePage Page => new RunePage(SelectedRunes.Select(o => o.ID).ToArray(), Tree.PrimaryTree.ID, Tree.SecondaryTree.ID, SelectedChampion, SelectedPosition);

        private readonly RuneProvider[] RuneProviders = new RuneProvider[]
        {
            new RunesLolProvider(),
            new ClientProvider()
        };

        public static INotificationManager NotificationManager;

        private int _SelectedChampion;
        private int SelectedChampion
        {
            get => _SelectedChampion;
            set
            {
                _SelectedChampion = value;

                Load.IsEnabled = value > 0;
            }
        }

        private bool ValidPage;
        private Position SelectedPosition;

        public MainWindow()
        {
            if (!InDesigner)
                Application.Current.MainWindow = this;

            NotificationManager = new NotificationManager(Dispatcher);

            InitializeComponent();

            Version.Text = "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

#if DEBUG
            Version.Text += "-debug";
#endif

            Version.Text += " by pipe01";

            this.Activate();
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            var s = await Riot.GetSummonerSpells();

            await InitDetectors();
            await InitControls();

            AppDomain.CurrentDomain.UnhandledException += (a, b) => Taskbar.Dispose();
            
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
            ReadyCheckDetector.Init();
        }

        private async Task InitControls()
        {
            await SetChampion(null);

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
                        Status.Text = Text.Disconnected;

                        await Task.Run(async () =>
                        {
                            await Task.Delay(1000);
                            LeagueClient.BeginTryInit();
                        });
                        break;
                    case GameStates.NotLoggedIn:
                        Status.Foreground = new SolidColorBrush(Colors.Orange);
                        Status.Text = Text.NotLoggedIn;
                        break;
                    case GameStates.LoggedIn:
                        Status.Foreground = new SolidColorBrush(Colors.Green);
                        Status.Text = Text.LoggedIn;
                        break;
                    case GameStates.InChampSelect:
                        Status.Foreground = new SolidColorBrush(Colors.SlateBlue);
                        Status.Text = Text.InChampSelect;
                        break;
                    case GameStates.LockedIn:
                        Status.Foreground = new SolidColorBrush(Colors.YellowGreen);
                        Status.Text = Text.LockedIn;

                        if (UploadOnLock && GameState.CanUpload && Config.Default.UploadOnLock)
                        {
                            string champion = (await Riot.GetChampions()).Single(o => o.ID == SelectedChampion).Name;

                            if (!ValidPage)
                            {
                                if (Config.Default.LoadOnLock)
                                {
                                    await LoadPageFromFirstProvider();
                                }
                                else
                                {
                                    NotificationManager.Show(new NotificationContent
                                    {
                                        Title = Text.PageChampNotSet.FormatStr(champion),
                                        Type = NotificationType.Error
                                    });
                                    break;
                                }
                            }
                            
                            NotificationManager.Show(new NotificationContent
                            {
                                Title = Text.LockedInMessage,
                                Message = champion + ", " + SelectedPosition.ToString().ToLower(),
                                Type = NotificationType.Success
                            });

                            await Task.Run(Page.UploadToClient);
                        }

                        break;
                }
            });
        }

        private async Task LoadPageFromFirstProvider()
        {
            var provider = RuneProviders.First();
            var positions = await provider.GetPossibleRoles(SelectedChampion);

            var position = positions.Contains(SelectedPosition) ? SelectedPosition : Position.Fill;

            var page = await provider.GetRunePage(SelectedChampion, position);

            Tree.SetPage(page);

            var champName = (await Riot.GetChampions()).Single(o => o.ID == SelectedChampion).Name;

            NotificationManager.Show(new NotificationContent
            {
                Title = Text.PageChampInPosNotSet.FormatStr(champName, SelectedPosition),
                Message = Text.PageNotSetDownloaded.FormatStr(provider.Name),
                Type = NotificationType.Information
            });
        }

        private async void ChampSelectDetector_SessionUpdated(LolChampSelectChampSelectSession obj)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ChampSelectDetector_SessionUpdated(obj));
                return;
            }

            var player = ChampSelectDetector.CurrentSelection;

            if (player == null || player.championId == 0 || !Attached)
                return;

            var p = player.assignedPosition.ToPosition();
            
            SetPosition(p);
            await SetChampion(player.championId);
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
                ChampionImage.Source = (ImageSource)Application.Current.FindResource("NoChamp");

                Tree.Clear();
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
            var availProviders = new List<(RuneProvider Provider, Position Position)>();

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
                menu.Items.Add(new MenuItem { Header = Text.LoadFrom, IsEnabled = false });
            else
                menu.Items.Add(new MenuItem { Header = Text.NoneAvailable, IsEnabled = false });

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
                this.Activate();
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

            if (champ.Success)
                await SetChampion(champ.Selected);
        }

        private static async void AttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
                await ChampSelectDetector.ForceUpdate();
        }

        private void Automation_Click(object sender, EventArgs e)
        {
            new AutomationWindow().Show(this);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Taskbar.Dispose();
            Environment.Exit(0);
        }

        private async void Taskbar_Exit(object sender, RoutedEventArgs e)
        {
            await Task.Delay(100);

            this.Close();
        }
    }
}
