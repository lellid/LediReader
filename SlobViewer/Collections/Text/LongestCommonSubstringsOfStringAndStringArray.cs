// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlobViewer.Collections.Text
{
    /// <summary>
    /// Determination of those strings from a string array, which have the longest common substrings with a provided string.
    /// </summary>
    public class LongestCommonSubstringsOfStringAndStringArray
    {
        private LongestCommonSubstringOfTwoStrings _twoStringsEvaluator = new LongestCommonSubstringOfTwoStrings();
        private int[] _counts = new int[0];
        private string[] _yTemp = new string[0];

        /// <summary>
        /// Evaluates the strings from array <paramref name="Y"/>, which have the longest common
        /// substrings with string <paramref name="X"/>. Those strings can then be found in <see cref="BestMatches"/>.
        /// </summary>
        /// <param name="X">The first string.</param>
        /// <param name="Y">The array of strings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public void Evaluate(string X, string[] Y, CancellationToken cancellationToken)
        {
            if (Y.Length != _yTemp.Length)
            {
                _yTemp = new string[Y.Length];
                _counts = new int[Y.Length];
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Array.Copy(Y, _yTemp, Y.Length);

            for (int i = 0; i < Y.Length; ++i)
            {
                _counts[i] = _twoStringsEvaluator.LongestCommonSubstring(X, Y[i]);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            Array.Sort(_counts, _yTemp);
        }

        /// <summary>
        /// Preallocates storage space to help avoid repeated allocations.
        /// </summary>
        /// <param name="maxLengthOfFirstString">The maximum length of the first string.</param>
        /// <param name="maxLengthOfArrayStrings">The maximum length of the strings in the string array.</param>
        /// <returns></returns>
        public LongestCommonSubstringsOfStringAndStringArray Preallocate(int maxLengthOfFirstString, int maxLengthOfArrayStrings)
        {
            _twoStringsEvaluator.Preallocate(maxLengthOfFirstString, maxLengthOfArrayStrings);
            return this;
        }

        /// <summary>
        /// Gets the best matches.
        /// </summary>
        /// <value>
        /// The best matches. Returns tuples, containing the string, and the length of the longest common substring of this match with the search string.
        /// </value>
        public IEnumerable<(string Phrase, int CommonCharCount)> BestMatches
        {
            get
            {
                for (int i = _yTemp.Length - 1; i >= 0; --i)
                {
                    yield return (_yTemp[i], _counts[i]);
                }
            }
        }
    }
}
