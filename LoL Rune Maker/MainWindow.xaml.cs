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

        private async void Window_Initialized(object sender, EventArgs e)
        {
            await Riot.CacheAllImages();

            RuneTree[] trees = await Riot.GetRuneTrees();

            Tree.SetTree(trees[0]);
            Second.SetTree(trees[1]);
            await Second.SetValidTrees((await Riot.GetRuneTrees()).Skip(1).Select(o => o.ID).ToArray());
        }

        private async void Tree_SelectedTreeChanged(object sender, int e)
        {
            await Second.SetValidTrees((await Riot.GetRuneTreesByID()).Keys.Where(o => o != e).ToArray());
        }
    }
}
