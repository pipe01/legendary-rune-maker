using LCU.NET;
using LCU.NET.API_Models;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Providers;
using Legendary_Rune_Maker.Game;
using Legendary_Rune_Maker.Overlay;
using Legendary_Rune_Maker.Utils;
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
using Timer = System.Timers.Timer;

namespace Legendary_Rune_Maker.Windows
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        public ObservableCollection<Enemy?> EnemySummoners { get; set; } = new ObservableCollection<Enemy?>();

        private int[] PreviousTeam;
        private bool Active;
        private IntPtr? LeagueHandle;

        private readonly Timer LockTimer;
        private readonly ILoL LoL;
        private readonly ILeagueClient Client;
        private readonly ITeamGuesser Guesser;

        public OverlayWindow() : this(null, null, null)
        {
        }

        public OverlayWindow(ILoL lol, ILeagueClient client, ITeamGuesser guesser)
        {
            this.LoL = lol;
            this.Client = client;
            this.Guesser = guesser;

            InitializeComponent();

            this.DataContext = this;

            LockTimer = new Timer(1000);
            LockTimer.Elapsed += this.LockTimer_Elapsed;
            LockTimer.Start();

            //client.Init();
        }

        private async void ChampSelectSessionCallback(EventType eventType, LolChampSelectChampSelectSession data)
        {
            await Dispatcher.Invoke(async () =>
            {
                if (eventType == EventType.Delete)
                {
                    Active = false;
                    this.Visibility = Visibility.Hidden;

                    EnemySummoners.Clear();
                    PreviousTeam = null;
                }
                else
                {
                    if (!Active || this.Visibility != Visibility.Visible)
                    {
                        Active = true;
                        this.Visibility = Visibility.Visible;
                    }

                    await UpdateTeam(data.theirTeam.Select(o => o.championId).ToArray());
                }
            });
        }

        private async Task UpdateTeam(int[] team)
        {
            if (PreviousTeam != null && team.SequenceEqual(PreviousTeam))
                return;

            var guessedPositions = Guesser.Guess(team.Where(o => o != 0).ToArray());

            for (int i = EnemySummoners.Count; i < team.Length; i++)
            {
                EnemySummoners.Add(null);
            }

            for (int i = 0; i < team.Length; i++)
            {
                var theirChamp = team[i];

                if (theirChamp != 0)
                {
                    EnemySummoners[i] = new Enemy(
                        await Riot.GetChampionAsync(theirChamp),
                        await new METAsrcProvider().GetCountersFor(theirChamp, Position.Fill),
                        null,
                        guessedPositions.Invert()[theirChamp].ToString().ToUpper());
                }
            }

            PreviousTeam = team;
        }

        private void LockTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Dispatcher.HasShutdownStarted)
                return;

            if (LeagueHandle == null)
            {
                var uxProcesses = Process.GetProcessesByName("LeagueClientUx");

                if (uxProcesses.Length == 0)
                    return;

                LeagueHandle = uxProcesses[0].MainWindowHandle;
                LockTimer.Interval = 10;
            }
            else if (!Win32.IsWindow(LeagueHandle.Value))
            {
                LeagueHandle = null;
                LockTimer.Interval = 1000;

                return;
            }

            if (!Active)
                return;

            Dispatcher.Invoke(() =>
            {
                Win32.RECT leagueRect = default;
                var focusedWindow = Win32.GetForegroundWindow();

                if (focusedWindow == LeagueHandle && Win32.GetWindowRect(LeagueHandle.Value, ref leagueRect))
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

        private async void Window_Initialized(object sender, EventArgs e)
        {
            await Guesser.Load(new Progress<float>(o => Console.WriteLine("Loading: {0:0.0}", o * 100)));

            await LoL?.Socket.SubscribeAndUpdate<LolChampSelectChampSelectSession>("/lol-champ-select/v1/session", ChampSelectSessionCallback);

            //var events = JsonConvert.DeserializeObject<EventData[]>(File.ReadAllText("events.json"));
            //Client.Socket.Playback(events, 10);
        }

        private async void Window_Activated(object sender, EventArgs e)
        {
            await Task.Delay(5);

            if (LeagueHandle != null)
                Win32.SetForegroundWindow(LeagueHandle.Value);
        }
    }
}
