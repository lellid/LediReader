using SlobViewer.Common;
using SlobViewer.Slob;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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

namespace SlobViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class DictionaryControl : UserControl
    {
        public DictionaryController Controller { get; private set; } = new DictionaryController();


        public DictionaryControl()
        {
            InitializeComponent();
            this.DataContext = Controller;
        }

        /*
        private void EhSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = ((TextBox)sender).Text;
            Controller.ShowContentForKey(searchText);
            Controller.UpdateBestMatches(searchText);
        }
        */

        private void EhSearchSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 == Controller.SearchListLock)
            {
                if (e.AddedItems.Count > 0)
                {
                    var item = e.AddedItems[0];
                    var s = (string)item;
                    _guiSearchText.Text = s;
                }
            }

            if (null != _guiSearchList.SelectedItem)
                _guiSearchList.ScrollIntoView(_guiSearchList.SelectedItem);
        }

        private void EhBestMatchSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var s = (string)e.AddedItems[0];
                Controller.ShowContentForKey(s);
            }
        }

        private void EhHide(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
        }
    }
}
