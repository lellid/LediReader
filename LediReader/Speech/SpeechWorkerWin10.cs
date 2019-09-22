using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace LediReader.Speech
{
  public class SpeechWorkerWin10 : SpeechWorkerBase
  {
    Windows.Media.Playback.MediaPlayer _mediaPlayer;
    Windows.Media.SpeechSynthesis.SpeechSynthesizer _synthesizer;

    MediaSource _nextSource, _lastSource;
    SpeechSynthesisStream _nextStream, _lastStream;
    MediaPlaybackItem _nextPBItem, _lastPBItem;

    public override bool IsSpeechSynthesizingActive => throw new NotImplementedException();

    private void AttachSynthesizer()
    {
      _mediaPlayer = new Windows.Media.Playback.MediaPlayer();
      _mediaPlayer.MediaEnded += EhSpeakCompleted;
      _synthesizer = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
      _synthesizer.Options.IncludeWordBoundaryMetadata = true; // to trigger Cues on word boundaries
      _synthesizer.Options.AudioVolume = _speechVolume;
      _synthesizer.Options.SpeakingRate = _speechRate;

      var voice = SpeechSynthesizer.AllVoices.Where(x => x.DisplayName == _speechVoice).FirstOrDefault();

      if (null != voice)
        _synthesizer.Voice = voice;
    }

    private void DetachSynthesizer()
    {
      _mediaPlayer.MediaEnded -= EhSpeakCompleted;
      _mediaPlayer.Dispose();
      _mediaPlayer = null;
      _synthesizer.Dispose();
      _synthesizer = null;

      _nextSource?.Dispose();
      _nextStream?.Dispose();
    }

    public override void StartSpeech(TextElement te)
    {
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
      return _lastSpokenElement;
    }

    async void InternalStartSpeech(TextElement te)
    {
      var stb = ExtractText(te);
      _flowDocument = GetParentDocument(te);
      if (null != _flowDocument)
      {
        _flowDocument.Background = _documentBackBrushInPlay;
      }
      _nextStream = await _synthesizer.SynthesizeTextToStreamAsync(stb.ToString());
      _nextSource = MediaSource.CreateFromStream(_nextStream, _nextStream.ContentType);
      _nextPBItem = new MediaPlaybackItem(_nextSource);
      RegisterForMarkEvents(_nextPBItem);
      _mediaPlayer.Source = _lastPBItem;
      _mediaPlayer.Play();

    }

    private async void EhSpeakCompleted(MediaPlayer mediaPlayer, object args)
    {
      _nextSource?.Dispose();
      _nextStream?.Dispose();


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
        timedTrack.CueEntered += EhSpeakProgress;
        mediaPlaybackItem.TimedMetadataTracks.SetPresentationMode((uint)index, TimedMetadataTrackPresentationMode.ApplicationPresented);
      }
    }

    /// <summary>
    /// This function executes when a SpeechCue is hit and calls the functions to update the UI
    /// </summary>
    /// <param name="timedMetadataTrack">The timedMetadataTrack associated with the event.</param>
    /// <param name="args">the arguments associated with the event.</param>
    private async void EhSpeakProgress(TimedMetadataTrack timedMetadataTrack, MediaCueEventArgs args)
    {
      // Check in case there are different tracks and the handler was used for more tracks 
      if (timedMetadataTrack.TimedMetadataKind == TimedMetadataKind.Speech)
      {
        var cue = args.Cue as SpeechCue;
        if (cue != null)
        {
          System.Diagnostics.Debug.WriteLine("Cue text:[" + cue.Text + "]");

          var (textPos, textEle) = FindMarker(cue.StartPositionInInput.Value);

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
      }
    }
  }
}
