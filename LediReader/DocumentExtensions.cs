using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
using System.Windows.Documents;

namespace LediReader
{
    public static class DocumentExtensions
    {
        // Point is specified relative to the given visual
        public static TextPointer ScreenPointToTextPointer(this FlowDocument document, Point screenPoint)
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

    }
}