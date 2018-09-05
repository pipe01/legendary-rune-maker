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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

namespace Legendary_Rune_Maker.Pages
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page, IMainWindow, IPage
    {
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


        private Rune[] SelectedRunes => Tree.SelectedPrimary.Concat(Tree.SelectedSecondary).Where(o => o != null).ToArray();

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

        public Position SelectedPosition { get => PositionPicker.Selected; set => PositionPicker.Selected = value; }

        public bool ValidPage => SelectedRunes?.Length == 6 && SelectedChampion != 0;

        private readonly Actuator Actuator;
        private readonly MainWindow Owner;

        public MainPage(MainWindow owner)
        {
            this.Actuator = new Actuator(this);
            this.Owner = owner;

            InitializeComponent();

            owner.SetSize(this.Width, this.Height);

            Version.Text = "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
#if DEBUG
            Version.Text += "-debug";
#endif
            Version.Text += " by pipe01";

            this.Width = 810;
            this.DataContext = this;
        }

        public Size GetSize() => new Size(Expanded ? 810 : 303, this.Height);
        
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

        public async Task LoadPageFromDefaultProvider()
        {
            var provider = Actuator.RuneProviders.FirstOrDefault(o => o.Name == Config.Default.LockLoadProvider)
                            ?? Actuator.RuneProviders[0];
            var positions = await provider.GetPossibleRoles(SelectedChampion);

            var position = positions.Contains(SelectedPosition) ? SelectedPosition : Position.Fill;

            var page = await provider.GetRunePage(SelectedChampion, position);

            Tree.SetPage(page);

            var champName = Riot.GetChampion(SelectedChampion).Name;

            MainWindow.ShowNotification(
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

            foreach (var provider in Actuator.RuneProviders)
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

        private async void ChampionImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var champ = await PickChampionPage.PickChampion(NavigationService);

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
            {
                Owner.SetSize(810, this.Height);
                RunesArrow.Source = (ImageSource)Application.Current.FindResource("LeftArrow");
            }
            else
            {
                Owner.SetSize(303, this.Height);
                RunesArrow.Source = (ImageSource)Application.Current.FindResource("RightArrow");
            }
        }

        private async void Page_Initialized(object sender, EventArgs e)
        {
            await Actuator.Init();
            await SetChampion(null);
        }

        public void ShowNotification(string title, string message = null, NotificationType type = NotificationType.Information)
            => MainWindow.ShowNotification(title, message, type);

        private void ChampionImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
