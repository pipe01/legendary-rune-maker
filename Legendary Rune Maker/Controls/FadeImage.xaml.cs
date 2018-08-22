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

namespace Legendary_Rune_Maker.Controls
{
    /// <summary>
    /// Interaction logic for FadeImage.xaml
    /// </summary>
    public partial class FadeImage : UserControl
    {
        public bool ShowA
        {
            get { return (bool)GetValue(ShowAProperty); }
            set { SetValue(ShowAProperty, value); }
        }
        public static readonly DependencyProperty ShowAProperty = DependencyProperty.Register("ShowA", typeof(bool), typeof(FadeImage));
        
        public FadeImage()
        {
            InitializeComponent();
        }
    }
}
