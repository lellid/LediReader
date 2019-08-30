// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Speech.Synthesis;
using System.Globalization;
using System.Windows.Documents;

namespace LediReader.Speech
{
    public class SpeechWorker
    {
        // TODO use System.Windows.Display and Microsoft.Windows.SDK.Contracts to keep the display on

        SpeechSynthesizer _synthesizer;
        TextElement _lastMarkedTextElement;
        System.Windows.Media.Brush _lastMarkedTextElementOriginalBackground;
        TextElement _nextElementToSpeak;
        int _textOffsetInPrompt;
        FlowDocument _flowDocument;
        TextElement _lastSpokenElement;
        bool _isEmphasisEnabled;

        int _workingBackgroundBlackTheme;
        int _workingBackgroundLightTheme;

        bool _isDarkThemeActivated;

        System.Windows.Media.Brush _documentBackBrushNormal = System.Windows.Media.Brushes.White;
        System.Windows.Media.Brush _documentBackBrushInPlay = System.Windows.Media.Brushes.LightGray;
        System.Windows.Media.Brush _spanBackBrushInPlay = System.Windows.Media.Brushes.White;

        /// <summary>
        /// Occurs when the speech is completed. The argument is the last text element that was spoken.
        /// </summary>
        public event Action<TextElement> SpeechCompleted;

        public SpeechWorker()
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SpeakProgress += EhSpeakProgress;
            _synthesizer.SpeakCompleted += EhSpeakCompleted;
            _synthesizer.SetOutputToDefaultAudioDevice();


        }

        public void ApplySettings(SpeechSettings s)
        {
            if (!string.IsNullOrEmpty(s.SpeechVoice))
            {
                _synthesizer.SelectVoice(s.SpeechVoice);
            }
            _synthesizer.Rate = s.SpeechRate;
            _synthesizer.Volume = s.SpeechVolume;
            _isEmphasisEnabled = s.IsEmphasisEnabled;
            _workingBackgroundBlackTheme = s.WorkingBackgroundBlackTheme;
            _workingBackgroundLightTheme = s.WorkingBackgroundLightTheme;

            _isDarkThemeActivated = !_isDarkThemeActivated; // trick here
            DarkTheme = !_isDarkThemeActivated; // to force calculation of colors
        }

        public void GetSettings(SpeechSettings s)
        {
            s.SpeechVoice = _synthesizer.Voice.Name;
            s.SpeechRate = _synthesizer.Rate;
            s.SpeechVolume = _synthesizer.Volume;
            s.IsEmphasisEnabled = _isEmphasisEnabled;
        }

        public bool DarkTheme
        {
            get
            {
                return _isDarkThemeActivated;
            }
            set
            {
                if (!(_isDarkThemeActivated == value))
                {
                    _isDarkThemeActivated = value;

                    if (_isDarkThemeActivated)
                    {
                        _documentBackBrushNormal = System.Windows.Media.Brushes.Black;
                        _spanBackBrushInPlay = System.Windows.Media.Brushes.Black;
                        var (r, g, b) = RGBFromInt(_workingBackgroundBlackTheme);
                        _documentBackBrushInPlay = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
                    }
                    else
                    {
                        _documentBackBrushNormal = System.Windows.Media.Brushes.White;
                        _spanBackBrushInPlay = System.Windows.Media.Brushes.White;
                        var (r, g, b) = RGBFromInt(_workingBackgroundLightTheme);
                        _documentBackBrushInPlay = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
                    }
                }
            }
        }

        public FlowDocument FlowDocument => _flowDocument;

        public TextElement LastSpokenElement => _lastSpokenElement;

        public bool IsSpeechSynthesizingActive => _synthesizer.State != SynthesizerState.Ready;

        static (byte r, byte g, byte b) RGBFromInt(int i)
        {
            return ((byte)((i & 0xFF0000) >> 16), (byte)((i & 0x00FF00) >> 8), (byte)((i & 0x0000FF)));
        }


        public SpeechSynthesizer Synthesizer => _synthesizer;


        private FlowDocument GetParentDocument(TextElement te)
        {
            while (te != null)
            {
                if (te.Parent is FlowDocument fd)
                    return fd;
                else
                    te = te.Parent as TextElement;
            }
            return null;
        }


        public void StartSpeech(TextElement te)
        {
            InternalStartSpeech(te);
        }

        private void InternalStartSpeech(TextElement te)
        {
            var pb = ExtractText(te);
            _flowDocument = GetParentDocument(te);
            if (null != _flowDocument)
            {
                _flowDocument.Background = _documentBackBrushInPlay;
            }
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
            {
                _lastMarkedTextElement.Background = _lastMarkedTextElementOriginalBackground;
                _lastMarkedTextElementOriginalBackground = null;
            }

            _lastMarkedTextElement = textEle.Parent is Span span ? span : textEle;

            if (null != _lastMarkedTextElement)
            {
                _lastMarkedTextElementOriginalBackground = _lastMarkedTextElement.Background;
                _lastMarkedTextElement.Background = _spanBackBrushInPlay;
            }

            if (null != textEle)
            {
                _lastSpokenElement = textEle;

                // var textPointer = textEle.ContentStart;
                // textPointer = textPointer.GetPositionAtOffset(e.CharacterPosition-textPos);
                textEle.BringIntoView();

            }
        }

        private void EhSpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            if (null != _lastMarkedTextElement)
            {
                _lastMarkedTextElement.Background = _lastMarkedTextElementOriginalBackground; // we still assume that we in play until it is proofed otherwise
                _lastMarkedTextElement = null;
                _lastMarkedTextElementOriginalBackground = null;
            }

            if (!e.Cancelled && null != _nextElementToSpeak)
            {
                InternalStartSpeech(_nextElementToSpeak);
            }
            else // we really end this speech
            {
                if (null != _flowDocument)
                {
                    _flowDocument.Background = _documentBackBrushNormal;
                    SpeechCompleted?.Invoke(_lastSpokenElement);
                }
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
