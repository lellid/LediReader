// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace LediReader.Speech
{
  /// <summary>
  /// Implementation of the <see cref="SpeechWorkerBase"/> using classes from the Windows 10 SDK.
  /// </summary>
  /// <seealso cref="LediReader.Speech.SpeechWorkerBase" />
  public class SpeechWorker_Windows10 : SpeechWorkerBase
  {
    /// <summary>The media player (used to play the speech waveform and to issue callbacks when certain marks are reached).</summary>
    MediaPlayer _mediaPlayer;

    /// <summary>
    /// The speech synthesizer. Here, it is only responsible for generating waveforms (but not for playing them).
    /// </summary>
    SpeechSynthesizer _synthesizer;

    /// <summary>
    /// The synthesized audio stream.
    /// </summary>
    SpeechSynthesisStream _nextStream;

    /// <summary>
    /// The media source that wrap the audio stream.
    /// </summary>
    MediaSource _nextSource;

    /// <summary>
    /// The media playback item that encapsulated the media source.
    /// </summary>
    MediaPlaybackItem _nextPBItem;

    /// <summary>
    /// True if speech generation is currently active.
    /// </summary>
    bool _isSpeechSynthesizingActive;

    /// <summary>
    /// Gets a value indicating whether speech synthesis is currently active.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is speech synthesizing active; otherwise, <c>false</c>.
    /// </value>
    public override bool IsSpeechSynthesizingActive => _isSpeechSynthesizingActive;

    /// <summary>
    /// Gets/sets the dispatcher to dispatch actions to the current UI thread.
    /// </summary>
    /// <value>
    /// The dispatcher.
    /// </value>
    public Dispatcher Dispatcher { get; internal set; }

    /// <summary>
    /// Returns true if this class can be used on the current computer.
    /// </summary>
    /// <returns>True if this class can be used on the current computer.</returns>
    public static bool IsSupportedByThisComputer()
    {
      return
        Windows10SDKApiHelper.IsTypeSupported("Windows.Media.SpeechSynthesis.SpeechSynthesizer") &&
        Windows10SDKApiHelper.IsTypeSupported("Windows.Media.Playback.MediaPlayer");
    }

    /// <summary>
    /// Creates synthesizer and media player.
    /// </summary>
    private void AttachSynthesizer()
    {
      _mediaPlayer = new Windows.Media.Playback.MediaPlayer();
      _mediaPlayer.MediaEnded += EhSpeakCompleted;
      _synthesizer = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
      _synthesizer.Options.IncludeWordBoundaryMetadata = true; // to trigger Cues on word boundaries

      // Convert a value of -10..0..10 to a double value between 0.5..1..6
      double speechRate = 1;
      if (_speechRate < 0)
        speechRate = Math.Pow(2, _speechRate / 10.0);
      else if (_speechRate > 0)
        speechRate = Math.Pow(6, _speechRate / 10);

      _synthesizer.Options.SpeakingRate = speechRate;

      // Convert the speech volume value from 0..100 to a value from 0..1
      _synthesizer.Options.AudioVolume = _speechVolume / 100.0;

      var voice = SpeechSynthesizer.AllVoices.Where(x => x.DisplayName == _speechVoice).FirstOrDefault();

      if (null != voice)
        _synthesizer.Voice = voice;
    }

    /// <summary>
    /// Disposes synthesizer and media player.
    /// </summary>
    private void DetachSynthesizer()
    {
      if (null != _mediaPlayer)
        _mediaPlayer.MediaEnded -= EhSpeakCompleted;
      _mediaPlayer?.Dispose();
      _mediaPlayer = null;
      _synthesizer?.Dispose();
      _synthesizer = null;

      _nextSource?.Dispose();
      _nextStream?.Dispose();
      _isSpeechSynthesizingActive = false;
    }

    /// <summary>
    /// Gets the installed voices.
    /// </summary>
    /// <returns>Enumeration of all installed voices.</returns>
    public override IEnumerable<IInstalledVoiceInfo> GetInstalledVoices()
    {
      return SpeechSynthesizer.AllVoices.Select(v => new VoiceInfoWrapper_Windows10(v, true));
    }

    /// <summary>
    /// Gets the current voice.
    /// </summary>
    /// <returns>The current voice used by the synthesizer.</returns>
    public override IInstalledVoiceInfo GetCurrentVoice()
    {
      if (_synthesizer != null)
      {
        return new VoiceInfoWrapper_Windows10(_synthesizer.Voice, true);
      }
      else
      {
        AttachSynthesizer();
        var result = new VoiceInfoWrapper_Windows10(_synthesizer.Voice, true);
        DetachSynthesizer();
        return result;
      }
    }

    public override void StartSpeech(TextElement te)
    {
      _isSpeechSynthesizingActive = true;
      if (null == _synthesizer)
      {
        AttachSynthesizer();
      }

      LockDisplay();

      InternalStartSpeech(te);
    }

    public override TextElement StopSpeech()
    {
      _mediaPlayer?.Pause();
      DetachSynthesizer();
      EhSpeakCompleted_GuiContext();
      return _lastSpokenElement;
    }

    public override void PauseSpeech()
    {
      _mediaPlayer?.Pause();
    }

    async void InternalStartSpeech(TextElement te)
    {
      if (_mediaPlayer is null || _synthesizer is null)
      {
        return;
      }

      var stb = ExtractText(te);
      _flowDocument = GetParentDocument(te);
      if (null != _flowDocument)
      {
        _flowDocument.Background = _documentBackBrushInPlay;
      }
      _nextStream = await _synthesizer.SynthesizeTextToStreamAsync(stb.ToString());

      if (_mediaPlayer is null || _synthesizer is null) // during await the mediaPlayer could be stopped
      {
        _nextStream?.Dispose();
        return;
      }

      _nextSource = MediaSource.CreateFromStream(_nextStream, _nextStream.ContentType);
      _nextPBItem = new MediaPlaybackItem(_nextSource);
      RegisterForMarkEvents(_nextPBItem);
      _mediaPlayer.Source = _nextPBItem;
      _mediaPlayer.Play();

    }

    /// <summary>
    /// Called when the media player has completed. Please note that this event is not triggered if StopSpeech is called.
    /// Since this call is not in a UI context, it simply calles <see cref="EhSpeakCompleted_GuiContext"/> in UI context.
    /// </summary>
    /// <param name="mediaPlayer">The media player.</param>
    /// <param name="args">The arguments.</param>
    private async void EhSpeakCompleted(MediaPlayer mediaPlayer, object args)
    {
      _nextSource?.Dispose();
      _nextStream?.Dispose();

      Dispatcher.BeginInvoke(
        new Action(EhSpeakCompleted_GuiContext),
        System.Windows.Threading.DispatcherPriority.Send
        );
    }

    /// <summary>
    /// Is called (in UI context) when the media player has completed playing the current speech.
    /// </summary>
    private void EhSpeakCompleted_GuiContext()
    {
      // System.Diagnostics.Debug.WriteLine($"EhSpeechCompleted");
      if (null != _lastMarkedTextElement)
      {
        _lastMarkedTextElement.Background = _lastMarkedTextElementOriginalBackground; // we still assume that we in play until it is proofed otherwise
        _lastMarkedTextElement = null;
        _lastMarkedTextElementOriginalBackground = null;
      }

      if (null != _mediaPlayer && null != _nextElementToSpeak)
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
        OnSpeechCompleted(_lastSpokenElement);
      }
    }


    /// <summary>
    /// Register for all mark events
    /// </summary>
    /// <param name="mediaPlaybackItem">The Media PLayback Item add handlers to.</param>
    private void RegisterForMarkEvents(MediaPlaybackItem mediaPlaybackItem)
    {
      //tracks could all be generated at creation
      for (int index = 0; index < mediaPlaybackItem.TimedMetadataTracks.Count; index++)
      {
        RegisterMetadataHandlerForMarks(mediaPlaybackItem, index);
      }

      // if the tracks are added later we will  
      // monitor the tracks being added and subscribe to the ones of interest 
      mediaPlaybackItem.TimedMetadataTracksChanged += (MediaPlaybackItem sender, IVectorChangedEventArgs args) =>
      {
        if (args.CollectionChange == CollectionChange.ItemInserted)
        {
          RegisterMetadataHandlerForMarks(sender, (int)args.Index);
        }
        else if (args.CollectionChange == CollectionChange.Reset)
        {
          for (int index = 0; index < sender.TimedMetadataTracks.Count; index++)
          {
            RegisterMetadataHandlerForMarks(sender, index);
          }
        }
      };
    }

    /// <summary>
    /// Register for just word bookmark events.
    /// </summary>
    /// <param name="mediaPlaybackItem">The Media Playback Item to register handlers for.</param>
    /// <param name="index">Index of the timedMetadataTrack within the mediaPlaybackItem.</param>
    private void RegisterMetadataHandlerForMarks(MediaPlaybackItem mediaPlaybackItem, int index)
    {
      //make sure we only register for bookmarks
      var timedTrack = mediaPlaybackItem.TimedMetadataTracks[index];
      if (timedTrack.Id == "SpeechWord")
      {
        timedTrack.CueEntered += EhCueReceived;
        mediaPlaybackItem.TimedMetadataTracks.SetPresentationMode((uint)index, TimedMetadataTrackPresentationMode.ApplicationPresented);
      }
    }

    /// <summary>
    /// This function executes when a SpeechCue is hit and calls then <see cref="EhSpeechProgress(int)"/> in UI context.
    /// </summary>
    /// <param name="timedMetadataTrack">The timedMetadataTrack associated with the event.</param>
    /// <param name="args">the arguments associated with the event.</param>
    private async void EhCueReceived(TimedMetadataTrack timedMetadataTrack, MediaCueEventArgs args)
    {
      // Check in case there are different tracks and the handler was used for more tracks 
      if (timedMetadataTrack.TimedMetadataKind == TimedMetadataKind.Speech)
      {
        var cue = args.Cue as SpeechCue;
        if (cue != null)
        {
          // System.Diagnostics.Debug.WriteLine($"Cue text:[{cue.Text}], Access={Dispatcher.CheckAccess()}");
          Dispatcher.BeginInvoke(
            new Action<int>(EhSpeechProgress),
            DispatcherPriority.Background,
            cue.StartPositionInInput
            );
        }
      }
    }

    /// <summary>
    /// Is called (in UI context) every time a word boundary is reached.
    /// </summary>
    /// <param name="characterPosition">The character position in the string that is read-aloud.</param>
    void EhSpeechProgress(int characterPosition)
    {
      var timeBeg = DateTime.UtcNow;

      var (textPos, textEle) = FindMarker(characterPosition);

      // System.Diagnostics.Debug.WriteLine($"SpeechProgress[{textPos}]");

      var newTextElementToMark = GetTextElementToMark(textEle);

      if (!object.ReferenceEquals(_lastMarkedTextElement, newTextElementToMark))
      {

        if (null != _lastMarkedTextElement)
        {
          _lastMarkedTextElement.Background = _lastMarkedTextElementOriginalBackground;
          _lastMarkedTextElementOriginalBackground = null;
        }

        _lastMarkedTextElement = newTextElementToMark;

        if (null != _lastMarkedTextElement)
        {
          _lastMarkedTextElementOriginalBackground = _lastMarkedTextElement.Background;
          _lastMarkedTextElement.Background = _spanBackBrushInPlay;
        }
      }

      if (null != textEle)
      {
        _lastSpokenElement = textEle;
        textEle.BringIntoView();
      }
    }
  }

  /// <summary>
  /// Wraps a <see cref="Windows.Media.SpeechSynthesis.VoiceInformation"/> to expose <see cref="IInstalledVoiceInfo"/>.
  /// </summary>
  /// <seealso cref="LediReader.Speech.IInstalledVoiceInfo" />
  public class VoiceInfoWrapper_Windows10 : IInstalledVoiceInfo
  {
    Windows.Media.SpeechSynthesis.VoiceInformation _voiceInfo;
    bool _isEnabled;

    public VoiceInfoWrapper_Windows10(Windows.Media.SpeechSynthesis.VoiceInformation voiceInfo, bool isEnabled)
    {
      _voiceInfo = voiceInfo;
      _isEnabled = isEnabled;
    }

    public string Name => _voiceInfo.DisplayName;

    public string Culture => _voiceInfo.Language;

    public string Age => "n.a.";

    public string Gender => _voiceInfo.Gender.ToString();

    public string Description => _voiceInfo.Description;

    public bool IsEnabled => _isEnabled;
  }
}
