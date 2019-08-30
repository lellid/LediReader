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

        SpeechSynthesizer _synthesizer;

        public SpeechSynthesizer Synthesizer
        {
            set
            {
                if (null != value)
                {
                    _synthesizer = value;
                    foreach (InstalledVoice voice in _synthesizer.GetInstalledVoices())
                    {
                        _voices.Add(voice.VoiceInfo);
                    }
                }

                SelectedVoice = _synthesizer.Voice;
                SpeakingRate = _synthesizer.Rate;
                SpeakingVolume = _synthesizer.Volume;
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
                var oldValue = _selectedVoice;
                _selectedVoice = value;
                if (_selectedVoice != oldValue)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedVoice)));
                    if (null != _synthesizer && _selectedVoice != null)
                        _synthesizer.SelectVoice(_selectedVoice.Name);
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
                var oldValue = _speakingRate;
                _speakingRate = (int)Math.Round(Math.Min(10, Math.Max(-10, value)));
                if (_speakingRate != oldValue)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpeakingRate)));
                    if (null != _synthesizer)
                    {
                        _synthesizer.Rate = _speakingRate;
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
                var oldValue = _speakingVolume;
                _speakingVolume = (int)Math.Round(Math.Min(100, Math.Max(0, value)));
                if (_speakingVolume != oldValue)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpeakingVolume)));
                    if (null != _synthesizer)
                    {
                        _synthesizer.Volume = _speakingVolume;
                    }
                }

            }
        }


        #endregion
    }
}
