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
    public partial class MainWindow : Window, IMainWindow
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
        public static readonly DependencyProperty AttachedProperty = DependencyProperty.Register("Attached", typeof(bool), typeof(MainWindow), new PropertyMetadata(true, async (_, e) =>
        {
            if ((bool)e.NewValue)
            {
                await ChampSelectDetector.ForceUpdate();
                await LoginDetector.ForceUpdate();
            }
        }));
        
        public bool Expanded
        {
            get { return (bool)GetValue(ExpandedProperty); }
            set { SetValue(ExpandedProperty, value); }
        }
        public static readonly DependencyProperty ExpandedProperty = DependencyProperty.Register("Expanded", typeof(bool), typeof(MainWindow));


        internal static readonly RuneProvider[] RuneProviders = new RuneProvider[]
        {
            new RunesLolProvider(),
            new ChampionGGProvider(),
            new OpGGProvider(),
            new ClientProvider()
        };

        private Rune[] SelectedRunes => Tree.SelectedPrimary.Concat(Tree.SelectedSecondary).Where(o => o != null).ToArray();

        public RunePage Page => new RunePage(SelectedRunes.Select(o => o.ID).ToArray(), Tree.PrimaryTree.ID, Tree.SecondaryTree.ID, SelectedChampion, SelectedPosition);

        public static INotificationManager NotificationManager;

        private int _SelectedChampion;
        public int SelectedChampion
        {
            get => _SelectedChampion;
            private set
            {
                _SelectedChampion = value;

                Load.IsEnabled = value > 0;
            }
        }

        public Position SelectedPosition { get => PositionPicker.Selected; set => PositionPicker.Selected = value; }

        public bool ValidPage => SelectedRunes?.Length == 6 && SelectedChampion != 0;

        private readonly Actuator Actuator;

        public MainWindow()
        {
            if (!InDesigner)
                Application.Current.MainWindow = this;

            NotificationManager = new NotificationManager(Dispatcher);
            this.Actuator = new Actuator(this);

            InitializeComponent();
            
            Version.Text = "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
#if DEBUG
            Version.Text += "-debug";
#endif
            Version.Text += " by pipe01";

            this.DataContext = this;
            this.Activate();
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            await Actuator.Init();
            await SetChampion(null);

            AppDomain.CurrentDomain.UnhandledException += (a, b) => Taskbar.Dispose();
            
            this.Show();
        }

        public void SafeInvoke(Action act)
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(act);
            else
                act();
        }

        public T SafeInvoke<T>(Func<T> act) => !Dispatcher.CheckAccess() ? Dispatcher.Invoke(act) : act();

        public void SetState(GameStates state)
        {
            AttachChk.IsEnabled = GameState.CanUpload;

            switch (state)
            {
                case GameStates.Disconnected:
                    Status.Foreground = new SolidColorBrush(Colors.Red);
                    Status.Text = Text.Disconnected;
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
                    break;
            }
        }

        public void ShowNotification(string title, string message = null, NotificationType type = NotificationType.Information)
        {
            NotificationManager.Show(new NotificationContent
            {
                Title = title,
                Message = message,
                Type = type
            });
        }
        
        public async Task LoadPageFromDefaultProvider()
        {
            var provider = RuneProviders.FirstOrDefault(o => o.Name == Config.Default.LockLoadProvider)
                            ?? RuneProviders[0];
            var positions = await provider.GetPossibleRoles(SelectedChampion);

            var position = positions.Contains(SelectedPosition) ? SelectedPosition : Position.Fill;

            var page = await provider.GetRunePage(SelectedChampion, position);

            Tree.SetPage(page);

            var champName = Riot.GetChampion(SelectedChampion).Name;

            ShowNotification(
                Text.PageChampInPosNotSet.FormatStr(champName, SelectedPosition.ToString().ToLower()), 
                Text.PageNotSetDownloaded.FormatStr(provider.Name),
                NotificationType.Information);
        }
        
        private async void Upload_Click(object sender, EventArgs e)
        {
            await Page.UploadToClient();
        }

        private void Tree_SelectedRunesChanged(object sender, EventArgs e)
        {
            Upload.IsEnabled = ValidPage && GameState.CanUpload;

            if (ValidPage)
                SaveRunePageToBook();
        }
        
        private async Task SetChampionIndex(int index)
        {
            await SetChampion((await Riot.GetChampions())[index]);
        }

        public Task SetChampion(int championId) => SetChampion(Riot.GetChampion(championId));

        public async Task SetChampion(Champion champ)
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
        
        private async void Load_Click(object sender, EventArgs e)
        {
            if (SelectedChampion == 0)
                return;
            
            var menu = new ContextMenu();
            menu.Items.Add(new MenuItem { Header = Text.Loading, IsEnabled = false });

            var availProviders = new List<(RuneProvider Provider, Position Position)>();
            
            Load.ContextMenu = menu;
            menu.IsOpen = true;

            menu.Cursor = Cursors.AppStarting;

            bool addedFirstHeader = false;

            foreach (var provider in RuneProviders)
            {
                var available = await provider.GetPossibleRoles(SelectedChampion);
                IList<(RuneProvider Provider, Position Position)> data = new List<(RuneProvider Provider, Position Position)>();

                if (SelectedPosition == Position.Fill)
                {
                    foreach (var avail in available)
                    {
                        data.Add((provider, avail));
                    }
                }
                else if (available.Contains(SelectedPosition))
                {
                    data.Add((provider, SelectedPosition));
                }

                if (data.Count == 0)
                    continue;

                foreach (var dataItem in data)
                {
                    string header = dataItem.Provider.Name + (dataItem.Position != Position.Fill ? $" - {dataItem.Position}" : "");

                    var menuItem = new MenuItem { Header = header };
                    menuItem.Click += async (a, b) =>
                                        Tree.SetPage(await dataItem.Provider.GetRunePage(SelectedChampion, dataItem.Position));

                    if (!addedFirstHeader)
                    {
                        addedFirstHeader = true;

                        menu.Items.Clear();
                        menu.Items.Add(new MenuItem { Header = Text.LoadFrom, IsEnabled = false });
                    }

                    menu.Items.Add(menuItem);
                }
            }
            
            if (!addedFirstHeader)
            {
                menu.Items.Clear();
                menu.Items.Add(new MenuItem { Header = Text.NoneAvailable, IsEnabled = false });
            }

            menu.Cursor = Cursors.Arrow;
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
        
        private async void ChampionImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var champ = PickChampionDialog.PickChampion();

            if (champ.Success)
                await SetChampion(champ.Selected);
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
            await Task.Delay(150);

            this.Close();
        }
        
        private void PositionPicker_SelectedChanged(object sender, EventArgs e)
        {
            if (PositionPicker.Selected != Position.UNSELECTED)
            {
                PositionImage.Source = (ImageSource)Application.Current.FindResource(PositionPicker.Selected.ToString());
                UpdateRunePageFromRuneBook();
            }
        }

        private void BugReport_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/pipe01/legendary-rune-maker/issues/new");
        }

        private void ShowRunes_Click(object sender, EventArgs e)
        {
            Expanded = !Expanded;

            if (Expanded)
                RunesArrow.Source = (ImageSource)Application.Current.FindResource("LeftArrow");
            else
                RunesArrow.Source = (ImageSource)Application.Current.FindResource("RightArrow");
        }

        public void Settings()
        {
            new SettingsWindow().ShowDialog(this);
        }
    }
}
