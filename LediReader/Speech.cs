using System;
using System.Collections.Generic;
using System.Text;
using System.Speech.Synthesis;
using System.Globalization;
using System.Windows.Documents;

namespace LediReader
{
    public class Speech
    {
        SpeechSynthesizer _synthesizer;
        TextElement _lastMarkedTextElement;
        TextElement _nextElementToSpeak;
        int _textOffsetInPrompt;

        public Speech()
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SpeakProgress += EhSpeakProgress;
            _synthesizer.SpeakCompleted += EhSpeakCompleted;
            _synthesizer.SetOutputToDefaultAudioDevice();
        }



        public void StartSpeech(TextElement te)
        {
            var pb = ExtractText(te);
            _synthesizer.SpeakAsync(pb);
        }

        public void StopSpeech()
        {
            _synthesizer.SpeakAsyncCancelAll();
        }

        private void EhSpeakProgress(object sender, SpeakProgressEventArgs e)
        {
            if (_textOffsetInPrompt < 0)
                _textOffsetInPrompt = e.CharacterPosition;

            var (textPos, textEle) = FindMarker(e.CharacterPosition - _textOffsetInPrompt);

            if (null != _lastMarkedTextElement)
                _lastMarkedTextElement.Background = System.Windows.Media.Brushes.White;

            _lastMarkedTextElement = textEle.Parent is Span span ? span : textEle;

            if (null != _lastMarkedTextElement)
                _lastMarkedTextElement.Background = System.Windows.Media.Brushes.Yellow;

            if (null != textEle)
            {
                // var textPointer = textEle.ContentStart;
                // textPointer = textPointer.GetPositionAtOffset(e.CharacterPosition-textPos);
                textEle.BringIntoView();

            }
        }

        private void EhSpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            if (null != _lastMarkedTextElement)
            {
                _lastMarkedTextElement.Background = System.Windows.Media.Brushes.White;
                _lastMarkedTextElement = null;
            }

            if (!e.Cancelled && null != _nextElementToSpeak)
            {
                StartSpeech(_nextElementToSpeak);
            }

        }

        public PromptBuilder ExtractText(TextElement te)
        {
            ClearMarkers();
            _nextElementToSpeak = null;
            var pb = new PromptBuilder();
            pb.StartVoice(new CultureInfo("en-US"));

            int textPosition = 0;
            foreach (var c in HtmlToFlowDocument.Rendering.WpfHelper.GetTextElementsBeginningWith(te))
            {
                switch (c)
                {
                    case Run run:
                        pb.AppendText(run.Text);
                        AddMarker(textPosition, c);
                        textPosition += run.Text.Length;
                        break;
                }

                if (textPosition > 0 && (c is Paragraph || c is Section)) // break if we have text and have reached the next paragraph
                {
                    _nextElementToSpeak = c;
                    break;
                }
            }

            pb.EndVoice();

            _textOffsetInPrompt = -1;

            return pb;
        }



        #region Markers
        List<int> _markerTextPositions = new List<int>();
        List<TextElement> _markerTextElements = new List<TextElement>();
        void AddMarker(int textPosition, TextElement te)
        {
            _markerTextPositions.Add(textPosition);
            _markerTextElements.Add(te);
        }
        void ClearMarkers()
        {
            _markerTextPositions.Clear();
            _markerTextElements.Clear();
        }
        (int TextPosition, TextElement TextElement) FindMarker(int textPosition)
        {
            var idx = _markerTextPositions.BinarySearch(textPosition);

            if (idx >= 0)
            {
                return (_markerTextPositions[idx], _markerTextElements[idx]);
            }
            else
            {
                idx = ~idx - 1;
                idx = Math.Min(Math.Max(0, idx), _markerTextPositions.Count - 1);
                return (_markerTextPositions[idx], _markerTextElements[idx]);
            }

        }
        #endregion
    }
}
