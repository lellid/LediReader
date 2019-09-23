// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Speech.Synthesis;
using System.Globalization;
using System.Windows.Documents;
using System.Linq;

namespace LediReader.Speech
{
  public class SpeechWorker : SpeechWorkerBase
  {
    // TODO use System.Windows.Display and Microsoft.Windows.SDK.Contracts to keep the display on

    SpeechSynthesizer _synthesizer;

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


    public override void ApplySettings(SpeechSettings s)
    {
      base.ApplySettings(s);

      if (null != _synthesizer)
      {
        _synthesizer.Rate = _speechRate;
        _synthesizer.Volume = _speechVolume;
        if (!string.IsNullOrEmpty(_speechVoice))
        {
          _synthesizer.SelectVoice(_speechVoice);
        }
      }
    }

    public override bool IsSpeechSynthesizingActive => null != _synthesizer && _synthesizer.State != SynthesizerState.Ready;

    public override IInstalledVoiceInfo GetCurrentVoice()
    {
      AttachSynthesizer();
      return new VoiceInfoWrapper_NetFramework(_synthesizer.Voice, true);
    }

    public override IEnumerable<IInstalledVoiceInfo> GetInstalledVoices()
    {
      AttachSynthesizer();
      return _synthesizer.GetInstalledVoices().Select(installedVoice => new VoiceInfoWrapper_NetFramework(installedVoice.VoiceInfo, installedVoice.Enabled));
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

    private void InternalStartSpeech(TextElement te)
    {
      var pb = ExtractTextToPrompt(te);
      _flowDocument = GetParentDocument(te);
      if (null != _flowDocument)
      {
        _flowDocument.Background = _documentBackBrushInPlay;
      }
      _synthesizer.SpeakAsync(pb);
    }

    public override TextElement StopSpeech()
    {
      _synthesizer?.SpeakAsyncCancelAll();
      return _lastSpokenElement;
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
        OnSpeechCompleted(_lastSpokenElement);
      }
    }


    PromptBuilder ExtractTextToPrompt(TextElement te)
    {
      var stb = ExtractText(te);
      var pbd = new PromptBuilder();
      pbd.StartVoice(new CultureInfo(_speechCulture));
      pbd.AppendText(stb.ToString());
      pbd.EndVoice();
      return pbd;
    }

  }

  public class VoiceInfoWrapper_NetFramework : IInstalledVoiceInfo
  {
    VoiceInfo _voiceInfo;
    bool _isEnabled;

    public VoiceInfoWrapper_NetFramework(VoiceInfo voiceInfo, bool isEnabled)
    {
      _voiceInfo = voiceInfo;
      _isEnabled = isEnabled;
    }

    public string Name => _voiceInfo.Name;

    public string Culture => _voiceInfo.Culture.ToString();

    public string Age => _voiceInfo.Age.ToString();

    public string Gender => _voiceInfo.Gender.ToString();

    public string Description => _voiceInfo.Description;

    public bool IsEnabled => _isEnabled;
  }
}
