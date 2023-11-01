// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using LediReader.Speech;
using LediReader.Translation;
using Microsoft.Win32;

namespace LediReader.Gui
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindowController Controller { get; private set; }

    private SpeechWorkerBase _speech;
    public static StartupEventArgs StartupArguments { get; set; }

    /// <summary>
    /// The last text element that is somehow marked by the user. If the user turns a page, this element is set to null, in order to ensure that 
    /// this element is always on a page visible to the user.
    /// </summary>
    private TextElement _lastTextElementConsidered;

    /// <summary>
    /// True if speech is stopped, and the user has not yet scrolled to another page.
    /// </summary>
    private bool _isInState_WaitForResumingSpeech;

    /// <summary>
    /// True if the dictionary page is currently shown on top.
    /// </summary>
    private bool _isInState_ShowingTheDictionary;


    /// <summary>
    /// The document page view visual component of the _guiViewer;
    /// </summary>
    private System.Windows.Controls.Primitives.DocumentPageView _documentPageView;

    #region Startup and Closing

    public MainWindow()
    {
      Controller = new MainWindowController();
      try
      {
        Controller.LoadSettings();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Could not load settings\r\nThe message is:\r\n{ex.Message}");
      }

      var ss = Controller.Settings.StartupSettings;

      if (ss.IsFullScreen || ss.IsMaximized)
      {
        this.WindowState = WindowState.Maximized;
      }
      else
      {
        this.WindowState = WindowState.Normal;
        this.Left = ss.Left;
        this.Top = ss.Top;
        this.Width = ss.Width;
        this.Height = ss.Height;
      }

      InitializeComponent();

#if DEBUG
      var m = new MenuItem() { Header = "Inspect DOM" };
      m.Click += EhInspectDOM;
      _guiMenuSettings.Items.Add(m);

      m = new MenuItem { Header = "Inspect XHtml" };
      m.Click += EhInspectXHtml;
      _guiMenuSettings.Items.Add(m);
#endif

      ApplyDarkTheme(Controller.Settings.BookSettings.IsBookInDarkMode, Controller.Settings.BookSettings.IsGuiInDarkMode, reRenderBook: false);
      Controller.IsInAudioMode = Controller.Settings.BookSettings.IsInAudioMode;

      this.DataContext = Controller;
      Loaded += EhLoaded;
    }



    private void EhLoaded(object sender, RoutedEventArgs e)
    {
      SetupViewPortChangeSubscription();


      _guiMenuItem_IsInAudioMode.IsChecked = Controller.IsInAudioMode;

      if (SpeechWorker_Windows10.IsSupportedByThisComputer())
        _speech = new SpeechWorker_Windows10() { IsInDarkMode = Controller.IsBookInDarkMode, Dispatcher = this.Dispatcher };
      else
        _speech = new SpeechWorker() { IsInDarkMode = Controller.IsBookInDarkMode };

      _speech.ApplySettings(Controller.Settings.SpeechSettings);
      _speech.SpeechCompleted += EhSpeechCompleted;

      Microsoft.Win32.SystemEvents.PowerModeChanged += EhPowerModeChanged; // Attention: unsubscribe to this event if the application is closing

      _guiDictionary.Controller.LoadDictionariesUsingSettings(Controller.Settings.DictionarySettings);

      HtmlToFlowDocument.Dom.FlowDocument document = null;
      Dictionary<string, string> fontDictionary = null;
      bool wasNewBookOpened = false;
      if (null != Gui.StartupSettings.StartupArguments && Gui.StartupSettings.StartupArguments.Length > 0)
      {
        (document, fontDictionary) = Controller.OpenEbook(Gui.StartupSettings.StartupArguments[0]);
        wasNewBookOpened = 0 != string.Compare(Gui.StartupSettings.StartupArguments[0], Controller.Settings.BookSettings.BookFileName, true);
      }
      else
      {
        try
        {
          (document, fontDictionary) = Controller.ReopenEbook();
          wasNewBookOpened = false;
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Could not re-open ebook {Controller.Settings.BookSettings.BookFileName}: {ex.Message}", "Error reopening ebook", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }

      if (null == document)
      {
        document = new HtmlToFlowDocument.Dom.FlowDocument();
        fontDictionary = new Dictionary<string, string>();
      }


      _guiViewer.Zoom = Controller.Settings.BookSettings.Zoom;
      ShowFlowDocument(document, fontDictionary);
      SlobViewer.Gui.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);

      bool navigated = false;
      if (Controller.Settings.BookSettings.Bookmark != null && !wasNewBookOpened)
      {
        var flowDocument = (FlowDocument)_guiViewer.Document;
        var textElement = HtmlToFlowDocument.Rendering.WpfHelper.GetTextElementFromBookmark(flowDocument, Controller.Settings.BookSettings.Bookmark);
        if (null != textElement)
        {
          textElement.BringIntoView();
          if (textElement is TextElement te)
            _guiViewer.Selection.Select(te.ContentStart, te.ContentEnd);
          _guiViewer.Focus();
          navigated = true;
        }
      }

      if (!navigated)
      {
        if (_guiViewer.CanGoToPage(Controller.Settings.BookSettings.PageNumber))
        {
          _guiViewer.GoToPage(Controller.Settings.BookSettings.PageNumber);
        }
      }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      // Update file paths
      Controller.UpdateDictionaryFileNames(_guiDictionary.Controller.Dictionaries.Select(x => x.FileName));

      if (_speech.IsSpeechSynthesizingActive)
      {
        _speech.StopSpeech();

        for (int i = 0; i < 100 && _speech.IsSpeechSynthesizingActive; ++i)
        {
          System.Threading.Thread.Sleep(100);
        }

        // save the last word spoken

        _lastTextElementConsidered = _speech.LastSpokenElement;
      }

      if (null == _lastTextElementConsidered)
      {
        _lastTextElementConsidered = _guiViewer.GetTextElementAtViewerPosition(new Point(0, 0));
      }

      if (null != _lastTextElementConsidered)
      {
        var bookmark = HtmlToFlowDocument.Rendering.WpfHelper.GetBookmarkFromTextElement(_lastTextElementConsidered);
        Controller.Settings.BookSettings.Bookmark = bookmark;
      }


      // save the current page
      Controller.Settings.BookSettings.PageNumber = _guiViewer.MasterPageNumber;
      Controller.Settings.BookSettings.Zoom = _guiViewer.Zoom;
      _speech.GetSettings(Controller.Settings.SpeechSettings);

      // save windows state
      {
        var ss = Controller.Settings.StartupSettings;
        ss.Left = this.Left;
        ss.Top = this.Top;
        ss.Width = this.ActualWidth;
        ss.Height = this.ActualHeight;
        ss.IsMaximized = this.WindowState == WindowState.Maximized;
        ss.IsFullScreen = false;
      }

      Controller.SaveSettings();

      Microsoft.Win32.SystemEvents.PowerModeChanged -= EhPowerModeChanged;

      base.OnClosing(e);
    }

    protected FlowDocument FlowDocument => _guiViewer.Document as FlowDocument;

    private void EhPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
      switch (e.Mode)
      {
        case PowerModes.Resume:
          break;
        case PowerModes.StatusChange:
          break;
        case PowerModes.Suspend:
          _speech.StopSpeech();
          break;
        default:
          break;
      }
    }

    #endregion Startup and Closing

    #region Viewport caluclation

    public class ViewPortProperties : INotifyPropertyChanged
    {
      private double _width = 800, _height = 600;

      public event PropertyChangedEventHandler PropertyChanged;

      public double Width
      {
        get
        {
          return _width;
        }
        set
        {
          if (!(_width == value))
          {
            _width = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Width)));
          }
        }
      }
      public double Height
      {
        get
        {
          return _height;
        }
        set
        {
          if (!(_height == value))
          {
            _height = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Height)));
          }
        }
      }
    }

    private ViewPortProperties _viewPortProperties = new ViewPortProperties();


    /// <summary>
    /// Setup the subscription to the events that follow a change in the document's viewport size.
    /// </summary>
    private void SetupViewPortChangeSubscription()
    {
      // Subscribe to change in ActualWidth, ActualHeight of the document page viewer, and Zoom of the _guiViewer

      _documentPageView = FindVisualChildren<System.Windows.Controls.Primitives.DocumentPageView>(_guiViewer).FirstOrDefault();
      if (null != _documentPageView)
      {
        var pd = DependencyPropertyDescriptor.FromProperty(FlowDocumentPageViewer.ActualWidthProperty, typeof(FrameworkElement));
        pd.AddValueChanged(_documentPageView, EhDocumentPageView_ViewPortChanged);
        pd = DependencyPropertyDescriptor.FromProperty(FlowDocumentPageViewer.ActualHeightProperty, typeof(FrameworkElement));
        pd.AddValueChanged(_documentPageView, EhDocumentPageView_ViewPortChanged);
      }
      else
      {
        var pd = DependencyPropertyDescriptor.FromProperty(FlowDocumentPageViewer.ActualWidthProperty, typeof(FrameworkElement));
        pd.AddValueChanged(_guiViewer, EhDocumentPageView_ViewPortChanged);
        pd = DependencyPropertyDescriptor.FromProperty(FlowDocumentPageViewer.ActualHeightProperty, typeof(FrameworkElement));
        pd.AddValueChanged(_guiViewer, EhDocumentPageView_ViewPortChanged);
      }

      {
        // Setup subscription to Zoom...
        var pd = DependencyPropertyDescriptor.FromProperty(FlowDocumentPageViewer.ZoomProperty, typeof(FlowDocumentPageViewer));
        pd.AddValueChanged(_guiViewer, EhDocumentPageView_ViewPortChanged);
      }
    }



    /// <summary>
    /// Is called if the document's viewport has changed.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void EhDocumentPageView_ViewPortChanged(object sender, EventArgs e)
    {
      CalculateViewPortProperties();
    }

    /// <summary>
    /// Calculates the size of the document's view port and stores the result in the member <see cref="_viewPortProperties"/>, in order
    /// to be used by the bindings in the document's elements.
    /// </summary>
    private void CalculateViewPortProperties()
    {
      var flowDocument = _guiViewer.Document as FlowDocument;
      var documentPadding = flowDocument.PagePadding;

      if (null != _documentPageView)
      {
        if (_documentPageView.ActualWidth > 0 && _documentPageView.ActualHeight > 0)
        {
          var w = _documentPageView.ActualWidth;
          if (documentPadding.Left > 0)
            w -= documentPadding.Left;
          if (documentPadding.Right > 0)
            w -= documentPadding.Right;

          var h = _documentPageView.ActualHeight;
          if (documentPadding.Top > 0)
            h -= documentPadding.Top;
          if (documentPadding.Bottom > 0)
            h -= documentPadding.Bottom;

          /*
          if (_viewPortProperties.Width != w || _viewPortProperties.Height != h)
          {
            System.Diagnostics.Debug.WriteLine($"AW={_documentPageView.ActualWidth}, AH={_documentPageView.ActualHeight}, Z={_guiViewer.Zoom}, w={w}, h={h}");
          }
          */

          _viewPortProperties.Width = w * 100.0 / _guiViewer.Zoom;
          _viewPortProperties.Height = h * 100.0 / _guiViewer.Zoom;
        }
      }
      else
      {
        // Fall back method, if _documentPageView could not be found...
        double toolBarHeight = 0;
        var toolbarHost = FindVisualChildren<FrameworkElement>(_guiViewer).Where(x => x.Name == "PART_FindToolBarHost").FirstOrDefault();
        if (null != toolbarHost)
        {
          // we need the parent of the toolbarhost
          var toolbarHostParent = VisualTreeHelper.GetParent(toolbarHost) as FrameworkElement;
          if (toolbarHostParent != null)
            toolBarHeight = toolbarHostParent.ActualHeight;
        }
        _viewPortProperties.Width = (_guiViewer.ActualWidth - _guiViewer.Padding.Right - _guiViewer.Padding.Left) * 100.0 / _guiViewer.Zoom;
        _viewPortProperties.Height = (_guiViewer.ActualHeight - _guiViewer.Padding.Top - _guiViewer.Padding.Bottom - toolBarHeight) * 100.0 / _guiViewer.Zoom;
      }
    }

    /// <summary>
    /// Helper class to enumerate all visual children of a given type of a parent element.
    /// </summary>
    /// <typeparam name="T">The type of children to return.</typeparam>
    /// <param name="parent">The parent element.</param>
    /// <returns>All visual children of the parent element of the given type.</returns>
    public static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
      int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
      for (int i = 0; i < childrenCount; i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);
        if (child is T tchild)
          yield return tchild;

        foreach (var other in FindVisualChildren<T>(child))
        {
          yield return other;
        }
      }
    }

    #endregion Viewport calculation


    private void ShowFlowDocument(HtmlToFlowDocument.Dom.FlowDocument flowDocument, Dictionary<string, string> fontDictionary)
    {
      if (null != flowDocument)
      {
        var renderer = new HtmlToFlowDocument.Rendering.WpfRenderer() { InvertColors = Controller.IsBookInDarkMode, AttachDomAsTags = true, FontDictionary = fontDictionary };
        renderer.TemplateBindingViewportWidth = new Binding(nameof(ViewPortProperties.Width)) { Source = _viewPortProperties };
        renderer.TemplateBindingViewportHeight = new Binding(nameof(ViewPortProperties.Height)) { Source = _viewPortProperties };

        var flowDocumentE = renderer.Render(flowDocument);

        flowDocumentE.IsColumnWidthFlexible = false;

        // var binding1 = new Binding("ActualWidth") { Source = _guiViewer };
        // flowDocumentE.SetBinding(FlowDocument.ColumnWidthProperty, binding1); // Make sure the ColumnWidth property is same as the actual width of the flow document

        // Note: binding ActualHeight to PageHeight seems not a good idea,
        // since the Zoom have to be taken into account. Otherwise zooming to increase font size will no longer work

        _guiViewer.Document = flowDocumentE;

        flowDocumentE.ColumnWidth = double.MaxValue;
        flowDocumentE.ColumnGap = 0; // ColumnWidth=infinity and columngap=0 are neccessary to span the text to a maximum width
        flowDocumentE.PagePadding = new Thickness(8); // set page padding to zero instead of auto


        CalculateViewPortProperties();
      }
      else
      {
        _guiViewer.Document = null;
      }
    }


    #region Gui event handlers





    private void ApplyDarkTheme(bool isBookInDarkMode, bool isGuiInDarkMode, bool reRenderBook)
    {
      Controller.IsBookInDarkMode = isBookInDarkMode;
      Controller.IsGuiInDarkMode = isGuiInDarkMode;
      Controller.Settings.BookSettings.IsBookInDarkMode = isBookInDarkMode;
      Controller.Settings.BookSettings.IsGuiInDarkMode = isGuiInDarkMode;
      Controller.Settings.DictionarySettings.IsInDarkMode = isBookInDarkMode;



      if (null != _speech)
        _speech.IsInDarkMode = isBookInDarkMode;

      var themes = new List<Uri>();

      if (isBookInDarkMode)
        themes.Add(new Uri("pack://application:,,,/Themes/BookStylesDark.xaml"));
      else
        themes.Add(new Uri("pack://application:,,,/Themes/BookStylesLight.xaml"));

      if (isGuiInDarkMode)
        themes.Add(new Uri("pack://application:,,,/Themes/GuiStylesDark.xaml"));
      else
        themes.Add(new Uri("pack://application:,,,/Themes/GuiStylesLight.xaml"));

      AppThemeSelector.ApplyTheme(themes.ToArray());

      if (reRenderBook)
      {
        var (doc, fontDictionary) = Controller.ReopenEbook();
        ShowFlowDocument(doc, fontDictionary);
      }
    }




    private void EhOpenBook(object sender, RoutedEventArgs e)
    {
      var dlg = new OpenFileDialog
      {
        Filter = "Epub files|*.epub" +
                      "|All Files|*.*",
        Multiselect = false,
        Title = "Open Ebook (Epub format)"
      };

      if (true == dlg.ShowDialog(this))
      {
        var (flowDocument, fontDictionary) = Controller.OpenEbook(dlg.FileName);
        ShowFlowDocument(flowDocument, fontDictionary);
      }
    }

    private void EhSpeechSettings(object sender, RoutedEventArgs e)
    {
      var controller = new SpeechSettingsController();
      controller.Initialize(Controller.Settings.SpeechSettings);
      controller.Synthesizer = _speech;
      var control = new SpeechSettingsControl(controller);


      var window = new DialogShellViewWpf(control);

      if (true == window.ShowDialog())
      {
        controller.Apply(Controller.Settings.SpeechSettings);
        _speech.ApplySettings(Controller.Settings.SpeechSettings);
      }
    }


    private void EhTranslationSettings(object sender, RoutedEventArgs e)
    {
      var controller = new TranslationSettingsController();
      controller.Initialize(Controller.Settings.TranslationSettings);
      var control = new TranslationSettingsControl() { DataContext = controller };

      var window = new DialogShellViewWpf(control);

      if (true == window.ShowDialog())
      {
        controller.Apply(Controller.Settings.TranslationSettings);
      }
    }

    private void EhViewer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      Action_ShowDictionaryForCurrentSelection();
    }


    private void EhStartSpeech(object sender, RoutedEventArgs e)
    {
      Action_StartSpeech();
    }

    private void EhStopSpeech(object sender, RoutedEventArgs e)
    {
      Action_StopSpeech();
    }

    private void EhImportTeiFile(object sender, RoutedEventArgs e)
    {
      SlobViewer.Gui.GuiActions.ImportTeiFile(_guiDictionary.Controller, this);
      SlobViewer.Gui.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);
    }

    private void EhImportTUChemnitzFile(object sender, RoutedEventArgs e)
    {
      SlobViewer.Gui.GuiActions.ImportTUChemnitzFile(_guiDictionary.Controller, this);
      SlobViewer.Gui.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);
    }

    private void EhImportKaikkiFile(object sender, RoutedEventArgs e)
    {
      SlobViewer.Gui.GuiActions.ImportKaikkiFile(_guiDictionary.Controller, this);
      SlobViewer.Gui.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);
    }


    private void EhOpenSlobFile(object sender, RoutedEventArgs e)
    {
      SlobViewer.Gui.GuiActions.OpenSlobFile(_guiDictionary.Controller, this);
      SlobViewer.Gui.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);
    }

    private void EhOpenStarDictFile(object sender, RoutedEventArgs e)
    {
      SlobViewer.Gui.GuiActions.OpenStarDictFile(_guiDictionary.Controller, this);
      SlobViewer.Gui.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);
    }

    private void EhGotoPage(object sender, RoutedEventArgs e)
    {
      Action_GotoPage();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {

      switch (e.Key)
      {
        case Key.PageUp:
          Action_GotoPreviousPage();
          e.Handled = true;
          break;
        case Key.PageDown:
          Action_GotoNextPage();
          e.Handled = true;
          break;
        case Key.Left:
          Action_PressLeftSide();
          e.Handled = true;
          break;
        case Key.Right:
          Action_PressRightSide();
          e.Handled = true;
          break;
      }

      base.OnPreviewKeyDown(e);
    }

    #endregion Gui event handlers

    #region Actions

    private void Action_GotoPage()
    {
      _speech.StopSpeech();
      var control = new GoToPageControl();
      control.Controller.MaxPageNumber = _guiViewer.PageCount;
      control.Controller.PageNumber = _guiViewer.MasterPageNumber;

      var dlg = new DialogShellViewWpf(control);

      dlg.ButtonApplyPressed += () => _guiViewer.GoToPage(control.Controller.PageNumber);

      if (dlg.ShowDialog() == true)
      {
        _guiViewer.GoToPage(control.Controller.PageNumber);
        QuitWaitingForResumingSpeech();
      }
    }

    private void Action_GotoNextPage()
    {
      _speech.StopSpeech();
      _guiViewer.NextPage();
      QuitWaitingForResumingSpeech();
    }

    private void Action_GotoPreviousPage()
    {
      _speech.StopSpeech();
      _guiViewer.PreviousPage();
      QuitWaitingForResumingSpeech();
    }

    private void QuitWaitingForResumingSpeech()
    {
      _lastTextElementConsidered = null;
      if (!_guiViewer.Selection.IsEmpty)
      {
        _guiViewer.Selection.Select(_guiViewer.Selection.Start, _guiViewer.Selection.Start);
      }
      _isInState_WaitForResumingSpeech = false;
    }

    private void Action_PressRightSide()
    {
      if (_isInState_ShowingTheDictionary)
      {
        Action_HideDictionary();
      }
      else if (_speech.IsSpeechSynthesizingActive)
      {
        // TODO yet to be defined what should happen: e.g. Goto the next paragraph?
      }
      else if (_isInState_WaitForResumingSpeech)
      {
        Action_StartSpeech();
      }
      else if (Controller.IsInAudioMode && !_guiViewer.Selection.IsEmpty)
      {
        Action_StartSpeech();
      }
      else
      {
        Action_GotoNextPage();
      }
    }

    private void Action_PressLeftSide()
    {
      if (_speech.IsSpeechSynthesizingActive)
      {
        Action_StopSpeech();
      }
      else if (_isInState_WaitForResumingSpeech)
      {
        QuitWaitingForResumingSpeech();
      }
      else if (Controller.IsInAudioMode && !_guiViewer.Selection.IsEmpty)
      {
        _guiViewer.Selection.Select(_guiViewer.Selection.Start, _guiViewer.Selection.Start);
      }
      else
      {
        Action_GotoPreviousPage();
      }
    }

    private void Action_StartSpeech()
    {
      if (!_guiViewer.Selection.IsEmpty && _guiViewer.Selection.Start.Parent is TextElement te1)
      {
        _guiViewer.Selection.Select(_guiViewer.Selection.Start, _guiViewer.Selection.Start);
        _speech.StartSpeech(te1);
      }
      else if (null != _lastTextElementConsidered)
      {
        _speech.StartSpeech(_lastTextElementConsidered);
      }
      else if (_guiViewer.Selection.IsEmpty)
      {
        var te2 = _guiViewer.GetTextElementAtViewerPosition(new Point(0, 0));
        if (null != te2)
        {
          _speech.StartSpeech(te2);
        }
      }
    }

    private void Action_StopSpeech()
    {
      _speech.StopSpeech();
    }

    private void EhSpeechCompleted(TextElement lastSpokenElement)
    {
      if (null != lastSpokenElement)
      {
        _lastTextElementConsidered = lastSpokenElement;
        _guiViewer.Selection.Select(lastSpokenElement.ContentStart, lastSpokenElement.ContentEnd);
        _isInState_WaitForResumingSpeech = true;
      }
    }



    private void Action_ShowDictionaryForCurrentSelection()
    {
      var selection = _guiViewer.Selection;
      if (!selection.IsEmpty)
      {
        Action_ShowDictionary(selection.Text);
      }
    }

    private void Action_ShowDictionary(string phrase)
    {
      _speech.StopSpeech();
      if (_guiDictionary.Controller.Dictionaries.Count > 0)
      {
        _isInState_ShowingTheDictionary = true;
        _guiDictionary.Visibility = Visibility.Visible;
        _guiDictionary.Margin = new Thickness(0, 0, _guiViewer.Margin.Right, 0);
        _guiDictionary.Controller.IsInDarkMode = Controller.IsBookInDarkMode;
        _guiDictionary.Controller.ShowContentForUntrimmedKey(phrase);
      }
    }

    private void Action_HideDictionary()
    {
      _isInState_ShowingTheDictionary = false;
      _guiDictionary.Visibility = Visibility.Hidden;
    }

    private void EhViewerMargin_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (object.ReferenceEquals(sender, _guiViewerBackground))
      {
        var pos = e.GetPosition(_guiViewerBackground);
        if (pos.X <= _guiViewer.Margin.Left) // left side
        {
          Action_PressLeftSide();
          e.Handled = true;
        }
        else if (pos.X > _guiViewerBackground.ActualWidth - _guiViewer.Margin.Right) // right side
        {
          Action_PressRightSide();
          e.Handled = true;
        }
      }
      else if (object.ReferenceEquals(sender, _guiViewer))
      {
        if (_speech.IsSpeechSynthesizingActive)
        {
          var pos = e.GetPosition(_guiViewer);
          _speech.PauseSpeech(); // firstly, we pause the speech only (if we stop, the next call will not find the text element)
          var te = _guiViewer.GetTextElementAtViewerPosition(pos); // This call is very time consuming (up to some seconds (!))  TODO find something faster
          _speech.StopSpeech(); // now we can stop speech completely
          if (te is Run run)
          {
            Action_ShowDictionary(run.Text);
            e.Handled = true;
          }
        }
      }
    }

    private void EhBookSettings(object sender, RoutedEventArgs e)
    {
      var control = new BookSettingsControl();
      var controller = control.Controller;
      controller.Initialize(this.Controller.Settings.BookSettings);
      var dlg = new DialogShellViewWpf(control);
      if (true == dlg.ShowDialog())
      {
        controller.Apply(this.Controller.Settings.BookSettings);
        ApplyBookSettings(this.Controller.Settings.BookSettings);
      }
    }

    private void ApplyBookSettings(Book.BookSettings settings)
    {
      _guiViewer.Margin = new Thickness(settings.LeftAndRightMargin, 0, settings.LeftAndRightMargin, 0);
      ApplyDarkTheme(settings.IsBookInDarkMode, settings.IsGuiInDarkMode, settings.IsBookInDarkMode != Controller.IsBookInDarkMode);
    }

    private void EhRegisterApplication(object sender, RoutedEventArgs e)
    {
      Registration.AppRegistration.RegisterApplication();
    }

    private void EhUnregisterApplication(object sender, RoutedEventArgs e)
    {
      Registration.AppRegistration.UnregisterApplication();
    }

    private void EhChangeBetweenAudioAndReadingMode(object sender, RoutedEventArgs e)
    {
      Controller.IsInAudioMode = _guiMenuItem_IsInAudioMode.IsChecked == true;
    }





    #endregion


    #region Inspection of elements (debugging only)

    private void EhInspectDOM(object sender, RoutedEventArgs e)
    {
      if (_guiViewer.Selection.Start?.Parent is FrameworkContentElement fce)
      {
        while (fce.Tag == null && fce.Parent is FrameworkContentElement)
        {
          fce = fce.Parent as FrameworkContentElement;
        }
        if (fce.Tag is HtmlToFlowDocument.Dom.TextElement textElement)
        {
          var control = new DomTreeInspectorControl();
          var controller = control.Controller;
          var root = HtmlToFlowDocument.Dom.TextElementExtensions.GetRootElement(textElement);
          controller.DomRootElement = root;
          controller.SelectedTextElement = textElement;
          var dlg = new DialogShellViewWpf(control);
          dlg.ShowDialog();
        }
      }
    }

    private void EhInspectXHtml(object sender, RoutedEventArgs e)
    {
      if (_guiViewer.Selection.Start?.Parent is FrameworkContentElement fce)
      {
        while ((!(fce.Tag is HtmlToFlowDocument.Dom.TextElement te) || !(te.Tag is System.Xml.XmlElement)) && fce.Parent is FrameworkContentElement)
        {
          fce = fce.Parent as FrameworkContentElement;
        }

        if (fce.Tag is HtmlToFlowDocument.Dom.TextElement textElement && textElement.Tag is System.Xml.XmlElement xe)
        {
          var control = new XHtmlTreeInspectorControl();
          var controller = control.Controller;
          var root = GetRootElement(xe);
          controller.DomRootElement = root;
          controller.SelectedTextElement = xe;
          var dlg = new DialogShellViewWpf(control);
          if (root.Attributes != null && root.Attributes["OriginatedFromSource"] != null)
            dlg.Title = root.Attributes["OriginatedFromSource"].Value;

          dlg.ShowDialog();
        }
      }
    }

    private static System.Xml.XmlNode GetRootElement(System.Xml.XmlNode te)
    {
      var result = te;

      while (result.ParentNode != null && !(result.ParentNode is System.Xml.XmlDocument))
      {
        result = result.ParentNode;
      }

      return result;
    }

    private void EhTranslateExternal(object sender, RoutedEventArgs e)
    {
      var selection = _guiViewer.Selection;
      if (selection.IsEmpty)
        return;

      var originalStart = selection.Start;
      var originalEnd = selection.End;
      var originalText = selection.Text;

      var refStart = selection.Start.Paragraph.ContentStart;
      var refEnd = selection.End.Paragraph.ContentEnd;

      selection.Select(originalStart, refEnd);
      var paraEndText = selection.Text;

      selection.Select(refStart, refEnd);
      var paraText = selection.Text;
      _guiViewer.Selection.Select(originalStart, originalEnd);


      int startOffset = 0, endOffset = paraText.Length - 1;
      for (int i = paraText.Length - paraEndText.Length; i >= 0; --i)
      {
        var c = paraText[i];
        if (c == '.' || c == '!' || c == '?')
        {
          startOffset = i + 1;
          break;
        }
      }

      for (int i = paraText.Length - paraEndText.Length + originalText.Length; i < paraText.Length; ++i)
      {
        var c = paraText[i];
        if (c == '.' || c == '!' || c == '?')
        {
          endOffset = i;
          break;
        }
      }

      var searchText = paraText.Substring(startOffset, endOffset - startOffset + 1);

      var isoLanguage = Controller.Settings.TranslationSettings.DestinationLanguageThreeLetterISOLanguageName;
      var culture = CultureInfo.GetCultures(CultureTypes.NeutralCultures).FirstOrDefault(c => c.ThreeLetterISOLanguageName == isoLanguage) ?? CultureInfo.DefaultThreadCurrentUICulture;

      string url = Controller.Settings.TranslationSettings.TranslationServiceProvider switch
      {
        TranslationServiceProvider.Google => $"https://translate.google.com/#auto|{culture.TwoLetterISOLanguageName}|{searchText.Trim()}",
        TranslationServiceProvider.DeepL => $"https://www.deepl.com/translator#auto/{culture.TwoLetterISOLanguageName}/{searchText.Trim()}",
        _ => throw new NotImplementedException($"ServiceProvider {Controller.Settings.TranslationSettings.TranslationServiceProvider} is not implemented yet!")
      };

      var psi = new System.Diagnostics.ProcessStartInfo
      {
        FileName = url,
        UseShellExecute = true
      };
      System.Diagnostics.Process.Start(psi);
    }



    #endregion

    // https://stackoverflow.com/questions/2981884/how-can-i-get-a-textpointer-from-a-mouse-click-in-a-flowdocument
  }
}
