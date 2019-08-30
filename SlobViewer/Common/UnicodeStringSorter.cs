// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlobViewer.Common
{
    /// <summary>
    /// An <see cref="IComparer{System.Globalization.SortKey}"/> that uses <see cref="System.Globalization.SortKey"/> instances
    /// for sorting of Unicode strings.
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IComparer{System.Globalization.SortKey}" />
    class UnicodeStringSorter : IComparer<SortKey>
    {
        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>
        /// A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.Value Meaning Less than zero
        /// <paramref name="x" /> is less than <paramref name="y" />.Zero
        /// <paramref name="x" /> equals <paramref name="y" />.Greater than zero
        /// <paramref name="x" /> is greater than <paramref name="y" />.
        /// </returns>
        public int Compare(SortKey x, SortKey y)
        {
            return SortKey.Compare(x, y);
        }
    }
}
