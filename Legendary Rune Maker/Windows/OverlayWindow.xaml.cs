using LCU.NET;
using LCU.NET.API_Models;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Overlay;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
        private readonly ILoL LoL;

        public OverlayWindow() : this(null, null)
        {
        }

        public OverlayWindow(ILoL lol, ILeagueClient client)
        {
            this.LoL = lol;

            InitializeComponent();

            this.DataContext = this;

            var uxProcess = Process.GetProcessesByName("LeagueClientUx").FirstOrDefault();

            if (uxProcess == null)
                return;

            LeagueHandle = uxProcess.MainWindowHandle;

            LockTimer = new Timer(10);
            LockTimer.Elapsed += this.LockTimer_Elapsed;
            LockTimer.Start();

            client.Init();
            lol?.Socket.SubscribeAndUpdate<LolChampSelectChampSelectSession>("/lol-champ-select/v1/session", ChampSelectSessionCallback);

            var events = JsonConvert.DeserializeObject<EventData[]>(File.ReadAllText("events.json"));
            client.Socket.Playback(events, 10);
        }

        private async void ChampSelectSessionCallback(EventType eventType, LolChampSelectChampSelectSession data)
        {
            await Dispatcher.Invoke(async () =>
            {
                if (eventType == EventType.Delete)
                    this.Close();

                if (this.EnemySummoners.Count == 0)
                {
                    foreach (var enemy in data.theirTeam)
                    {
                        EnemySummoners.Add(new Enemy(enemy.championId == 0 ? null : await Riot.GetChampionAsync(enemy.championId), null, null));
                    }
                }
                else
                {
                    for (int i = 0; i < data.theirTeam.Length; i++)
                    {
                        var theirChamp = data.theirTeam[i].championId;

                        if (theirChamp != 0 && EnemySummoners[i].Champion == null)
                        {
                            EnemySummoners[i] = new Enemy(await Riot.GetChampionAsync(theirChamp), new[] { EnemySummoners[i].Champion }, null);
                        }
                    }
                }
            });
        }

        private void LockTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Dispatcher.HasShutdownStarted)
                return;

            Dispatcher.Invoke(() =>
            {
                Win32.RECT leagueRect = default;
                var focusedWindow = Win32.GetForegroundWindow();

                if (focusedWindow == LeagueHandle && Win32.GetWindowRect(LeagueHandle, ref leagueRect))
                {
                    if (this.Visibility != Visibility.Visible)
                        this.Visibility = Visibility.Visible;

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

        private void Window_Initialized(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Visible;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Win32.SetForegroundWindow(LeagueHandle);
        }
    }
}
