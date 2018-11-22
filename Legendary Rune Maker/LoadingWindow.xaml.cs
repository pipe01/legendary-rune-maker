using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Locale;
using Legendary_Rune_Maker.Pages;
using Ninject;
using Ninject.Parameters;
using Onova;
using Onova.Models;
using Onova.Services;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Legendary_Rune_Maker
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        private bool Shown;
        private CancellationTokenSource CancelSource = new CancellationTokenSource();

        private readonly IKernel Kernel;

        public LoadingWindow(IKernel kernel)
        {
            this.Kernel = kernel;

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            
            InitializeComponent();
        }

        private void ShowMainWindow()
        {
            if (Shown)
                return;
            Shown = true;

            var win = Kernel.Get<MainWindow>();
            var mainPage = Kernel.Get<MainPage>(new Parameter("MainWindow", win, true));
            win.SetMainPage(mainPage);

            this.Close();
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
#if !DEBUG
            if (Config.Default.CheckUpdatesBeforeStartup)
                await CheckUpdates();
#endif

            if (Config.Default.LoadCacheBeforeStartup)
                await LoadCache();

            ShowMainWindow();
        }

        private async Task LoadCache()
        {
            Progress.IsIndeterminate = false;
            Progress.Value = 0;
            Status.Text = Text.Loading;
            Cancel.Visibility = Visibility.Hidden;
            Hint.Visibility = Visibility.Visible;

            //If the cache is already downloaded, hide the window
            if (ImageCache.Instance.LocalCache)
            {
                this.Visibility = Visibility.Hidden;
            }

            if (WebCache.CacheGameVersion != await Riot.GetLatestVersionAsync()
                || WebCache.CacheLocale != CultureInfo.CurrentCulture.Name)
            {
                WebCache.Clear();

                WebCache.CacheGameVersion = await Riot.GetLatestVersionAsync();
                WebCache.CacheLocale = CultureInfo.CurrentCulture.Name;
            }
            
            await Riot.SetLanguage(Config.Default.Culture);
            await Riot.CacheAll(o => Dispatcher.Invoke(() => Progress.Value = o));
        }
        
        private async Task CheckUpdates()
        {
            Status.Text = Text.CheckingUpdates;
            Progress.IsIndeterminate = true;

            var manager = new UpdateManager(
                new GithubPackageResolver("pipe01", "legendary-rune-maker", "*.*.*"),
                new ZipPackageExtractor());

            var update = await await Task.WhenAny(
                    Task.Delay(3000).ContinueWith<CheckForUpdatesResult>(_ => null),
                    manager.CheckForUpdatesAsync());
            
            if (update?.CanUpdate == true)
            {
                Status.Text = Text.Updating;
                Cancel.Visibility = Visibility.Visible;
                Progress.IsIndeterminate = false;

                var progress = new Progress<double>(o => Progress.Value = o);

                try
                {
                    await manager.PrepareUpdateAsync(update.LastVersion, progress, CancelSource.Token);
                }
                catch (TaskCanceledException)
                {
                }

                if (manager.IsUpdatePrepared(update.LastVersion))
                {
                    manager.LaunchUpdater(update.LastVersion);
                    Environment.Exit(0);
                }
            }
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            CancelSource.Cancel();
            Cancel.IsEnabled = false;
        }
    }
}
