// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace LediReader.Speech
{
  /// <summary>
  /// Common base class for <see cref="SpeechWorker"/> using .NET frameworker, and <see cref="SpeechWorker_Windows10"/> using the Windows 10 SDK.
  /// </summary>
  public abstract class SpeechWorkerBase
  {
    /// <summary>Speech rate</summary>
    protected int _speechRate = 0;

    /// <summary>Speech volume</summary>
    protected int _speechVolume = 100;

    /// <summary>Speech culture string.</summary>
    protected string _speechCulture = "en-US";

    /// <summary>Name of the speech voice</summary>
    protected string _speechVoice;

    /// <summary>True if emphasised words should be emphasised in speech (currently not implemented).</summary>
    protected bool _isEmphasisEnabled;

    /// <summary>Background color (RGBA format) when speech is active, if a black theme is used.</summary>
    protected uint _workingBackgroundBlackTheme;

    /// <summary>Background color (RGBA format) when speech is active, if a light theme is used.</summary>
    protected uint _workingBackgroundLightTheme;

    /// <summary>True if a dark theme is used.</summary>
    protected bool _isInDarkMode;

    /// <summary>If true, the display is kept on during speech.</summary>
    protected bool _keepDisplayOnDuringSpeech;

    /// <summary>Background brush of the document, if speech is not active.</summary>
    protected System.Windows.Media.Brush _documentBackBrushNormal = System.Windows.Media.Brushes.White;

    /// <summary>Background brush of the document, if speech is active.</summary>
    protected System.Windows.Media.Brush _documentBackBrushInPlay = System.Windows.Media.Brushes.LightGray;

    /// <summary>Background brush of the span that is currently read aloud.</summary>
    protected System.Windows.Media.Brush _spanBackBrushInPlay = System.Windows.Media.Brushes.White;

    /// <summary>Text elements whose background was exchanged to mark it as currently spoken aloud.</summary>
    protected TextElement _lastMarkedTextElement;

    /// <summary>Original background of the last marked element.</summary>
    protected System.Windows.Media.Brush _lastMarkedTextElementOriginalBackground;

    /// <summary>Contains the next text element that should be read aloud, after the player finishs speaking the current elements.</summary>
    protected TextElement _nextElementToSpeak;

    /// <summary></summary>
    protected int _textOffsetInPrompt;

    /// <summary>The flow document, that is read aloud.</summary>
    protected FlowDocument _flowDocument;

    /// <summary>Gets the text element that was lastly spoken.</summary>
    protected TextElement _lastSpokenElement;

    /// <summary>
    /// Occurs when the speech is completed. The argument is the last text element that was spoken.
    /// </summary>
    public event Action<TextElement> SpeechCompleted;

    /// <summary>
    /// Gets the settings from the provided parameter <paramref name="s"/> and applies them to this speech worker.
    /// </summary>
    /// <param name="s">The provided settings.</param>
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

    /// <summary>
    /// Get the speech settings from this speech worker and uses them to set the settings in <paramref name="s"/>.
    /// </summary>
    /// <param name="s">On return, contains the current settings of this speech worker.</param>
    public virtual void GetSettings(SpeechSettings s)
    {
      s.SpeechCulture = _speechCulture;
      s.SpeechVoice = _speechVoice;
      s.SpeechRate = _speechRate;
      s.SpeechVolume = _speechVolume;
      s.KeepDisplayOnDuringSpeech = _keepDisplayOnDuringSpeech;
      s.IsEmphasisEnabled = _isEmphasisEnabled;
    }

    /// <summary>
    /// Gets the flow document, that is read aloud.
    /// </summary>
    /// <value>
    /// The flow document, that is read aloud.
    /// </value>
    public FlowDocument FlowDocument => _flowDocument;

    /// <summary>
    /// Gets the text element that was lastly spoken.
    /// </summary>
    /// <value>
    /// The last spoken text element.
    /// </value>
    public TextElement LastSpokenElement => _lastSpokenElement;


    /// <summary>
    /// Gets or sets a value indicating whether this instance is in dark mode.
    /// </summary>

    /// <summary>True if a dark theme is used.</summary>
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




    /// <summary>Speech rate</summary>
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

    /// <summary>Speech volume</summary>
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

    /// <summary>Name of the speech voice</summary>
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

    /// <summary>Helper variable. If set, this indicates that are display request is active. To release the request,
    /// this variable must be disposed.</summary>
    IDisposable _displayRequest;

    /// <summary>
    /// Locks the display to keep it active. Use <see cref="ReleaseDisplay"/> to release it again.
    /// </summary>
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

    /// <summary>
    /// Releases the display, to no longer keep it on.
    /// </summary>
    protected void ReleaseDisplay()
    {
      var oldRequest = System.Threading.Interlocked.Exchange(ref _displayRequest, null);
      oldRequest?.Dispose();
    }

    /// <summary>
    /// Gets all installed voices.
    /// </summary>
    /// <returns>All installed voices.</returns>
    public abstract IEnumerable<IInstalledVoiceInfo> GetInstalledVoices();

    /// <summary>
    /// Gets the current used voice.
    /// </summary>
    /// <returns></returns>
    public abstract IInstalledVoiceInfo GetCurrentVoice();

    /// <summary>
    /// Starts the speech at the provided text element.
    /// </summary>
    /// <param name="te">The text element to start the speech with.</param>
    public abstract void StartSpeech(TextElement te);

    /// <summary>
    /// Stops the speech immediately.
    /// </summary>
    /// <returns>The last spoken text element.</returns>
    public abstract TextElement StopSpeech();


    /// <summary>
    /// Pauses the speech immediately without doing anything else.
    /// </summary>
    public abstract void PauseSpeech();

    /// <summary>
    /// Gets a value indicating whether speech synthesizing is currently active.
    /// </summary>
    public abstract bool IsSpeechSynthesizingActive { get; }

    /// <summary>
    /// Called when speech is completed.
    /// </summary>
    /// <param name="te">The text element that was lastly spoken.</param>
    protected void OnSpeechCompleted(TextElement te)
    {
      SpeechCompleted?.Invoke(te);
    }

    /// <summary>
    /// Extracts the text, beginning with the provided text element in <paramref name="te"/> and ending with the last element of that block.
    /// </summary>
    /// <param name="te">The text element to begin with.</param>
    /// <returns>The text, beginning with the provided text element in <paramref name="te"/> and ending with the last element of that block.</returns>
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

    /// <summary>
    /// Gets the parent flow document of a text element.
    /// </summary>
    /// <param name="te">The text element.</param>
    /// <returns>The parent flow document of that text element.</returns>
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

    /// <summary>
    /// Given a text element (usually a Run), looks for the first TextBlock that contains that element, and returns the block.
    /// </summary>
    /// <param name="te">The text element.</param>
    /// <returns>The first TextBlock containing the provided text element.</returns>
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

    /// <summary>
    /// Adds the marker. A marker links a text position in the text to read aloud to the corresponding text element.
    /// </summary>
    /// <param name="textPosition">The text position.</param>
    /// <param name="te">The corresponding text element.</param>
    protected void AddMarker(int textPosition, TextElement te)
    {
      _markerTextPositions.Add(textPosition);
      _markerTextElements.Add(te);
    }
    /// <summary>
    /// Clears the markers.
    /// </summary>
    protected void ClearMarkers()
    {
      _markerTextPositions.Clear();
      _markerTextElements.Clear();
    }
    /// <summary>
    /// Given a text position in the text to read aloud, finds the marker, and returns both the original text position of the marker and the corresponding text element.
    /// </summary>
    /// <param name="textPosition">The text position.</param>
    /// <returns>Both the original text position of the marker and the corresponding text element. If nothing is found (null, null) is returned.</returns>
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
