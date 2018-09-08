using Legendary_Rune_Maker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Legendary_Rune_Maker
{
    /// <summary>
    /// Interaction logic for TreePickerControl.xaml
    /// </summary>
    public partial class TreePickerControl : UserControl
    {
        private int[] ValidIDs;

        private int _SelectedTree;
        public int SelectedTree
        {
            get => _SelectedTree;
            set
            {
                _SelectedTree = value;

                if (ValidIDs == null)
                    return;

                int i = ValidIDs.ToList().IndexOf(value);
                Grid.SetColumn(Selector, i == -1 ? 0 : i);
            }
        }

        public event EventHandler SelectionChanged = delegate { };

        public TreePickerControl()
        {
            InitializeComponent();
        }

        public async Task SetIDs(int[] ids)
        {
            this.ValidIDs = ids;

            var trees = (await Riot.GetRuneTrees())
                .Where(o => ids.Contains(o.ID))
                .OrderBy(o => o.ID);

            var remove = new List<UIElement>();
            foreach (UIElement item in MainGrid.Children)
            {
                if (item != Selector)
                    remove.Add(item);
            }
            foreach (var item in remove)
            {
                MainGrid.Children.Remove(item);
            }

            MainGrid.ColumnDefinitions.Clear();

            int col = 0;
            foreach (var item in trees)
            {
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition());

                var icon = new Image
                {
                    Source = await ImageCache.Instance.Get(Riot.ImageEndpoint + item.IconURL),
                    Cursor = Cursors.Hand,
                    Tag = item,
                    Width = 23,
                    Height = 23
                };
                icon.MouseDown += Icon_MouseDown;

                MainGrid.Children.Add(icon);
                Grid.SetColumn(icon, col++);
            }

            if (ids.Contains(SelectedTree))
            {
                SelectedTree = SelectedTree;
            }
        }

        private void Icon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectedTree = ((RuneTree)((Image)sender).Tag).ID;
            SelectionChanged(this, EventArgs.Empty);
        }
    }
}
