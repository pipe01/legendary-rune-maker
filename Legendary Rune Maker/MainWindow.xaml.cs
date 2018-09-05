using LCU.NET;
using LCU.NET.API_Models;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Rune_providers;
using Legendary_Rune_Maker.Game;
using Legendary_Rune_Maker.Locale;
using Legendary_Rune_Maker.Pages;
using Legendary_Rune_Maker.Utils;
using Notifications.Wpf;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Legendary_Rune_Maker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool InDesigner => DesignerProperties.GetIsInDesignMode(new DependencyObject());

        public static INotificationManager NotificationManager;

        public MainWindow()
        {
            NotificationManager = new NotificationManager(Dispatcher);

            InitializeComponent();

            Frame.Navigate(new MainPage(this));

            this.Show();
            this.Activate();

            if (!InDesigner)
                Application.Current.MainWindow = this;
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (a, b) => Taskbar.Dispose();
        }

        public static void ShowNotification(string title, string message = null, NotificationType type = NotificationType.Information)
        {
            NotificationManager.Show(new NotificationContent
            {
                Title = title,
                Message = message,
                Type = type
            });
        }

        public void SetSize(double width, double height, TimeSpan? duration = null)
        {
            var animDuration = duration ?? TimeSpan.FromSeconds(0.25);

            if (double.IsNaN(MainGrid.Width))
                MainGrid.Width = 0;
            if (double.IsNaN(MainGrid.Height))
                MainGrid.Height = 0;

            if (width != MainGrid.Width)
            {
                var anim = new DoubleAnimation(MainGrid.Width, width, animDuration)
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                MainGrid.BeginAnimation(WidthProperty, anim);
            }

            if (height != MainGrid.Height)
            {
                var anim = new DoubleAnimation(MainGrid.Height, height, animDuration)
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                MainGrid.BeginAnimation(HeightProperty, anim);
            }
        }

        private async void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                await Task.Delay(500); //Wait for Windows' window minimize animation to finish, cuz it looks K00L
                this.Hide();
            }
        }

        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            }
        }

        private async void Taskbar_Exit(object sender, RoutedEventArgs e)
        {
            await Task.Delay(150);

            this.Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                new DebugProxyWindow().Show();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Taskbar.Dispose();
            Environment.Exit(0);
        }

        public void Settings()
        {
            new SettingsWindow().ShowDialog(this);
        }
    }
}
