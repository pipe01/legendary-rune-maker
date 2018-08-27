﻿using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.WAMP;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Legendary_Rune_Maker
{
    /// <summary>
    /// Interaction logic for DebugProxyWindow.xaml
    /// </summary>
    public partial class DebugProxyWindow : Window
    {
        private class DebugProxy : IProxy
        {
            private DebugProxyWindow Window;

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

        public DebugProxyWindow()
        {
            LeagueClient.Proxy = new DebugProxy(this);
            LeagueSocket.DumpToDebug = true;
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

            LeagueSocket.HandleEvent(ev);
        }

        private void List_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(Events[List.SelectedIndex].Json);
        }
    }
}