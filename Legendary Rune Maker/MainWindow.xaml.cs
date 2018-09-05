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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Legendary_Rune_Maker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool InDesigner => DesignerProperties.GetIsInDesignMode(new DependencyObject());

        public static INotificationManager NotificationManager;

        private bool AllowDirectNavigation;

        private readonly TimeSpan TransitionDuration = TimeSpan.FromSeconds(0.18);

        public MainWindow()
        {
            NotificationManager = new NotificationManager(Dispatcher);

            InitializeComponent();

            Rect workArea = SystemParameters.WorkArea;
            this.Left = (workArea.Width - this.Width) / 2 + workArea.Left;
            this.Top = (workArea.Height - this.Height) / 2 + workArea.Top;

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

        private void Current_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            //Don't animate the first time the window is opened
            if (Frame.Content == null)
                return;

            if (e.Content != null && !AllowDirectNavigation)
            {
                e.Cancel = true;
                AllowDirectNavigation = true;

                if (e.Content is IPage page)
                {
                    var size = page.GetSize();
                    SetSize(size.Width, size.Height);
                }

                var anim = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(TransitionDuration.TotalSeconds * .5));
                anim.Completed += (_, __) => SlideCompleted(e);
                Frame.BeginAnimation(OpacityProperty, anim);
            }
            else
            {
                AllowDirectNavigation = false;
            }
        }

        private void SlideCompleted(NavigatingCancelEventArgs navArgs)
        {
            AllowDirectNavigation = true;

            switch (navArgs.NavigationMode)
            {
                case NavigationMode.New:
                    if (navArgs.Uri == null)
                        Frame.Navigate(navArgs.Content);
                    else
                        Frame.Navigate(navArgs.Uri);
                    break;
                case NavigationMode.Back:
                    Frame.GoBack();
                    break;
                case NavigationMode.Forward:
                    Frame.GoForward();
                    break;
                case NavigationMode.Refresh:
                    Frame.Refresh();
                    break;
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                (ThreadStart)(() =>
                {
                    var anim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(TransitionDuration.TotalSeconds * .5));
                    Frame.BeginAnimation(OpacityProperty, anim);
                }));
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
            var animDuration = duration ?? TransitionDuration;

            if (double.IsNaN(MainGrid.Width))
                MainGrid.Width = 0;
            if (double.IsNaN(MainGrid.Height))
                MainGrid.Height = 0;

            if (width != MainGrid.Width)
            {
                double difference = MainGrid.Width - width;
                
                MainGrid.BeginAnimation(WidthProperty, new DoubleAnimation(MainGrid.Width, width, animDuration)
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                });
                
                this.BeginAnimation(LeftProperty, new DoubleAnimation(this.Left, this.Left + difference / 2, animDuration)
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.Stop
                });
            }

            if (height != MainGrid.Height)
            {
                double difference = MainGrid.Height - height;
                
                MainGrid.BeginAnimation(HeightProperty, new DoubleAnimation(MainGrid.Height, height, animDuration)
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                });

                this.BeginAnimation(TopProperty, new DoubleAnimation(this.Top, this.Top + difference / 2, animDuration)
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.Stop
                });
            }
        }

        private async void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized && Config.Default.MinimizeToTaskbar)
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

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
