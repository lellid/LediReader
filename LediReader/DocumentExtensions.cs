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
            var scale = VisualTreeHelper.GetDpi(viewer);
            var viewerPositionScaled = new Point(viewerPosition.X / scale.DpiScaleX, viewerPosition.Y / scale.DpiScaleY);
            var screenPoint = viewer.PointToScreen(viewerPositionScaled);
            return GetTextElementAtScreenPosition((FlowDocument)viewer.Document, screenPoint);
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
                var pointer = document.ContentStart.GetPositionAtOffset(charsBeforePoint);

                // Adjust for difference between "text offset" and actual number of characters before pointer
                for (int i = 0; i < 10; i++)  // Limit to 10 adjustments
                {
                    int error = charsBeforePoint - new TextRange(document.ContentStart, pointer).Text.Length;
                    if (error == 0) break;
                    pointer = pointer.GetPositionAtOffset(error);
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