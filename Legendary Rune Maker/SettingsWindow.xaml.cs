using Legendary_Rune_Maker.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
    }
}
