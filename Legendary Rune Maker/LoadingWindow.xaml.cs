using LCU.NET;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Properties;
using Onova;
using Onova.Models;
using Onova.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Legendary_Rune_Maker
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        private bool Shown;
        private CancellationTokenSource CancelSource = new CancellationTokenSource();

        public LoadingWindow()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            
            InitializeComponent();
        }

        private void ShowMainWindow()
        {
            if (Shown)
                return;
            Shown = true;

            new MainWindow();
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
            Status.Text = "Loading...";
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
            Status.Text = "Checking for updates...";
            Progress.IsIndeterminate = true;

            var manager = new UpdateManager(
                new WebPackageResolver("https://pipe0481.heliohost.org/plrm/updates.man"),
                new ZipPackageExtractor());

            var update = await await Task.WhenAny(Task.Delay(3000).ContinueWith<CheckForUpdatesResult>(_ => null), manager.CheckForUpdatesAsync());

            if (update?.CanUpdate == true)
            {
                Status.Text = "Updating...";
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
