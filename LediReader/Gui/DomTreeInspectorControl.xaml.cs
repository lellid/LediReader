// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HtmlToFlowDocument.Dom;

namespace LediReader.Gui
{
  /// <summary>
  /// Interaction logic for DomTreeInspectorControl.xaml
  /// </summary>
  public partial class DomTreeInspectorControl : UserControl
  {
    public DomTreeInspectorController Controller { get; private set; }
    public DomTreeInspectorControl()
    {
      InitializeComponent();

      _guiTree.ItemContainerGenerator.ItemsChanged -= EhItemsContainer_Changed;
      _guiTree.ItemContainerGenerator.ItemsChanged += EhItemsContainer_Changed;
      _guiTree.ItemContainerGenerator.StatusChanged -= EhItemsContainer_Changed;
      _guiTree.ItemContainerGenerator.StatusChanged += EhItemsContainer_Changed;

      Controller = new DomTreeInspectorController();
      this.DataContext = Controller;

    }


    private void EhSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
      Controller.SelectedTextElement = e.NewValue as HtmlToFlowDocument.Dom.TextElement;
    }



    private void EhItemsContainer_Changed(object sender, EventArgs e)
    {
      var containerGenerator = (sender as ItemContainerGenerator) ?? _guiTree.ItemContainerGenerator;

      for (int i = Controller.SelectedHierarchyOfInitiallySelectedItem.Count - 1; i >= 0; --i)
      {
        var te = Controller.SelectedHierarchyOfInitiallySelectedItem[i];
        var item = containerGenerator.ContainerFromItem(te);
        if (item is TreeViewItem treeViewItem && !treeViewItem.IsExpanded)
        {
          treeViewItem.ItemContainerGenerator.StatusChanged -= EhItemsContainer_Changed;
          treeViewItem.ItemContainerGenerator.StatusChanged += EhItemsContainer_Changed;
          treeViewItem.ItemContainerGenerator.ItemsChanged -= EhItemsContainer_Changed;
          treeViewItem.ItemContainerGenerator.ItemsChanged += EhItemsContainer_Changed;

          treeViewItem.IsExpanded = true;
          if (i == 0)
          {
            treeViewItem.IsSelected = true;
            treeViewItem.BringIntoView();
          }
        }
      }
    }
  }
}
