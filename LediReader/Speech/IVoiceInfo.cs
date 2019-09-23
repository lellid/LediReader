// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LediReader.Speech
{
  /// <summary>
  /// Common interface for both, <see cref="System.Speech.Synthesis.VoiceInfo"/> and <see cref="Windows.Media.SpeechSynthesis.VoiceInformation"/>.
  /// </summary>
  public interface IInstalledVoiceInfo
  {
    /// <summary>Gets the name of the voice.</summary>
    string Name { get; }
    /// <summary>Gets the culture string of the voice</summary>
    string Culture { get; }
    /// <summary>Gets the age of the speaker.</summary>
    string Age { get; }
    /// <summary>Gets the gender of the speaker.</summary>
    string Gender { get; }
    /// <summary>Gets the description of the voice.</summary>
    string Description { get; }
    /// <summary>Get a value whether this voice is currently enabled or not.</summary>
    bool IsEnabled { get; }
  }
}
