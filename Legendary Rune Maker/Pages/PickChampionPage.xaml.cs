using LCU.NET.Plugins.LoL;
using Legendary_Rune_Maker.Controls;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Game;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Legendary_Rune_Maker.Pages
{
    /// <summary>
    /// Interaction logic for PickChampionPage.xaml
    /// </summary>
    public partial class PickChampionPage : Page, IPage
    {
        private static bool ShowOnlyAvailable;

        public bool ShowNoChampion { get; set; } = true;

        public Champion SelectedChampion { get; private set; }

        public ObservableCollection<Champion> ChampionList { get; set; } = new ObservableCollection<Champion>();

        private TaskCompletionSource<Champion> Completion;
        private int[] AvailableIDs;

        private readonly IChampions PChampions;

        public PickChampionPage(TaskCompletionSource<Champion> completion, IChampions champions)
        {
            this.PChampions = champions;

            InitializeComponent();

            Available.IsEnabled = GameState.CanUpload;
            this.Completion = completion;
            this.DataContext = this;

            Search.Focus();
        }

        private async void Page_Initialized(object sender, EventArgs e)
        {
            await LoadAllChampions();

            //Show if champ is null, the search matches and either the available IDs are null or the champion ID is in
            //the available IDs
            CollectionViewSource.GetDefaultView(ChampionList).Filter =
                o => o == null || (
                     ((Champion)o).Name.IndexOf(Search.Text, StringComparison.OrdinalIgnoreCase) >= 0
                     && (AvailableIDs?.Contains(((Champion)o).ID) != false));

            await SetOnlyShowAvailable(ShowOnlyAvailable);
        }

        private void Champion_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SetChampion(((ChampionImageControl)sender).Champion);
        }

        private void SetChampion(Champion champion)
        {
            SelectedChampion = champion;

            Completion.SetResult(SelectedChampion);
            NavigationService.GoBack();
        }

        public static async Task<(bool Success, Champion Selected)> PickChampion(NavigationService navigator,
            IChampions champions, bool ban = false)
        {
            var tcs = new TaskCompletionSource<Champion>();
            var win = new PickChampionPage(tcs, champions);

            if (ban)
                win.BackImage.ImageSource = (ImageSource)Application.Current.FindResource("BgRed");
            
            navigator.Navigate(win);

            Champion selChampion;

            try
            {
                selChampion = await tcs.Task;
            }
            catch (TaskCanceledException)
            {
                return (false, null);
            }

            return (true, selChampion);
        }

        private async void Available_Checked(object sender, RoutedEventArgs e)
        {
            await SetOnlyShowAvailable(true);
        }

        private async void Available_Unchecked(object sender, RoutedEventArgs e)
        {
            await SetOnlyShowAvailable(false);
        }

        private async Task SetOnlyShowAvailable(bool val)
        {
            Available.IsChecked = ShowOnlyAvailable = val;

            AvailableIDs = val ? (await PChampions.GetOwnedChampionsMinimal()).Select(o => o.id).ToArray() : null;
            CollectionViewSource.GetDefaultView(ChampionList).Refresh();
        }

        private async Task LoadAllChampions()
        {
            ChampionList.Clear();

            if (ShowNoChampion)
                ChampionList.Add(null);

            foreach (var item in await Riot.GetChampionsAsync())
            {
                ChampionList.Add(item);
            }
        }

        public Size GetSize() => new Size(this.Width, this.Height);
        
        private void Page_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Completion.SetCanceled();
            }
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(ChampionList).Refresh();
        }

        private void Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var list = CollectionViewSource.GetDefaultView(ChampionList).Cast<Champion>().Where(o => o != null);

                if (list.Count() == 1)
                    SetChampion(list.First());
            }
        }
    }
}
