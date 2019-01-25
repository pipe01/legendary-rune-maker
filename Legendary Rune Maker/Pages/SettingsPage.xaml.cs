using Legendary_Rune_Maker.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Legendary_Rune_Maker.Pages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page, IPage
    {
        public SettingsPage()
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
        
        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            Restart();
        }

        private void Restart()
        {
            Config.Default.Save();

            Process.Start(Assembly.GetExecutingAssembly().Location);
            Application.Current.Shutdown();
        }

        private void LanguageCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.Default.CultureName = Config.AvailableLanguages[LanguageCb.SelectedIndex];
        }

        public Size GetSize() => new Size(this.Width, this.Height);
        
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Config.Reload();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Config.Default.Save();
            NavigationService.GoBack();
        }

        private void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Wipe cache? This will automatically restart the application.\n" +
                    "Please note that it may take some time to regenerate the cache.", "Wipe cache?", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
            {
                try
                {
                    Directory.Delete(ImageCache.Instance.FullCachePath, true);
                }
                finally
                {
                    Restart();
                }
            }
        }
    }
}
