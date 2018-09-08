using Legendary_Rune_Maker.Data;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Legendary_Rune_Maker
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            foreach (var item in Config.AvailableLanguages)
            {
                string name = new CultureInfo(item).DisplayName;
                int parenthesis = name.IndexOf('(');

                if (parenthesis >= 0)
                    name = name.Substring(0, parenthesis - 1);

                LanguageCb.Items.Add(name);

                if (item == Config.Default.CultureName)
                    LanguageCb.SelectedItem = name;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Config.Default.Save();
        }

        private void Restart_Click(object sender, EventArgs e)
        {
            Config.Default.Save();

            Process.Start(Assembly.GetExecutingAssembly().Location);
            Application.Current.Shutdown();
        }

        private void LanguageCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.Default.CultureName = Config.AvailableLanguages[LanguageCb.SelectedIndex];
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("MouseEnter");
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("MouseLeave");
        }

        private void Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("MouseDown");
        }

        private void Button_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("MouseUp");
        }
    }
}
