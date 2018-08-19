using LoL_Rune_Maker.Data;
using LoL_Rune_Maker.Utils;
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

namespace LoL_Rune_Maker
{
    /// <summary>
    /// Interaction logic for GrayscaleImageControl.xaml
    /// </summary>
    public partial class GrayscaleImageControl : UserControl
    {
        private BitmapSource Normal, Gray;
        private string URL;
        public bool Selected
        {
            get { return (bool)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); SetSelected(); }
        }
        public static readonly DependencyProperty SelectedProperty =
            DependencyProperty.Register("Selected", typeof(bool), typeof(GrayscaleImageControl));

        public GrayscaleImageControl(string url)
        {
            this.URL = url;
            InitializeComponent();
        }

        private async void UserControl_Initialized(object sender, EventArgs e)
        {
            this.Normal = await ImageCache.Instance.Get(this.URL);
            this.Gray = await ImageCache.Instance.GetGrayscale(this.URL);

            View.Source = this.Gray;
        }

        private void SetSelected()
        {
            View.Source = Selected ? this.Normal : this.Gray;
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Selected = true;
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!Selected)
                View.Source = this.Normal;
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!Selected)
                View.Source = this.Gray;
        }
    }
}
