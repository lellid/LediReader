// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlobViewer
{
  public interface IWordDictionary
  {
    string FileName { get; set; }

    /// <summary>Gets all keys of this dictionary.</summary>
    /// <returns></returns>
    string[] GetKeys();

    /// <summary>
    /// Gets a number of <paramref name="count"/> keys including the key given in <paramref name="key"/> (if found).
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <param name="count">The number of keys following the given key.</param>
    /// <returns>A array of keys, including the key given (if it is found in the dictionary). The size of the array is equal to or less than <paramref name="count"/>.</returns>
    string[] GetKeys(string key, int count);

    /// <summary>
    /// Try to get the content for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key for which the content should be retrieved..</param>
    /// <param name="value">If successfull, contains the retrieved content and the content identification.</param>
    /// <returns>True if the content with the specified key was found; otherwise, false.</returns>
    bool TryGetValue(string key, out (string Content, string ContentId) value);

    /// <summary>
    /// Gets the <see cref="System.ValueTuple{System.String, System.String}"/> with the specified key.
    /// </summary>
    /// <value>
    /// The value <see cref="System.ValueTuple{System.String, System.String}"/> for the specified key.
    /// </value>
    /// <param name="key">The key.</param>
    /// <returns>The value <see cref="System.ValueTuple{System.String, System.String}"/> for the specified key.</returns>
    (string Content, string ContentId) this[string key] { get; }
  }
}
