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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Legendary_Rune_Maker.Overlay.Controls
{
    /// <summary>
    /// Interaction logic for SummonerSlot.xaml
    /// </summary>
    public partial class SummonerSlot : UserControl
    {
        public static readonly DependencyProperty EnemyDataProperty = DependencyProperty.Register("EnemyData", typeof(Enemy), typeof(SummonerSlot));
        public Enemy EnemyData
        {
            get { return (Enemy)GetValue(EnemyDataProperty); }
            set { SetValue(EnemyDataProperty, value); }
        }

        public bool OnBanPhase { get; set; }

        public int AvatarLeft => OnBanPhase ? 142 : 181;

        public SummonerSlot()
        {
            InitializeComponent();
        }
    }
}
