// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LediReader
{
  /// <summary>
  /// Helper functions to determine if a given type is supported by the current Windows version.
  /// </summary>
  public static class Windows10SDKApiHelper
  {
    /// <summary>
    /// Determines whether the provided type of the Windows 10 SDK is supported.
    /// </summary>
    /// <param name="typeName">Name of the type.</param>
    /// <returns>
    /// <c>true</c> if the specified type is supported; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsTypeSupported(string typeName)
    {
      if (Environment.OSVersion.Version.Major > 6 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 2))
      {
        if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent(typeName))
        {
          return true;
        }
      }
      return false;

    }
  }
}
