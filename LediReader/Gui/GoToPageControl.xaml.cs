// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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

namespace LediReader.Gui
{
  /// <summary>
  /// Interaction logic for GoToPageControl.xaml
  /// </summary>
  public partial class GoToPageControl : UserControl
  {
    public GotoPageController Controller { get; private set; }
    public GoToPageControl()
    {
      Controller = new GotoPageController();
      this.DataContext = Controller;
      InitializeComponent();
    }
  }
}
