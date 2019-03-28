using Anotar.Log4Net;
using LCU.NET;
using LCU.NET.Extras;
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
using System.Text;
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
    public partial class MainPage : Page, IUiActuator, IPage, INotifyPropertyChanged
    {
        public static double BaseWidth { get; } = 303;
        public static double BaseHeight { get; } = 310;
        public static double ExpandWidth { get; } = 810;
        public static double ExpandHeight { get; } = 325;

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

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow Owner { get; set; }

        public MainPage(ILoL lol, MainWindow owner, Actuator actuator)
        {
            this.LoL = lol;

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
            LCUApp.MainWindow = this;
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

        public async Task<RunePage> LoadPageFromDefaultProvider(int championId = -1)
        {
            var champId = championId == -1 ? SelectedChampion : championId;

            var champName = Riot.GetChampion(champId).Name;
            var provider = Actuator.RuneProviders.FirstOrDefault(o => o.Name == Config.Current.LockLoadProvider)
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

            return page;
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
            await SetChampion((await Riot.GetChampionsAsync())[index]);
        }

        public Task SetChampion(int championId) => SetChampion(Riot.GetChampion(championId));

        public async Task SetChampion(Champion champ, bool canCopy = false)
        {
            if (champ == null)
            {
                SelectedChampion = 0;
                ChampionImage.Source = (ImageSource)Application.Current.FindResource("NoChamp");
                ImportItem.IsEnabled = false;

                Tree.Clear();
                return;
            }

            SelectedChampion = champ.ID;
            ChampionImage.Source = await ImageCache.Instance.Get(champ.ImageURL);
            ImportItem.IsEnabled = true;

            UpdateRunePageFromRuneBook(canCopy);
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
                    Icon = new Image() { Source = (ImageSource)Application.Current.FindResource(provider.GetType().Name + "Icon") },
                    IsEnabled = provider.IsEnabled
                };
                menu.Items.Add(header);

                header.SubmenuOpened += async (_, __) =>
                {
                    Position[] roles;

                    try
                    {
                        roles = await provider.GetPossibleRoles(SelectedChampion);
                    }
                    catch (Exception ex)
                    {
                        LogTo.ErrorException($"Failed to load possible positions from {provider.Name} for {Riot.GetChampion(SelectedChampion).Name}", ex);

                        header.Items.Clear();
                        header.Items.Add("Error");

                        return;
                    }

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
                await SetChampion(champ.Selected, true);
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

            await Actuator.Init();
            await SetChampion(null);
        }

        public void ShowNotification(string title, string message = null, LCU.NET.Extras.Data.NotificationType type = LCU.NET.Extras.Data.NotificationType.Information)
        {
            LogTo.Debug("Showing notification with title \"{0}\"", title);
            MainWindow.ShowNotification(title, message, (NotificationType)type);
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

        private void AttachChk_Checked(object sender, RoutedEventArgs e)
        {
            Actuator.Enabled = true;
        }

        private void AttachChk_Unchecked(object sender, RoutedEventArgs e)
        {
            Actuator.Enabled = false;
        }

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            Share.ContextMenu.IsOpen = true;

            ShareItem.IsEnabled = SelectedRunes?.Length == 9;
            ImportItem.IsEnabled = GetClipboard().Success;
        }

        private void ShareItem_Click(object sender, RoutedEventArgs e)
        {
            byte[] data = Page.RuneIDs.SelectMany(o => BitConverter.GetBytes((short)o)).ToArray();
            Clipboard.SetText(Convert.ToBase64String(data));

            ShowNotification(Text.Done, Text.PageCopiedToClipboard, LCU.NET.Extras.Data.NotificationType.Success);
        }

        private void ImportItem_Click(object sender, RoutedEventArgs e)
        {
            string str = Clipboard.GetText();

            var (success, result) = GetClipboard();

            if (!success)
                ShowNotification(Text.InvalidImportFormatTitle, Text.InvalidImportFormatMsg, LCU.NET.Extras.Data.NotificationType.Error);

            Tree.SetPage(result);
        }

        private (bool Success, RunePage Result) GetClipboard()
        {
            string str = Clipboard.GetText();

            if (string.IsNullOrWhiteSpace(str))
                goto no;

            byte[] data;

            try
            {
                data = Convert.FromBase64String(str);
            }
            catch (FormatException)
            {
                goto no;
            }

            if (data.Length != sizeof(short) * 9)
                goto no;

            int[] ids = Enumerable.Range(0, 9).Select(i => (int)BitConverter.ToInt16(data, i * sizeof(short))).ToArray();

            int primary = GetStyleFromRuneID(ids[0]);
            int secondary = GetStyleFromRuneID(ids[4]);

            if (primary == -1 || secondary == -1)
                goto no;

            return (true, new RunePage(ids, primary, secondary, SelectedChampion, SelectedPosition));

        no:
            return (false, null);

            int GetStyleFromRuneID(int runeId)
            {
                var tree = Riot.TreeStructures
                    .Select(o => o.Value)
                    .FirstOrDefault(o => o.PerkSlots.SelectMany(i => i).Any(i => i.ID == runeId));

                if (tree != null)
                    return tree.ID;

                return -1;
            }
        }
    }
}
