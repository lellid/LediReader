// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using LediReader.Speech;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace LediReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowController _controller;
        SpeechWorker _speech;
        public static StartupEventArgs StartupArguments { get; set; }

        /// <summary>
        /// The last text element that is somehow marked by the user. If the user turns a page, this element is set to null, in order to ensure that 
        /// this element is always on a page visible to the user.
        /// </summary>
        TextElement _lastTextElementConsidered;

        /// <summary>
        /// True if speech is stopped, and the user has not yet scrolled to another page.
        /// </summary>
        bool _isInState_WaitForResumingSpeech;

        /// <summary>
        /// True if the dictionary page is currently shown on top.
        /// </summary>
        bool _isInState_ShowingTheDictionary;

        #region Startup and Closing

        public MainWindow()
        {
            _controller = new MainWindowController();
            try
            {
                _controller.LoadSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load settings\r\nThe message is:\r\n{ex.Message}");
            }

            var ss = _controller.Settings.StartupSettings;

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
            this.DataContext = _controller;
            Loaded += EhLoaded;
        }

        private void EhLoaded(object sender, RoutedEventArgs e)
        {
            _speech = new SpeechWorker();
            _speech.ApplySettings(_controller.Settings.SpeechSettings);
            _speech.SpeechCompleted += EhSpeechCompleted;

            Microsoft.Win32.SystemEvents.PowerModeChanged += EhPowerModeChanged;

            _guiDictionary.Controller.LoadDictionariesUsingSettings(_controller.Settings.DictionarySettings);

            HtmlToFlowDocument.Dom.FlowDocument document = null;
            if (null != Gui.StartupSettings.StartupArguments && Gui.StartupSettings.StartupArguments.Length > 0)
            {
                document = _controller.OpenEbook(Gui.StartupSettings.StartupArguments[0]);
            }
            else
            {
                document = _controller.ReopenEbook();
            }


            _guiViewer.Zoom = _controller.Settings.BookSettings.Zoom;
            ShowFlowDocument(document);
            SlobViewer.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);

            bool navigated = false;
            if (_controller.Settings.BookSettings.Bookmark != null)
            {
                var flowDocument = (FlowDocument)_guiViewer.Document;
                var textElement = HtmlToFlowDocument.Rendering.WpfHelper.GetTextElementFromBookmark(flowDocument, _controller.Settings.BookSettings.Bookmark);
                if (null != textElement)
                {
                    textElement.BringIntoView();
                    navigated = true;
                }
            }

            if (!navigated)
            {
                if (_guiViewer.CanGoToPage(_controller.Settings.BookSettings.PageNumber))
                {
                    _guiViewer.GoToPage(_controller.Settings.BookSettings.PageNumber);
                }
            }
        }



        protected override void OnClosing(CancelEventArgs e)
        {
            // Update file paths
            _controller.UpdateDictionaryFileNames(_guiDictionary.Controller.Dictionaries.Select(x => x.FileName));

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
                _controller.Settings.BookSettings.Bookmark = bookmark;
            }


            // save the current page
            _controller.Settings.BookSettings.PageNumber = _guiViewer.MasterPageNumber;
            _controller.Settings.BookSettings.Zoom = _guiViewer.Zoom;
            _speech.GetSettings(_controller.Settings.SpeechSettings);

            // save windows state
            {
                var ss = _controller.Settings.StartupSettings;
                ss.Left = this.Left;
                ss.Top = this.Top;
                ss.Width = this.ActualWidth;
                ss.Height = this.ActualHeight;
                ss.IsMaximized = this.WindowState == WindowState.Maximized;
                ss.IsFullScreen = false;
            }

            _controller.SaveSettings();
            base.OnClosing(e);
        }

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
                    _speech.DetachSynthesizer();
                    break;
                default:
                    break;
            }
        }

        #endregion Startup and Closing


        private void ShowFlowDocument(HtmlToFlowDocument.Dom.FlowDocument flowDocument)
        {
            if (null != flowDocument)
            {
                var renderer = new HtmlToFlowDocument.Rendering.WpfRenderer() { InvertColors = false, AttachDomAsTags = true };
                var flowDocumentE = renderer.Render(flowDocument);

                flowDocumentE.IsColumnWidthFlexible = false;

                var binding = new Binding("ActualWidth") { Source = _guiViewer };
                flowDocumentE.SetBinding(FlowDocument.ColumnWidthProperty, binding); // Make sure the ColumnWidth property is same as the actual width of the flow document
                _guiViewer.Document = flowDocumentE;
            }
            else
            {
                _guiViewer.Document = null;
            }
        }













        #region Gui event handlers

        private void EhOpenBook(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Epub files|*.epub" +
                            "|All Files|*.*",
                Multiselect = false,
                Title = "Open Ebook (Epub format)"
            };

            if (true == dlg.ShowDialog(this))
            {
                var flowDocument = _controller.OpenEbook(dlg.FileName);
                ShowFlowDocument(flowDocument);
            }
        }

        private void EhSettings(object sender, RoutedEventArgs e)
        {
            var controller = new Gui.SpeechSettingsController() { Synthesizer = _speech };
            var control = new Gui.SpeechSettingsControl(controller);


            var window = new Window();
            window.Content = control;
            window.Show();
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
            SlobViewer.GuiActions.ImportTeiFile(_guiDictionary.Controller, this);
            SlobViewer.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);
        }

        private void EhImportTUChemnitzFile(object sender, RoutedEventArgs e)
        {
            SlobViewer.GuiActions.ImportTUChemnitzFile(_guiDictionary.Controller, this);
            SlobViewer.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);
        }

        private void EhOpenSlobFile(object sender, RoutedEventArgs e)
        {
            SlobViewer.GuiActions.OpenSlobFile(_guiDictionary.Controller, this);
            SlobViewer.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);
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
            var control = new Gui.GoToPageControl();
            control.Controller.MaxPageNumber = _guiViewer.PageCount;
            control.Controller.PageNumber = _guiViewer.MasterPageNumber;

            var dlg = new Gui.DialogShellViewWpf(control);

            dlg.ButtonApplyPressed += () => _guiViewer.GoToPage(control.Controller.PageNumber);

            if (dlg.ShowDialog() == true)
            {
                _guiViewer.GoToPage(control.Controller.PageNumber);
                _lastTextElementConsidered = null;
                _isInState_WaitForResumingSpeech = false;
            }
        }

        private void Action_GotoNextPage()
        {
            _speech.StopSpeech();
            _guiViewer.NextPage();
            _lastTextElementConsidered = null;
            _isInState_WaitForResumingSpeech = false;
        }

        private void Action_GotoPreviousPage()
        {
            _speech.StopSpeech();
            _guiViewer.PreviousPage();
            _lastTextElementConsidered = null;
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
                _isInState_WaitForResumingSpeech = false;
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
                    var pt = e.GetPosition(_guiViewer);
                    var te = _guiViewer.GetTextElementAtViewerPosition(pt);
                    if (te is Run run)
                    {
                        _speech.StopSpeech();
                        Action_ShowDictionary(run.Text);
                        e.Handled = true;
                    }
                }
            }
        }





        #endregion



        // https://stackoverflow.com/questions/2981884/how-can-i-get-a-textpointer-from-a-mouse-click-in-a-flowdocument
    }
}
