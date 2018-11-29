using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.WAMP;
using Microsoft.Win32;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Legendary_Rune_Maker
{
    /// <summary>
    /// Interaction logic for DebugProxyWindow.xaml
    /// </summary>
    public partial class DebugProxyWindow : Window
    {
        private class DebugProxy : IProxy
        {
            public DebugProxyWindow Window;

            public DebugProxy(DebugProxyWindow window)
            {
                this.Window = window;
            }

            public bool Handle<T>(string url, Method method, object data, out T result)
            {
                this.Window.Add("-> " + url, data);

                if (typeof(T) == typeof(LolChampSelectChampSelectSession))
                    result = (T)(object)new LolChampSelectChampSelectSession
                    {
                        actions = new LolChampSelectChampSelectAction[0][]
                    };
                else
                    result = Activator.CreateInstance<T>();

                return false;
            }

            public bool Handle(string url, Method method, object data)
            {
                this.Window.Add("-> " + url, null);
                return false;
            }

            public JsonApiEvent Handle(JsonApiEvent @event)
            {
                this.Window.Add($"<- [{@event.EventType}] {@event.URI}", @event);

                return @event;
            }
        }

        private IList<(string Uri, string Json)> Events = new List<(string, string)>();

        private readonly ILeagueClient LeagueClient;

        public DebugProxyWindow(ILeagueClient leagueClient)
        {
            this.LeagueClient = leagueClient;

            LeagueClient.Proxy = new DebugProxy(this);
            //LeagueSocket.DumpToDebug = true;
            Debug.Listeners.Add(new TextWriterTraceListener("log.txt"));

            InitializeComponent();

            this.DataContext = this;
        }

        public void Add(string msg, object data)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => Add(msg, data));
                return;
            }

            List.Items.Add(msg);
            Events.Add((msg, JsonConvert.SerializeObject(data)));

            if (VisualTreeHelper.GetChildrenCount(List) > 0)
            {
                var border = (Border)VisualTreeHelper.GetChild(List, 0);
                var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var ev = JsonConvert.DeserializeObject<JsonApiEvent>(Input.Text);

            LeagueClient.Socket.HandleEvent(ev);
        }

        private void List_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(Events[List.SelectedIndex].Json);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            var diag = new OpenFileDialog();

            if (diag.ShowDialog(this) == true)
            {
                var events = JsonConvert.DeserializeObject<EventData[]>(File.ReadAllText(diag.FileName));

                (LeagueClient.Socket as LeagueSocket)?.Playback(events, 5);
            }
        }
    }
}
