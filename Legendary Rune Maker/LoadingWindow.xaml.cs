using LCU.NET;
using Legendary_Rune_Maker.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
            if (!ImageCache.Instance.LocalCache)
            {
                await Riot.CacheAll(o => Dispatcher.Invoke(() => Progress.Value = o));
                //await Riot.DownloadCacheCompressed();
            }

            ShowMainWindow();
        }
    }
}
