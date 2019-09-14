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

                SelectedVoice = _speechWorker.SpeechVoiceInfo;
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
                var oldValue = _selectedVoice;
                _selectedVoice = value;
                if (_selectedVoice != oldValue)
                {
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
                var oldValue = _speakingRate;
                _speakingRate = (int)Math.Round(Math.Min(10, Math.Max(-10, value)));
                if (_speakingRate != oldValue)
                {
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
                var oldValue = _speakingVolume;
                _speakingVolume = (int)Math.Round(Math.Min(100, Math.Max(0, value)));
                if (_speakingVolume != oldValue)
                {
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
                var oldValue = _grayLevelDarkMode;
                _grayLevelDarkMode = (int)Math.Round(Math.Min(255, Math.Max(0, value)));
                if (_grayLevelDarkMode != oldValue)
                {
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
                var oldValue = _grayLevelLightMode;
                _grayLevelLightMode = (int)Math.Round(Math.Min(255, Math.Max(0, value)));
                if (_grayLevelLightMode != oldValue)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GrayLevelLightMode)));
                }
            }
        }


        #endregion

        public void Initialize(Speech.SpeechSettings settings)
        {
            this.SpeakingRate = settings.SpeechRate;
            this.SpeakingVolume = settings.SpeechVolume;
            GrayLevelDarkMode = ToGrayFromRGBA(settings.WorkingBackgroundColorDarkMode);
            GrayLevelLightMode = ToGrayFromRGBA(settings.WorkingBackgroundColorLightMode);
        }

        public static (byte r, byte g, byte b, byte a) ToRGBA(int value)
        {
            uint v = (uint)value;

            byte a = (byte)(v & 0xFF);
            v >>= 8;
            byte b = (byte)(v & 0xFF);
            v >>= 8;
            byte g = (byte)(v & 0xFF);
            v >>= 8;
            byte r = (byte)(v & 0xFF);
            return (r, g, b, a);
        }

        public static byte ToGrayFromRGBA(int value)
        {
            var (r, g, b, a) = ToRGBA(value);
            return (byte)(0.30 * r + 0.59 * g + 0.11 * b);
        }

        public static int ToRGBAIntFromGrayLevel(byte grayLevel)
        {
            return ToRGBAInt((grayLevel, grayLevel, grayLevel, 255));
        }

        public static int ToRGBAInt((byte r, byte g, byte b, byte a) tuple)
        {
            uint v = 0;

            v |= tuple.r;
            v <<= 8;
            v |= tuple.g;
            v <<= 8;
            v |= tuple.b;
            v <<= 8;
            v |= tuple.a;
            return (int)v;
        }

        public void Apply(Speech.SpeechSettings settings)
        {
            settings.SpeechVoice = this.SelectedVoice.Name;
            settings.SpeechRate = (int)this.SpeakingRate;
            settings.SpeechVolume = (int)this.SpeakingVolume;

            settings.WorkingBackgroundColorDarkMode = ToRGBAIntFromGrayLevel((byte)this.GrayLevelDarkMode);
            settings.WorkingBackgroundColorLightMode = ToRGBAIntFromGrayLevel((byte)this.GrayLevelLightMode);
        }
    }
}
