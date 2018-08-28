using Legendary_Rune_Maker.Controls;
using Legendary_Rune_Maker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for PickChampionDialog.xaml
    /// </summary>
    public partial class PickChampionDialog : Window
    {
        public PickChampionDialog()
        {
            InitializeComponent();
        }

        public bool ShowNoChampion { get; set; } = true;

        public Champion SelectedChampion { get; private set; }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            if (ShowNoChampion)
                Champions.Items.Add(null);

            foreach (var item in await Riot.GetChampions())
            {
                Champions.Items.Add(item);
            }
        }

        private void Champion_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SelectedChampion = ((ChampionImageControl)sender).Champion;

            this.DialogResult = true;
            this.Close();
        }

        public static (bool Success, Champion Selected) PickChampion(bool ban = false)
        {
            var win = new PickChampionDialog();

            if (ban)
                win.BackImage.ImageSource = (ImageSource)Application.Current.FindResource("BgRed");

            if (win.ShowDialog() != true)
                return (false, null);

            return (true, win.SelectedChampion);
        }
    }
}
