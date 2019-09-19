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

    string _speechCulture = "en-US";
    string _speechVoice;

    bool _isEmphasisEnabled;

    uint _workingBackgroundBlackTheme;
    uint _workingBackgroundLightTheme;

    bool _isInDarkMode;

    bool _keepDisplayOnDuringSpeech;

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
        var synthesizer = new SpeechSynthesizer();
        synthesizer.SpeakProgress += EhSpeakProgress;
        synthesizer.SpeakCompleted += EhSpeakCompleted;
        synthesizer.SetOutputToDefaultAudioDevice();

        synthesizer.Rate = _speechRate;
        synthesizer.Volume = _speechVolume;
        if (!string.IsNullOrEmpty(_speechVoice))
        {
          synthesizer.SelectVoice(_speechVoice);
        }

        var oldSynthesizer = System.Threading.Interlocked.Exchange(ref _synthesizer, synthesizer);

        if (null != oldSynthesizer)
        {
          oldSynthesizer.SpeakCompleted -= EhSpeakCompleted;
          oldSynthesizer.SpeakProgress -= EhSpeakProgress;
          oldSynthesizer.Dispose();
          oldSynthesizer = null;
        }
      }
    }

    public void DetachSynthesizer()
    {
      var synthesizer = System.Threading.Interlocked.Exchange(ref _synthesizer, null);
      if (null != synthesizer)
      {
        synthesizer.SpeakCompleted -= EhSpeakCompleted;
        synthesizer.SpeakProgress -= EhSpeakProgress;
        synthesizer.Dispose();

      }
    }



    public void ApplySettings(SpeechSettings s)
    {
      _speechCulture = s.SpeechCulture ?? "en-US";
      _speechVoice = s.SpeechVoice;
      _speechRate = s.SpeechRate;
      _speechVolume = s.SpeechVolume;
      _keepDisplayOnDuringSpeech = s.KeepDisplayOnDuringSpeech;
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
      _workingBackgroundBlackTheme = s.WorkingBackgroundColorDarkMode;
      _workingBackgroundLightTheme = s.WorkingBackgroundColorLightMode;

      _isInDarkMode = !_isInDarkMode; // trick here
      IsInDarkMode = !_isInDarkMode; // to force calculation of colors
    }

    public void GetSettings(SpeechSettings s)
    {
      s.SpeechCulture = _speechCulture;
      s.SpeechVoice = _speechVoice;
      s.SpeechRate = _speechRate;
      s.SpeechVolume = _speechVolume;
      s.KeepDisplayOnDuringSpeech = _keepDisplayOnDuringSpeech;
      s.IsEmphasisEnabled = _isEmphasisEnabled;
    }

    public bool IsInDarkMode
    {
      get
      {
        return _isInDarkMode;
      }
      set
      {
        if (!(_isInDarkMode == value))
        {
          _isInDarkMode = value;

          if (_isInDarkMode)
          {
            _documentBackBrushNormal = System.Windows.Media.Brushes.Black;
            _spanBackBrushInPlay = System.Windows.Media.Brushes.Black;
            var (r, g, b, _) = Gui.ColorConverter.ToRGBA(_workingBackgroundBlackTheme);
            _documentBackBrushInPlay = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
          }
          else
          {
            _documentBackBrushNormal = System.Windows.Media.Brushes.White;
            _spanBackBrushInPlay = System.Windows.Media.Brushes.White;
            var (r, g, b, _) = Gui.ColorConverter.ToRGBA(_workingBackgroundLightTheme);
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

    public VoiceInfo GetSpeechVoiceInfo()
    {
      AttachSynthesizer();
      return _synthesizer.Voice;
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

    IDisposable _displayRequest;
    void LockDisplay()
    {
      if (_keepDisplayOnDuringSpeech)
      {
        if (Environment.OSVersion.Version.Major > 6 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 2))
        {
          if (null == _displayRequest)
          {
            var oldRequest = System.Threading.Interlocked.Exchange(ref _displayRequest, new DisplayRequestLevel1());
            oldRequest?.Dispose();
          }
        }
      }
    }

    void ReleaseDisplay()
    {
      var oldRequest = System.Threading.Interlocked.Exchange(ref _displayRequest, null);
      oldRequest?.Dispose();
    }

    public void StartSpeech(TextElement te)
    {
      if (null == _synthesizer)
      {
        AttachSynthesizer();
      }

      LockDisplay();

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
        }
        DetachSynthesizer();
        ReleaseDisplay();
        SpeechCompleted?.Invoke(_lastSpokenElement);
      }
    }

    public PromptBuilder ExtractText(TextElement te)
    {
      ClearMarkers();
      _nextElementToSpeak = null;
      var stb = new StringBuilder();
      var single = new StringBuilder();

      int textPosition = 0;
      foreach (var c in HtmlToFlowDocument.Rendering.WpfHelper.GetTextElementsBeginningWith(te))
      {
        switch (c)
        {
          case Run run:
            single.Clear();
            single.Append(run.Text);
            single = single.Replace("\u2012", ", ");  // Figure dash
            single = single.Replace("\u2013", ", ");  // EN dash
            single = single.Replace("\u2014", ", ");  // EM dash
            single = single.Replace("\u2E3A", ", ");  // Two-EM dash
            single = single.Replace("\u2E3B", ", ");  // Three-EM dash
            stb.Append(single);
            AddMarker(textPosition, c);
            textPosition += single.Length;
            break;
        }

        if (textPosition > 0 && (c is Paragraph || c is Section)) // break if we have text and have reached the next paragraph
        {
          _nextElementToSpeak = c;
          break;
        }
      }

      // Some replacements - but make sure to replace the same number of chars; otherwise we have to adjust the positions
      stb = stb.Replace(" - ", " , "); // minus sign with spaces to the left and right should take time like a comma

      var pbd = new PromptBuilder();
      pbd.StartVoice(new CultureInfo(_speechCulture));
      pbd.AppendText(stb.ToString());
      pbd.EndVoice();

      _textOffsetInPrompt = -1;

      return pbd;
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
