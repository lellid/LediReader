// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlobViewer
{
  /// <summary>
  /// Helper class for relative file paths.
  /// </summary>
  public static class PathResolver
  {
    /// <summary>
    /// Converts an absolute file path to a relative file path that is relative to the path of the entry assembly.
    /// </summary>
    /// <param name="absolutePath">The absolute path.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">absolutePath - Path is not rooted</exception>
    public static string GetPathRelativeToEntryAssembly(string absolutePath)
    {
      if (!Path.IsPathRooted(absolutePath))
        throw new ArgumentOutOfRangeException(nameof(absolutePath), "Path is not rooted");
      var entryAssemblyDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

      if (Path.GetPathRoot(absolutePath) != Path.GetPathRoot(entryAssemblyDirectory))
        return null; // there is no common root, so no relative name could be returned

      var absolutePathParts = absolutePath.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
      var entryAssemblyPathParts = entryAssemblyDirectory.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });


      int rootIndex;
      for (rootIndex = 0; rootIndex < Math.Min(absolutePathParts.Length, entryAssemblyPathParts.Length); ++rootIndex)
      {
        if (absolutePathParts[rootIndex] != entryAssemblyPathParts[rootIndex])
          break;
      }

      if (rootIndex == 0) // then both parts do not have a common root
        return null; // there is no relative name that could be returned

      // no go down from entryAssemblyPathParts.Length to i, and then up again to absolutePathParts.Length

      var stb = new StringBuilder();

      for (int i = entryAssemblyPathParts.Length - 1; i >= rootIndex; --i)
      {
        stb.Append("..");
        stb.Append(Path.DirectorySeparatorChar);
      }

      for (int i = rootIndex; i < absolutePathParts.Length; ++i)
      {
        stb.Append(absolutePathParts[i]);
        if (i != (absolutePathParts.Length - 1))
          stb.Append(Path.DirectorySeparatorChar);
      }

      return stb.ToString();
    }

    /// <summary>
    /// Resolves an absolute and relative file path. The absolute path has precedence, i.e. if the absolute path is valid, it is returned. If it is not valid,
    /// then the relative path is converted to an absolute path, using the path of the entry assembly as root.
    /// </summary>
    /// <param name="absolutePath">The absolute path.</param>
    /// <param name="relativePath">The relative path.</param>
    /// <returns>Either the absolute path, or if it is not valid, then the relative path with the path of the entry assembly as root.</returns>
    public static string ResolvePathRelativeToEntryAssembly(string absolutePath, string relativePath)
    {
      if (File.Exists(absolutePath))
        return absolutePath;

      if (!string.IsNullOrEmpty(relativePath))
      {
        var entryAssemblyDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        if (!entryAssemblyDirectory.EndsWith("" + Path.DirectorySeparatorChar))
          entryAssemblyDirectory += Path.DirectorySeparatorChar;
        absolutePath = entryAssemblyDirectory + relativePath;
        FileInfo file = new FileInfo(absolutePath);
        if (file.Exists)
          return file.FullName;
      }

      return null;
    }
  }
}
