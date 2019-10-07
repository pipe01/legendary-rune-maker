using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Overlay;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Legendary_Rune_Maker.Windows
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        public ObservableCollection<Enemy> EnemySummoners { get; set; } = new ObservableCollection<Enemy>();

        private readonly Timer LockTimer;
        private readonly IntPtr LeagueHandle;

        public OverlayWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            var uxProcess = Process.GetProcessesByName("LeagueClientUx").FirstOrDefault();

            if (uxProcess == null)
                return;

            LeagueHandle = uxProcess.MainWindowHandle;

            LockTimer = new Timer(10);
            LockTimer.Elapsed += this.LockTimer_Elapsed;
            LockTimer.Start();
        }

        private void LockTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Win32.RECT leagueRect = default;
                if (Win32.GetWindowRect(LeagueHandle, ref leagueRect))
                {
                    if (this.Visibility != Visibility.Visible)
                        this.Visibility = Visibility.Hidden;

                    this.Left = leagueRect.Left;
                    this.Top = leagueRect.Top;
                    this.Width = leagueRect.Width;
                    this.Height = leagueRect.Height;
                }
                else if (this.Visibility != Visibility.Hidden)
                {
                    this.Visibility = Visibility.Hidden;
                }
            });
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Visible;

            var champs = await Riot.GetChampionsAsync();
            var rng = new Random();
            
            for (int i = 0; i < 3; i++)
            {
                EnemySummoners.Add(new Enemy
                {
                    GoodPicks = Enumerable.Repeat(0, 4).Select(o => champs[rng.Next(0, champs.Length)]).ToArray()
                });
            }
        }
    }
}
