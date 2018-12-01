using Anotar.Log4Net;
using LCU.NET;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Providers;
using Legendary_Rune_Maker.Game;
using Legendary_Rune_Maker.Locale;
using Legendary_Rune_Maker.Utils;
using Notifications.Wpf;
using System;
using System.Collections;
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
using System.Windows.Navigation;

namespace Legendary_Rune_Maker.Pages
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page, IMainWindow, IPage, INotifyPropertyChanged
    {
        public static double BaseWidth { get; } = 303;
        public static double BaseHeight { get; } = 310;
        public static double ExpandWidth { get; } = 810;
        public static double ExpandHeight { get; } = 325;

        public bool Attached
        {
            get => (bool)GetValue(AttachedProperty);
            set => SetValue(AttachedProperty, value);
        }
        public static readonly DependencyProperty AttachedProperty = DependencyProperty.Register("Attached", typeof(bool), typeof(MainWindow), new PropertyMetadata(true, async (sender, e) =>
        {
            if ((bool)e.NewValue)
            {
                var p = (MainPage)sender;

                //TODO Update detectors
                //await p.ChampSelectDetector.ForceUpdate();
                //await p.LoginDetector.ForceUpdate();
            }
        }));

        public bool Expanded
        {
            get { return (bool)GetValue(ExpandedProperty); }
            set { SetValue(ExpandedProperty, value); }
        }
        public static readonly DependencyProperty ExpandedProperty = DependencyProperty.Register("Expanded", typeof(bool), typeof(MainWindow));


        private Rune[] SelectedRunes => Tree.SelectedPrimary
            .Concat(Tree.SelectedSecondary).Where(o => o != null)
            .Concat(Tree.SelectedStats.Where(o => o != default).Select(o => new Rune { ID = o.ID }))
            .ToArray();

        public RunePage Page => new RunePage(SelectedRunes.Select(o => o.ID).ToArray(), Tree.PrimaryTree.ID, Tree.SecondaryTree.ID, SelectedChampion, SelectedPosition);

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

        public Position SelectedPosition { get => PositionPicker.Selected; set => PositionPicker.SetSelectedRaw(value); }

        public bool ValidPage => SelectedRunes?.Length == 9 && SelectedChampion != 0;

        private readonly Actuator Actuator;
        private readonly ILoL LoL;
        private readonly IList<Detector> Detectors = new List<Detector>();

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow Owner { get; set; }

        public MainPage(ILoL lol, LoginDetector loginDetector, ReadyCheckDetector readyCheckDetector,
            ChampSelectDetector champSelectDetector, MainWindow owner, Actuator actuator)
        {
            this.LoL = lol;

            this.Detectors.Add(loginDetector);
            this.Detectors.Add(readyCheckDetector);
            this.Detectors.Add(champSelectDetector);

            this.Actuator = actuator;
            this.Actuator.Main = this;

            this.Owner = owner;

            InitializeComponent();

            Version.Text = "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
#if DEBUG
            Version.Text += "-" + LRM.GitCommit.Substring(0, 7);
#endif
            Version.Text += " by pipe01";

            this.Width = 810;
            this.DataContext = this;
        }

        public Size GetSize() => new Size(Expanded ? ExpandWidth : BaseWidth, Expanded ? ExpandHeight : BaseHeight);
        
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

        public async Task LoadPageFromDefaultProvider(int championId = -1)
        {
            var champId = championId == -1 ? SelectedChampion : championId;

            var champName = Riot.GetChampion(champId).Name;
            var provider = Actuator.RuneProviders.FirstOrDefault(o => o.Name == Config.Default.LockLoadProvider)
                            ?? Actuator.RuneProviders[0];
            
            LogTo.Debug("Loading page from {0} (default) for champion {1}", provider.Name, champName);
            var positions = await provider.GetPossibleRoles(champId);

            LogTo.Info("Available positions for {0}: {1}", champName, string.Join(", ", positions));

            var position = positions.Contains(SelectedPosition) ? SelectedPosition : Position.Fill;

            LogTo.Debug("Downloading rune page...");
            var page = await provider.GetRunePage(champId, position);
            LogTo.Debug("Downloaded rune page");

            if (champId == SelectedChampion)
            {
                LogTo.Debug("Setting rune page UI");
                Tree.SetPage(page);
            }

            MainWindow.ShowNotification(
                Text.PageChampInPosNotSet.FormatStr(champName, SelectedPosition.ToString().ToLower()),
                Text.PageNotSetDownloaded.FormatStr(provider.Name),
                NotificationType.Information);
        }

        private async void Upload_Click(object sender, EventArgs e)
        {
            LogTo.Info("Uploading rune page manually");
            await Page.UploadToClient(LoL.Perks);

            UploadImage.Source = (ImageSource)Application.Current.FindResource("OkHand");

            await Task.Delay(115);
            UploadImage.Width = UploadImage.Height = 28;

            await Task.Delay(2000);

            UploadImage.Source = (ImageSource)Application.Current.FindResource("Upload");
            UploadImage.Width = UploadImage.Height = 20;
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

            LogTo.Debug("Deleting current rune page");
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
            LogTo.Info("Saving rune page to book");

            RuneBook.Instance.Remove(SelectedChampion, SelectedPosition);

            if (SelectedChampion != 0)
                RuneBook.Instance.Add(this.Page);
        }

        private void Load_Click(object sender, EventArgs e)
        {
            if (SelectedChampion == 0)
                return;

            LogTo.Debug("Building rune page load context menu");

            var menu = new ContextMenu();
            Load.ContextMenu = menu;
            menu.IsOpen = true;

            var providers = Actuator.RuneProviders.Where(o => o.Supports(Provider.Options.RunePages));

            LogTo.Debug("Available providers: {0}", string.Join(", ", providers.Select(o => o.Name)));

            foreach (var provider in providers)
            {
                var header = new MenuItem
                {
                    Header = provider.Name,
                    Icon = new Image() { Source = (ImageSource)Application.Current.FindResource(provider.GetType().Name + "Icon") }
                };
                menu.Items.Add(header);

                header.SubmenuOpened += async (_, __) =>
                {
                    var roles = await provider.GetPossibleRoles(SelectedChampion);
                    header.Items.Clear();

                    foreach (var role in roles)
                    {
                        var roleItem = new MenuItem { Header = role.ToString() };
                        roleItem.Click += async (___, ____) =>
                        {
                            LogTo.Info(() => $"Manually loading page from {provider.Name} for {Riot.GetChampion(SelectedChampion).Name}");

                            this.Cursor = Cursors.Wait;
                            try
                            {
                                var page = await provider.GetRunePage(SelectedChampion, role);

                                LogTo.Debug("Setting page tree");
                                Tree.SetPage(page);
                            }
                            catch (Exception ex)
                            {
                                LogTo.ErrorException("Failed to set/download rune page from UI", ex);
                                MessageBox.Show(ex.Message);
                            }
                            this.Cursor = Cursors.Arrow;
                        };

                        header.Items.Add(roleItem);
                    }
                };
                header.Items.Add(new MenuItem { Header = Text.Loading, IsEnabled = false });
            }
        }

        private async void ChampionImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var champ = await PickChampionPage.PickChampion(NavigationService, LoL.Champions);

            if (champ.Success)
                await SetChampion(champ.Selected);
        }

        private void Automation_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new AutomationPage());
        }

        private void PositionPicker_SelectedChanged(object sender, EventArgs e)
        {
            if (PositionPicker.Selected != Position.UNSELECTED)
            {
                LogTo.Debug("Changed position selection");

                PositionImage.Source = (ImageSource)Application.Current.FindResource(PositionPicker.Selected.ToString());
                UpdateRunePageFromRuneBook();
            }
        }
        
        private void ShowRunes_Click(object sender, EventArgs e)
        {
            Expanded = !Expanded;

            PropertyChanged(this, new PropertyChangedEventArgs(nameof(Expanded)));

            if (Expanded)
            {
                Owner.SetSize(ExpandWidth, ExpandHeight);
                RunesArrow.Source = (ImageSource)Application.Current.FindResource("LeftArrow");
            }
            else
            {
                Owner.SetSize(BaseWidth, BaseHeight);
                RunesArrow.Source = (ImageSource)Application.Current.FindResource("RightArrow");
            }
        }

        private async void Page_Initialized(object sender, EventArgs e)
        {
            LogTo.Debug("Initializing main page");
            
            await Actuator.Init(Detectors.ToArray());
            await SetChampion(null);

            if (Config.Default.LockLoadProvider == null)
            {
                Config.Default.LockLoadProvider =
                    Actuator.RuneProviders.First(o => o.Supports(Provider.Options.RunePages) && !(o is ClientProvider)).Name;
                Config.Default.Save();
            }
            
            if (Config.Default.ItemSetProvider == null)
            {
                Config.Default.ItemSetProvider =
                    Actuator.RuneProviders.First(o => o.Supports(Provider.Options.ItemSets)).Name;
                Config.Default.Save();
            }
        }

        public void ShowNotification(string title, string message = null, NotificationType type = NotificationType.Information)
        {
            LogTo.Debug("Showing notification with title \"{0}\"", title);
            MainWindow.ShowNotification(title, message, type);
        }

        private void ChampionImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BugReport_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Text.ReportBugDialog, "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                LogTo.Info("Opening browser to report a bug");
                Process.Start("https://github.com/pipe01/legendary-rune-maker/issues/new");
            }
        }
    }
}
