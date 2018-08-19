using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.Plugins.LoL;
using LoL_Rune_Maker.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

namespace LoL_Rune_Maker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private int[] SelectedRunes => Tree.SelectedRunes.Concat(Second.SelectedRunes).Where(o => o != null).Select(o => o.ID).ToArray();

        private async void Window_Initialized(object sender, EventArgs e)
        {
            if (!LeagueClient.TryInit())
            {
                MessageBox.Show("Make sure the League of Legends client is open!");
                this.Close();
            }

            await Riot.CacheAllImages();

            RuneTree[] trees = await Riot.GetRuneTrees();

            Tree.SetTree(trees[0]);
            Second.SetTree(trees[1]);
            await SetSecondaryTrees();
        }

        private async Task SetSecondaryTrees()
        {
            await Second.SetValidTrees((await Riot.GetRuneTreesByID()).Keys.Where(o => o != Tree.SelectedTree.ID).ToArray());
        }

        private async void Tree_SelectedTreeChanged(object sender, int e)
        {
            await SetSecondaryTrees();
        }
        
        private async void Upload_Click(object sender, EventArgs e)
        {
            var page = new RunePage(SelectedRunes, Tree.SelectedTree.ID, Second.SelectedTree.ID);
            await page.UploadToClient();
        }

        private void Tree_SelectedRunesChanged(object sender, EventArgs e)
        {
            Upload.IsEnabled = SelectedRunes.Length == 6;
        }
    }
}
