// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace LediReader
{
  public static class DocumentExtensions
  {
    public static TextElement GetTextElementAtViewerPosition(this FlowDocumentPageViewer viewer, Point viewerPosition)
    {
      var mainWindow = App.Current.MainWindow;
      var mainWindowPosition = viewer.TranslatePoint(viewerPosition, mainWindow); // to main windows coordinates
      return GetTextElementAtMainWindowPosition(mainWindow, (FlowDocument)viewer.Document, mainWindowPosition);
    }

    /// <summary>
    /// Gets the text element at the main window position.
    /// </summary>
    /// <param name="window">The main window.</param>
    /// <param name="flowDocument">The flow document.</param>
    /// <param name="windowPosition">The position into the main window, e.g. get by mouse events like <c>e.GetPosition(mainWindow)</c>.</param>
    /// <returns>The text element under this position</returns>
    public static TextElement GetTextElementAtMainWindowPosition(this Window window, FlowDocument flowDocument, Point windowPosition)
    {
      var scale = VisualTreeHelper.GetDpi(window);
      var windowPositionScaled = new Point(windowPosition.X / scale.DpiScaleX, windowPosition.Y / scale.DpiScaleY);
      var screenPoint = window.PointToScreen(windowPositionScaled);
      return GetTextElementAtScreenPosition(flowDocument, screenPoint);
    }

    public static TextElement GetTextElementAtScreenPosition(this FlowDocument flowDocument, Point screenPoint)
    {

      TextPointer pointer = DocumentExtensions.ScreenPointToTextPointer(flowDocument, screenPoint);
      if (null != pointer && pointer.Parent is TextElement te)
        return te;
      else if (null != pointer && pointer.Parent is FlowDocument doc)
        return doc.Blocks.FirstBlock;
      else
        return null;
    }


    // Point is specified relative to the given visual
    public static TextPointer ScreenPointToTextPointer(this FlowDocument document, Point screenPoint)
    {
      try
      {
        // Get text before point using automation
        var peer = new DocumentAutomationPeer(document);
        var textProvider = (ITextProvider)peer.GetPattern(PatternInterface.Text);
        var rangeProvider = textProvider.RangeFromPoint(screenPoint);
        rangeProvider.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Document, 1);
        int charsBeforePoint = rangeProvider.GetText(int.MaxValue).Length;

        // Find the pointer that corresponds to the TextPointer
        var pointer = document.ContentStart.GetPositionAtOffset(charsBeforePoint); // this is only a first guess

        // Adjust for difference between "text offset" and actual number of characters before pointer
        // Note that this is time consuming and can take some seconds (!) for big documents
        for (int i = 0; i < 12; i++)  // Limit to 12 adjustments
        {
          int error = charsBeforePoint - new TextRange(document.ContentStart, pointer).Text.Length;
          if (error == 0) break;
          pointer = pointer.GetPositionAtOffset(error); // try to get the pointer iteratively
        }
        return pointer;
      }
      catch (System.Exception ex)
      {
        return null;
      }
    }

  }
}
