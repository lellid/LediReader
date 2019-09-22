using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace LediReader.Speech
{
  public abstract class SpeechWorkerBase
  {
    protected int _speechRate = 0;
    protected int _speechVolume = 100;
    protected string _speechCulture = "en-US";
    protected string _speechVoice;

    protected bool _isEmphasisEnabled;

    protected uint _workingBackgroundBlackTheme;
    protected uint _workingBackgroundLightTheme;

    protected bool _isInDarkMode;

    protected bool _keepDisplayOnDuringSpeech;

    protected System.Windows.Media.Brush _documentBackBrushNormal = System.Windows.Media.Brushes.White;
    protected System.Windows.Media.Brush _documentBackBrushInPlay = System.Windows.Media.Brushes.LightGray;
    protected System.Windows.Media.Brush _spanBackBrushInPlay = System.Windows.Media.Brushes.White;

    protected TextElement _lastMarkedTextElement;
    protected System.Windows.Media.Brush _lastMarkedTextElementOriginalBackground;
    protected TextElement _nextElementToSpeak;
    protected int _textOffsetInPrompt;
    protected FlowDocument _flowDocument;
    protected TextElement _lastSpokenElement;

    /// <summary>
    /// Occurs when the speech is completed. The argument is the last text element that was spoken.
    /// </summary>
    public event Action<TextElement> SpeechCompleted;

    public virtual void ApplySettings(SpeechSettings s)
    {
      _speechCulture = s.SpeechCulture ?? "en-US";
      _speechVoice = s.SpeechVoice;
      _speechRate = s.SpeechRate;
      _speechVolume = s.SpeechVolume;
      _keepDisplayOnDuringSpeech = s.KeepDisplayOnDuringSpeech;
      _isEmphasisEnabled = s.IsEmphasisEnabled;



      _isEmphasisEnabled = s.IsEmphasisEnabled;
      _workingBackgroundBlackTheme = s.WorkingBackgroundColorDarkMode;
      _workingBackgroundLightTheme = s.WorkingBackgroundColorLightMode;

      _isInDarkMode = !_isInDarkMode; // trick here
      IsInDarkMode = !_isInDarkMode; // to force calculation of colors
    }

    public virtual void GetSettings(SpeechSettings s)
    {
      s.SpeechCulture = _speechCulture;
      s.SpeechVoice = _speechVoice;
      s.SpeechRate = _speechRate;
      s.SpeechVolume = _speechVolume;
      s.KeepDisplayOnDuringSpeech = _keepDisplayOnDuringSpeech;
      s.IsEmphasisEnabled = _isEmphasisEnabled;
    }

    public FlowDocument FlowDocument => _flowDocument;

    public TextElement LastSpokenElement => _lastSpokenElement;


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
        }
      }
    }

    IDisposable _displayRequest;
    protected void LockDisplay()
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

    protected void ReleaseDisplay()
    {
      var oldRequest = System.Threading.Interlocked.Exchange(ref _displayRequest, null);
      oldRequest?.Dispose();
    }


    public abstract void StartSpeech(TextElement te);

    public abstract TextElement StopSpeech();

    public abstract bool IsSpeechSynthesizingActive { get; }

    protected void OnSpeechCompleted(TextElement te)
    {
      SpeechCompleted?.Invoke(te);
    }

    public StringBuilder ExtractText(TextElement te)
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
      _textOffsetInPrompt = -1;
      return stb;
    }

    protected FlowDocument GetParentDocument(TextElement te)
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

    protected TextElement GetTextElementToMark(TextElement te)
    {
      var result = te;
      while (result.Parent is Span span)
      {
        result = span;
      }
      return result.Parent is TextElement resultTe ? resultTe : result;
    }

    #region Markers
    List<int> _markerTextPositions = new List<int>();
    List<TextElement> _markerTextElements = new List<TextElement>();
    protected void AddMarker(int textPosition, TextElement te)
    {
      _markerTextPositions.Add(textPosition);
      _markerTextElements.Add(te);
    }
    protected void ClearMarkers()
    {
      _markerTextPositions.Clear();
      _markerTextElements.Clear();
    }
    protected (int TextPosition, TextElement TextElement) FindMarker(int textPosition)
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
