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

        int _speechRate = 0;

        int _speechVolume = 100;

        string _speechVoice;

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

        }

        public void AttachSynthesizer()
        {
            if (null == _synthesizer)
            {
                _synthesizer = new SpeechSynthesizer();
                _synthesizer.SpeakProgress += EhSpeakProgress;
                _synthesizer.SpeakCompleted += EhSpeakCompleted;
                _synthesizer.SetOutputToDefaultAudioDevice();

                _synthesizer.Rate = _speechRate;
                _synthesizer.Volume = _speechVolume;
                if (!string.IsNullOrEmpty(_speechVoice))
                {
                    _synthesizer.SelectVoice(_speechVoice);
                }
            }
        }

        public void DetachSynthesizer()
        {
            if (null != _synthesizer)
            {
                _synthesizer.SpeakCompleted -= EhSpeakCompleted;
                _synthesizer.SpeakProgress -= EhSpeakProgress;
                _synthesizer.Dispose();
                _synthesizer = null;
            }
        }



        public void ApplySettings(SpeechSettings s)
        {
            _speechVoice = s.SpeechVoice;
            _speechRate = s.SpeechRate;
            _speechVolume = s.SpeechVolume;
            _isEmphasisEnabled = s.IsEmphasisEnabled;

            if (null != _synthesizer)
            {
                _synthesizer.Rate = _speechRate;
                _synthesizer.Volume = _speechVolume;
                if (!string.IsNullOrEmpty(_speechVoice))
                {
                    _synthesizer.SelectVoice(_speechVoice);
                }
            }

            _isEmphasisEnabled = s.IsEmphasisEnabled;
            _workingBackgroundBlackTheme = s.WorkingBackgroundBlackTheme;
            _workingBackgroundLightTheme = s.WorkingBackgroundLightTheme;

            _isDarkThemeActivated = !_isDarkThemeActivated; // trick here
            DarkTheme = !_isDarkThemeActivated; // to force calculation of colors
        }

        public void GetSettings(SpeechSettings s)
        {
            s.SpeechVoice = _speechVoice;
            s.SpeechRate = _speechRate;
            s.SpeechVolume = _speechVolume;
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

        public bool IsSpeechSynthesizingActive => null != _synthesizer && _synthesizer.State != SynthesizerState.Ready;

        static (byte r, byte g, byte b) RGBFromInt(int i)
        {
            return ((byte)((i & 0xFF0000) >> 16), (byte)((i & 0x00FF00) >> 8), (byte)((i & 0x0000FF)));
        }

        public int SpeechRate
        {
            get
            {
                return _speechRate;
            }
            set
            {
                if (!(_speechRate == value))
                {
                    _speechRate = value;
                    if (null != _synthesizer)
                    {
                        _synthesizer.Rate = _speechRate;
                    }
                }
            }
        }

        public int SpeechVolume
        {
            get
            {
                return _speechVolume;
            }
            set
            {
                if (!(_speechVolume == value))
                {
                    _speechVolume = value;
                    if (null != _synthesizer)
                    {
                        _synthesizer.Volume = _speechVolume;
                    }
                }
            }
        }

        public string SpeechVoice
        {
            get
            {
                return _speechVoice;
            }
            set
            {
                if (!(_speechVoice == value))
                {
                    _speechVoice = value;
                    if (null != _synthesizer)
                    {
                        _synthesizer.SelectVoice(_speechVoice);
                    }
                }
            }
        }

        public VoiceInfo SpeechVoiceInfo
        {
            get
            {
                AttachSynthesizer();
                return _synthesizer.Voice;
            }
        }

        public IEnumerable<InstalledVoice> GetInstalledVoices()
        {
            AttachSynthesizer();
            return _synthesizer.GetInstalledVoices();
        }


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
            if (null == _synthesizer)
            {
                AttachSynthesizer();
            }

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

        public TextElement StopSpeech()
        {
            _synthesizer?.SpeakAsyncCancelAll();
            return _lastSpokenElement;
        }

        TextElement GetTextElementToMark(TextElement te)
        {
            var result = te;
            while (result.Parent is Span span)
            {
                result = span;
            }
            return result.Parent is TextElement resultTe ? resultTe : result;
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

            _lastMarkedTextElement = GetTextElementToMark(textEle);

            if (null != _lastMarkedTextElement)
            {
                _lastMarkedTextElementOriginalBackground = _lastMarkedTextElement.Background;
                _lastMarkedTextElement.Background = _spanBackBrushInPlay;
            }

            if (null != textEle)
            {
                _lastSpokenElement = textEle;
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
