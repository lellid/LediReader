// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace LediReader.Gui
{
  public class SpeechSettingsController : INotifyPropertyChanged
  {
    ObservableCollection<VoiceInfo> _voices = new ObservableCollection<VoiceInfo>();

    public event PropertyChangedEventHandler PropertyChanged;


    public SpeechSettingsController()
    {

    }

    Speech.SpeechWorker _speechWorker;

    public Speech.SpeechWorker Synthesizer
    {
      set
      {
        if (null != value)
        {
          _speechWorker = value;
          foreach (InstalledVoice voice in _speechWorker.GetInstalledVoices())
          {
            _voices.Add(voice.VoiceInfo);
          }
        }

        SelectedVoice = _speechWorker.GetSpeechVoiceInfo();
        SpeakingRate = _speechWorker.SpeechRate;
        SpeakingVolume = _speechWorker.SpeechVolume;
      }
    }

    #region Bindable properties

    public IEnumerable<VoiceInfo> Voices => _voices;


    VoiceInfo _selectedVoice;

    public VoiceInfo SelectedVoice
    {
      get
      {
        return _selectedVoice;
      }
      set
      {
        if (!(_selectedVoice == value))
        {
          _selectedVoice = value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedVoice)));
          if (null != _speechWorker && _selectedVoice != null)
            _speechWorker.SpeechVoice = _selectedVoice.Name;
        }
      }
    }

    int _speakingRate;

    public double SpeakingRate
    {
      get
      {
        return _speakingRate;
      }
      set
      {
        value = (int)Math.Round(Math.Min(10, Math.Max(-10, value)));
        if (!(_speakingRate == value))
        {
          _speakingRate = (int)value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpeakingRate)));
          if (null != _speechWorker)
          {
            _speechWorker.SpeechRate = _speakingRate;
          }
        }
      }
    }

    int _speakingVolume;

    public double SpeakingVolume
    {
      get
      {
        return _speakingVolume;
      }
      set
      {

        value = Math.Round(Math.Min(100, Math.Max(0, value)));
        if (!(_speakingVolume == value))
        {
          _speakingVolume = (int)value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpeakingVolume)));
          if (null != _speechWorker)
          {
            _speechWorker.SpeechVolume = _speakingVolume;
          }
        }

      }
    }

    int _grayLevelDarkMode;
    public double GrayLevelDarkMode
    {
      get
      {
        return _grayLevelDarkMode;
      }
      set
      {

        value = Math.Round(Math.Min(255, Math.Max(0, value)));
        if (!(_grayLevelDarkMode == value))
        {
          _grayLevelDarkMode = (int)value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GrayLevelDarkMode)));
        }
      }
    }

    int _grayLevelLightMode;
    public double GrayLevelLightMode
    {
      get
      {
        return _grayLevelLightMode;
      }
      set
      {

        value = Math.Round(Math.Min(255, Math.Max(0, value)));
        if (!(_grayLevelLightMode == value))
        {
          _grayLevelLightMode = (int)value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GrayLevelLightMode)));
        }
      }
    }

    bool _keepDisplayOn;
    public bool KeepDisplayOn
    {
      get
      {
        return _keepDisplayOn;
      }
      set
      {
        if (!(_keepDisplayOn == value))
        {
          _keepDisplayOn = value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeepDisplayOn)));
        }
      }
    }

    #endregion

    public void Initialize(Speech.SpeechSettings settings)
    {
      this.SpeakingRate = settings.SpeechRate;
      this.SpeakingVolume = settings.SpeechVolume;
      this.KeepDisplayOn = settings.KeepDisplayOnDuringSpeech;
      GrayLevelDarkMode = ColorConverter.ToGrayFromRGBA(settings.WorkingBackgroundColorDarkMode);
      GrayLevelLightMode = ColorConverter.ToGrayFromRGBA(settings.WorkingBackgroundColorLightMode);
    }



    public void Apply(Speech.SpeechSettings settings)
    {
      settings.SpeechVoice = this.SelectedVoice.Name;
      settings.SpeechCulture = this.SelectedVoice.Culture.Name;
      settings.SpeechRate = (int)this.SpeakingRate;
      settings.SpeechVolume = (int)this.SpeakingVolume;
      settings.KeepDisplayOnDuringSpeech = this.KeepDisplayOn;

      settings.WorkingBackgroundColorDarkMode = ColorConverter.ToRGBAIntFromGrayLevel((byte)this.GrayLevelDarkMode);
      settings.WorkingBackgroundColorLightMode = ColorConverter.ToRGBAIntFromGrayLevel((byte)this.GrayLevelLightMode);
    }
  }
}
