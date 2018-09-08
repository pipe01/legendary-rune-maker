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
        public bool ShowNoChampion { get; set; } = true;

        public Champion SelectedChampion { get; private set; }

        public ObservableCollection<Champion> ChampionList { get; set; } = new ObservableCollection<Champion>();

        private TaskCompletionSource<Champion> Completion;

        public PickChampionPage(TaskCompletionSource<Champion> completion)
        {
            InitializeComponent();

            Available.IsEnabled = GameState.CanUpload;
            this.Completion = completion;
            this.DataContext = this;
        }

        private async void Page_Initialized(object sender, EventArgs e)
        {
            await LoadAllChampions();

            CollectionViewSource.GetDefaultView(ChampionList).Filter =
                o => o == null || ((Champion)o).Name.IndexOf(Search.Text, StringComparison.OrdinalIgnoreCase) >= 0;

            this.Focus();
        }

        private void Champion_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SelectedChampion = ((ChampionImageControl)sender).Champion;

            Completion.SetResult(SelectedChampion);
            NavigationService.GoBack();
        }

        public static async Task<(bool Success, Champion Selected)> PickChampion(NavigationService navigator, bool ban = false)
        {
            var tcs = new TaskCompletionSource<Champion>();
            var win = new PickChampionPage(tcs);

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
            var availableIds = (await LCU.NET.Plugins.LoL.Champions.GetOwnedChampionsMinimal()).Select(o => o.id);

            foreach (var item in ChampionList.Where(o => o != null && !availableIds.Contains(o.ID)).ToArray())
            {
                ChampionList.Remove(item);
            }
        }

        private async void Available_Unchecked(object sender, RoutedEventArgs e)
        {
            await LoadAllChampions();
        }

        private async Task LoadAllChampions()
        {
            ChampionList.Clear();

            if (ShowNoChampion)
                ChampionList.Add(null);

            foreach (var item in await Riot.GetChampions())
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
    }
}
