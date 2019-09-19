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
  /// Responsible for keeping the display switched on during speech.
  /// Encapsulates a <see cref="Windows.System.Display.DisplayRequest"/> to hide the inner class in order to avoid exceptions if the inner class is not supported by the operating system.
  /// </summary>
  /// <seealso cref="System.IDisposable" />
  /// <remarks>This class needs to reference Nuget Microsoft.Windows.SDK.Contracts.</remarks>
  public class DisplayRequestLevel1 : IDisposable
  {
    Windows.System.Display.DisplayRequest _displayRequest;


    public DisplayRequestLevel1()
    {
      if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.System.Display.DisplayRequest"))
      {
        _displayRequest = new Windows.System.Display.DisplayRequest();
        _displayRequest.RequestActive();
      }
    }

    public void Dispose()
    {
      _displayRequest?.RequestRelease();
      _displayRequest = null;
    }
  }
}
